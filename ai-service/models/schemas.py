from pydantic import BaseModel, Field
from typing import List, Optional
from enum import Enum


class TaskCategory(str, Enum):
    FRONTEND = "Frontend"
    BACKEND = "Backend"
    FULL_STACK = "Full Stack"
    TESTING = "Testing"
    DEVOPS = "DevOps"
    DESIGN = "Design"


class ExtractTextRequest(BaseModel):
    file_name: str


class ExtractTextResponse(BaseModel):
    text: str
    page_count: Optional[int] = None
    word_count: int


class AnalyzedTask(BaseModel):
    title: str
    description: str
    category: TaskCategory
    skills_required: List[str]
    estimated_hours: int
    priority: str = "Medium"  # Low, Medium, High
    complexity: str = "Medium"  # Low, Medium, High


class AnalyzeRequirementsRequest(BaseModel):
    text: str
    project_name: Optional[str] = "Unnamed Project"


class AnalyzeRequirementsResponse(BaseModel):
    project_summary: str
    tasks: List[AnalyzedTask]
    total_estimated_hours: int
    tech_stack_detected: List[str]


class DeveloperProfile(BaseModel):
    id: str
    name: str
    skills: List[str]
    experience_years: int
    availability: str  # Available, Busy, OnLeave
    current_workload: int  # 0-100 percentage


class DeveloperMatchRequest(BaseModel):
    task: AnalyzedTask
    developers: List[DeveloperProfile]


class DeveloperScore(BaseModel):
    developer_id: str
    developer_name: str
    score: float
    skill_match_score: float
    experience_score: float
    availability_score: float
    workload_score: float
    matched_skills: List[str]
    missing_skills: List[str]
    recommendation_reason: str


class DeveloperMatchResponse(BaseModel):
    task_title: str
    recommendations: List[DeveloperScore]
    best_match: Optional[DeveloperScore] = None
