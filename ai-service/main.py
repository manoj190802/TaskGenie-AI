"""
TaskGenie-AI Python AI Service
FastAPI application providing document extraction and AI-powered task analysis.
"""
import logging
import shutil
import tempfile
import os
from pathlib import Path
from typing import Optional

import uvicorn
from fastapi import FastAPI, File, Form, HTTPException, UploadFile
from fastapi.middleware.cors import CORSMiddleware
from fastapi.responses import JSONResponse

from config import PORT, CORS_ORIGINS
from models.schemas import (
    AnalyzeRequirementsRequest,
    AnalyzeRequirementsResponse,
    DeveloperMatchRequest,
    DeveloperMatchResponse,
    ExtractTextResponse,
)
from services.document_extractor import extract_text
from services.ai_analyzer import analyze_requirements
from services.developer_matcher import match_developers

# Configure logging
logging.basicConfig(
    level=logging.INFO,
    format="%(asctime)s - %(name)s - %(levelname)s - %(message)s",
)
logger = logging.getLogger(__name__)

# FastAPI app
app = FastAPI(
    title="TaskGenie AI Service",
    description="AI-powered document analysis and developer matching for TaskGenie-AI",
    version="1.0.0",
    docs_url="/docs",
    redoc_url="/redoc",
)

# CORS middleware
app.add_middleware(
    CORSMiddleware,
    allow_origins=CORS_ORIGINS,
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)


# ── Health Check ──────────────────────────────────────────────────────────────

@app.get("/health", tags=["Health"])
async def health_check():
    """Health check endpoint."""
    return {"status": "healthy", "service": "TaskGenie AI Service", "version": "1.0.0"}


# ── Document Extraction ───────────────────────────────────────────────────────

@app.post("/extract-text", response_model=ExtractTextResponse, tags=["Document"])
async def extract_text_endpoint(
    file: UploadFile = File(..., description="PDF, DOCX, or TXT file to extract text from"),
):
    """
    Upload a document file (PDF, DOCX, or TXT) and extract its text content.
    Returns the extracted text, page count (for PDFs), and word count.
    """
    # Validate file type
    allowed_extensions = {".pdf", ".docx", ".doc", ".txt"}
    file_ext = Path(file.filename).suffix.lower()

    if file_ext not in allowed_extensions:
        raise HTTPException(
            status_code=400,
            detail=f"Unsupported file type '{file_ext}'. Allowed: {', '.join(allowed_extensions)}"
        )

    # Read file bytes
    file_bytes = await file.read()
    if len(file_bytes) == 0:
        raise HTTPException(status_code=400, detail="Uploaded file is empty")

    max_size_mb = 10
    if len(file_bytes) > max_size_mb * 1024 * 1024:
        raise HTTPException(status_code=400, detail=f"File too large. Maximum size: {max_size_mb}MB")

    try:
        result = extract_text(file_bytes, file.filename)
        return ExtractTextResponse(**result)
    except ValueError as e:
        raise HTTPException(status_code=422, detail=str(e))
    except Exception as e:
        logger.error(f"Unexpected error during text extraction: {e}")
        raise HTTPException(status_code=500, detail="Internal error during text extraction")


# ── AI Requirement Analysis ───────────────────────────────────────────────────

@app.post("/analyze-requirements", response_model=AnalyzeRequirementsResponse, tags=["AI Analysis"])
async def analyze_requirements_endpoint(request: AnalyzeRequirementsRequest):
    """
    Analyze project requirements text using AI/LLM.
    Returns classified tasks with skill requirements, estimated hours, and priority.
    Uses OpenAI GPT-4o if API key is configured, otherwise falls back to rule-based NLP.
    """
    if not request.text or len(request.text.strip()) < 50:
        raise HTTPException(
            status_code=400,
            detail="Requirements text is too short. Please provide at least 50 characters."
        )

    try:
        result = await analyze_requirements(request.text, request.project_name)
        return result
    except Exception as e:
        logger.error(f"Error analyzing requirements: {e}")
        raise HTTPException(status_code=500, detail=f"Analysis failed: {str(e)}")


@app.post("/extract-and-analyze", tags=["AI Analysis"])
async def extract_and_analyze(
    file: UploadFile = File(...),
    project_name: str = Form("Unnamed Project"),
):
    """
    Combined endpoint: upload file → extract text → analyze with AI.
    Returns both the extracted text and the full AI analysis.
    """
    # Extract text
    allowed_extensions = {".pdf", ".docx", ".doc", ".txt"}
    file_ext = Path(file.filename).suffix.lower()
    if file_ext not in allowed_extensions:
        raise HTTPException(status_code=400, detail=f"Unsupported file type: {file_ext}")

    file_bytes = await file.read()
    if not file_bytes:
        raise HTTPException(status_code=400, detail="Empty file")

    try:
        extracted = extract_text(file_bytes, file.filename)
        analysis = await analyze_requirements(extracted["text"], project_name)
        return {
            "extraction": extracted,
            "analysis": analysis.model_dump(),
        }
    except ValueError as e:
        raise HTTPException(status_code=422, detail=str(e))
    except Exception as e:
        logger.error(f"Error in extract-and-analyze: {e}")
        raise HTTPException(status_code=500, detail=str(e))


# ── Developer Matching ────────────────────────────────────────────────────────

@app.post("/match-developers", response_model=DeveloperMatchResponse, tags=["Matching"])
async def match_developers_endpoint(request: DeveloperMatchRequest):
    """
    Match developers to a task using the weighted scoring algorithm.
    Considers: skill match (40%), experience (25%), availability (20%), workload (15%).
    Returns ranked list of developers with scores and recommendation reasons.
    """
    if not request.developers:
        raise HTTPException(status_code=400, detail="No developers provided for matching")

    try:
        result = match_developers(request.task, request.developers)
        return result
    except Exception as e:
        logger.error(f"Error matching developers: {e}")
        raise HTTPException(status_code=500, detail=f"Matching failed: {str(e)}")


# ── Error Handlers ────────────────────────────────────────────────────────────

@app.exception_handler(Exception)
async def global_exception_handler(request, exc):
    logger.error(f"Unhandled exception: {exc}")
    return JSONResponse(
        status_code=500,
        content={"detail": "An unexpected error occurred. Please try again."},
    )


# ── Entry Point ───────────────────────────────────────────────────────────────

if __name__ == "__main__":
    uvicorn.run(
        "main:app",
        host="0.0.0.0",
        port=PORT,
        reload=True,
        log_level="info",
    )
