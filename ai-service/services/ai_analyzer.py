"""
AI Analyzer Service — Uses OpenAI GPT to analyze project requirements
and extract structured tasks with classifications.
"""
import json
import logging
from typing import Optional
from openai import AsyncOpenAI
from config import OPENAI_API_KEY, OPENAI_MODEL
from models.schemas import AnalyzeRequirementsResponse, AnalyzedTask, TaskCategory

logger = logging.getLogger(__name__)

client = None
if OPENAI_API_KEY and OPENAI_API_KEY != "your_openai_api_key_here":
    try:
        client = AsyncOpenAI(api_key=OPENAI_API_KEY)
    except Exception as init_err:
        logger.error(f"Failed to initialize OpenAI client: {init_err}")

SYSTEM_PROMPT = """You are an expert software project analyst and technical architect.
Your job is to analyze software project requirement documents and extract structured task information.

For each requirement, create specific, actionable development tasks and classify them accurately.

Task categories:
- Frontend: UI components, pages, user interactions, CSS, Angular/React/Vue work
- Backend: APIs, business logic, database operations, .NET/Node/Python server code
- Full Stack: Features requiring both frontend and backend work
- Testing: Unit tests, integration tests, E2E tests, QA activities
- DevOps: CI/CD, deployment, infrastructure, Docker, cloud
- Design: UI/UX design, wireframes, mockups

Always respond with valid JSON only. No markdown, no explanation outside JSON."""

ANALYSIS_PROMPT_TEMPLATE = """Analyze the following software project requirements document for project "{project_name}".

REQUIREMENTS DOCUMENT:
---
{text}
---

Extract all development tasks and return a JSON object with this exact structure:
{{
  "project_summary": "2-3 sentence summary of what the project does",
  "tech_stack_detected": ["list", "of", "technologies", "mentioned"],
  "total_estimated_hours": <integer total>,
  "tasks": [
    {{
      "title": "Short task title",
      "description": "Detailed description of what needs to be done",
      "category": "Frontend|Backend|Full Stack|Testing|DevOps|Design",
      "skills_required": ["skill1", "skill2"],
      "estimated_hours": <integer>,
      "priority": "Low|Medium|High",
      "complexity": "Low|Medium|High"
    }}
  ]
}}

Be thorough — extract ALL tasks from the requirements. Minimum 5 tasks, maximum 30.
For skills_required, use specific technology names (e.g., "Angular", "MongoDB", ".NET", "JWT", "Python").
"""


async def analyze_requirements(text: str, project_name: str = "Unnamed Project") -> AnalyzeRequirementsResponse:
    """
    Send requirements text to OpenAI and get back structured task analysis.
    Falls back to rule-based analysis if OpenAI is unavailable.
    """
    if not client:
        logger.warning("OpenAI client is not initialized, using rule-based fallback")
        return _rule_based_analysis(text, project_name)

    try:
        prompt = ANALYSIS_PROMPT_TEMPLATE.format(text=text[:8000], project_name=project_name)

        response = await client.chat.completions.create(
            model=OPENAI_MODEL,
            messages=[
                {"role": "system", "content": SYSTEM_PROMPT},
                {"role": "user", "content": prompt},
            ],
            temperature=0.3,
            response_format={"type": "json_object"},
            max_tokens=4000,
        )

        raw = response.choices[0].message.content
        data = json.loads(raw)

        tasks = []
        for t in data.get("tasks", []):
            try:
                tasks.append(AnalyzedTask(
                    title=t["title"],
                    description=t["description"],
                    category=TaskCategory(t["category"]),
                    skills_required=t.get("skills_required", []),
                    estimated_hours=int(t.get("estimated_hours", 8)),
                    priority=t.get("priority", "Medium"),
                    complexity=t.get("complexity", "Medium"),
                ))
            except Exception as task_err:
                logger.warning(f"Skipping malformed task: {task_err}")

        return AnalyzeRequirementsResponse(
            project_summary=data.get("project_summary", ""),
            tasks=tasks,
            total_estimated_hours=sum(t.estimated_hours for t in tasks),
            tech_stack_detected=data.get("tech_stack_detected", []),
        )

    except Exception as e:
        logger.error(f"OpenAI analysis failed: {e}, falling back to rule-based")
        return _rule_based_analysis(text, project_name)


def _rule_based_analysis(text: str, project_name: str) -> AnalyzeRequirementsResponse:
    """
    Fallback rule-based task extraction using keyword analysis.
    Used when OpenAI API key is not configured.
    """
    text_lower = text.lower()
    tasks = []

    # Keyword → category + skills mapping
    rules = [
        # Frontend signals
        (["angular", "react", "vue", "ui", "dashboard", "frontend", "page", "component", "form"],
         TaskCategory.FRONTEND, ["Angular", "TypeScript", "CSS", "HTML"]),
        # Backend signals
        ([".net", "api", "endpoint", "controller", "database", "mongodb", "backend", "server", "auth", "jwt"],
         TaskCategory.BACKEND, [".NET", "C#", "MongoDB", "REST API"]),
        # Testing signals
        (["test", "testing", "unit test", "integration test", "e2e", "qa"],
         TaskCategory.TESTING, ["xUnit", "Jest", "Playwright"]),
        # Python/AI signals
        (["python", "ai", "llm", "machine learning", "nlp", "extract", "analyze"],
         TaskCategory.BACKEND, ["Python", "FastAPI", "OpenAI"]),
    ]

    # Detect tech stack
    tech_keywords = {
        "Angular": ["angular"], "React": ["react"], ".NET": [".net", "dotnet", "asp.net"],
        "MongoDB": ["mongodb"], "Python": ["python"], "JWT": ["jwt"], "Docker": ["docker"],
        "TypeScript": ["typescript"], "C#": ["c#", "csharp"],
    }
    tech_stack = [tech for tech, kws in tech_keywords.items() if any(kw in text_lower for kw in kws)]

    # Generate tasks based on detected patterns
    if any(k in text_lower for k in ["angular", "dashboard", "ui"]):
        tasks.append(AnalyzedTask(
            title="Build Angular Dashboard UI",
            description="Create responsive Angular dashboard with KPI cards, navigation, and overview widgets.",
            category=TaskCategory.FRONTEND,
            skills_required=["Angular", "TypeScript", "CSS", "Angular Material"],
            estimated_hours=24, priority="High", complexity="Medium",
        ))
        tasks.append(AnalyzedTask(
            title="Implement Authentication UI",
            description="Create login and registration pages with JWT token handling and route guards.",
            category=TaskCategory.FRONTEND,
            skills_required=["Angular", "JWT", "TypeScript", "RxJS"],
            estimated_hours=16, priority="High", complexity="Medium",
        ))

    if any(k in text_lower for k in [".net", "api", "backend", "auth"]):
        tasks.append(AnalyzedTask(
            title="Set Up .NET Web API Project",
            description="Scaffold .NET 8 Web API with MongoDB integration, JWT authentication, and CORS configuration.",
            category=TaskCategory.BACKEND,
            skills_required=[".NET", "C#", "MongoDB", "JWT"],
            estimated_hours=20, priority="High", complexity="High",
        ))
        tasks.append(AnalyzedTask(
            title="Implement User Authentication & Authorization",
            description="JWT-based auth with role management (Admin, Project Manager). Password hashing, token refresh.",
            category=TaskCategory.BACKEND,
            skills_required=[".NET", "C#", "JWT", "MongoDB"],
            estimated_hours=16, priority="High", complexity="Medium",
        ))

    if any(k in text_lower for k in ["mongodb", "database", "store"]):
        tasks.append(AnalyzedTask(
            title="Design MongoDB Data Models",
            description="Define collections and schemas for users, developers, projects, tasks, and assignments.",
            category=TaskCategory.BACKEND,
            skills_required=["MongoDB", "Database Design"],
            estimated_hours=8, priority="High", complexity="Medium",
        ))

    if any(k in text_lower for k in ["python", "ai", "extract", "analyze", "document"]):
        tasks.append(AnalyzedTask(
            title="Implement Document Text Extraction",
            description="Python service to extract text from PDF, DOCX, and TXT files using pdfplumber and python-docx.",
            category=TaskCategory.BACKEND,
            skills_required=["Python", "FastAPI", "pdfplumber", "python-docx"],
            estimated_hours=12, priority="High", complexity="Medium",
        ))
        tasks.append(AnalyzedTask(
            title="AI-Powered Requirement Analysis",
            description="Integrate OpenAI API to analyze requirement documents and classify tasks by category and skills.",
            category=TaskCategory.BACKEND,
            skills_required=["Python", "OpenAI API", "FastAPI", "NLP"],
            estimated_hours=20, priority="High", complexity="High",
        ))

    if any(k in text_lower for k in ["assign", "developer", "recommend"]):
        tasks.append(AnalyzedTask(
            title="Developer Recommendation Algorithm",
            description="Build scoring algorithm to match tasks with developers based on skills, experience, and workload.",
            category=TaskCategory.BACKEND,
            skills_required=["Python", "Algorithm Design"],
            estimated_hours=16, priority="High", complexity="High",
        ))

    if any(k in text_lower for k in ["report", "chart", "analytics"]):
        tasks.append(AnalyzedTask(
            title="Build Reports & Analytics Module",
            description="Generate assignment reports with charts showing task distribution, completion rates, and developer workload.",
            category=TaskCategory.FULL_STACK,
            skills_required=["Angular", "Chart.js", ".NET", "MongoDB Aggregation"],
            estimated_hours=20, priority="Medium", complexity="Medium",
        ))

    # Always add testing task
    tasks.append(AnalyzedTask(
        title="Write Integration & Unit Tests",
        description="Comprehensive test coverage for API endpoints, Angular components, and Python services.",
        category=TaskCategory.TESTING,
        skills_required=["xUnit", "Jest", "pytest", "Playwright"],
        estimated_hours=24, priority="Medium", complexity="Medium",
    ))

    if not tasks:
        # Generic fallback
        tasks.append(AnalyzedTask(
            title="Analyze and Implement Requirements",
            description="Implement the specified requirements based on the provided document.",
            category=TaskCategory.FULL_STACK,
            skills_required=["Full Stack Development"],
            estimated_hours=40, priority="High", complexity="High",
        ))

    return AnalyzeRequirementsResponse(
        project_summary=f"{project_name}: A software project requiring full-stack development across frontend, backend, and AI services.",
        tasks=tasks,
        total_estimated_hours=sum(t.estimated_hours for t in tasks),
        tech_stack_detected=tech_stack,
    )
