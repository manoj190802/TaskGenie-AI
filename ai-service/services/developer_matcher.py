"""
Developer Matcher Service — Scores and ranks developers for a given task
using a weighted multi-factor algorithm.
"""
import logging
from typing import List
from models.schemas import (
    DeveloperProfile, AnalyzedTask, DeveloperScore, DeveloperMatchResponse
)

logger = logging.getLogger(__name__)

# Scoring weights (must sum to 1.0)
WEIGHTS = {
    "skill_match": 0.40,
    "experience": 0.25,
    "availability": 0.20,
    "workload": 0.15,
}

AVAILABILITY_SCORES = {
    "Available": 1.0,
    "Busy": 0.5,
    "OnLeave": 0.0,
}


def _normalize_skill(skill: str) -> str:
    """Normalize skill name for comparison."""
    return skill.lower().strip().replace(" ", "").replace(".", "").replace("#", "sharp")


def _skill_match_score(task_skills: List[str], dev_skills: List[str]) -> tuple[float, List[str], List[str]]:
    """
    Calculate skill match score.
    Returns (score 0-1, matched_skills, missing_skills)
    """
    if not task_skills:
        return 1.0, [], []

    norm_task = {_normalize_skill(s): s for s in task_skills}
    norm_dev = {_normalize_skill(s) for s in dev_skills}

    matched = [orig for norm, orig in norm_task.items() if norm in norm_dev]
    missing = [orig for norm, orig in norm_task.items() if norm not in norm_dev]

    # Partial match bonus: if dev has related skills
    bonus = 0.0
    related_pairs = [
        ({"angular", "react", "vue"}, {"typescript", "javascript", "html", "css"}),
        ({"dotnet", "csharp"}, {"java", "python"}),
        ({"mongodb"}, {"postgresql", "mysql", "nosql"}),
    ]
    for group_a, group_b in related_pairs:
        task_in_a = any(_normalize_skill(s) in group_a for s in task_skills)
        dev_in_b = any(_normalize_skill(s) in group_b for s in dev_skills)
        if task_in_a and dev_in_b:
            bonus = min(bonus + 0.05, 0.15)

    score = len(matched) / len(task_skills) if task_skills else 0.0
    score = min(score + bonus, 1.0)

    return score, matched, missing


def _experience_score(years: int) -> float:
    """Map experience years to 0-1 score."""
    if years <= 0:
        return 0.1
    elif years == 1:
        return 0.3
    elif years == 2:
        return 0.5
    elif years <= 4:
        return 0.7
    elif years <= 7:
        return 0.85
    else:
        return 1.0


def _workload_score(current_workload: int) -> float:
    """Lower workload = higher score. workload is 0-100."""
    return max(0.0, (100 - current_workload) / 100)


def _generate_recommendation_reason(
    dev: DeveloperProfile,
    matched_skills: List[str],
    missing_skills: List[str],
    score: float,
) -> str:
    """Generate a human-readable recommendation reason."""
    parts = []

    if score >= 0.85:
        parts.append(f"Excellent match for this task.")
    elif score >= 0.65:
        parts.append(f"Good match for this task.")
    elif score >= 0.45:
        parts.append(f"Moderate match for this task.")
    else:
        parts.append(f"Limited match for this task.")

    if matched_skills:
        parts.append(f"Has required skills: {', '.join(matched_skills[:3])}{'...' if len(matched_skills) > 3 else ''}.")

    if missing_skills:
        parts.append(f"Missing: {', '.join(missing_skills[:2])}.")

    if dev.availability == "Available":
        parts.append(f"Currently available.")
    elif dev.availability == "Busy":
        parts.append(f"Currently busy but can be assigned.")

    if dev.current_workload <= 30:
        parts.append(f"Low workload ({dev.current_workload}%).")
    elif dev.current_workload <= 60:
        parts.append(f"Moderate workload ({dev.current_workload}%).")
    else:
        parts.append(f"High workload ({dev.current_workload}%) — consider availability.")

    parts.append(f"{dev.experience_years} year(s) of experience.")

    return " ".join(parts)


def match_developers(task: AnalyzedTask, developers: List[DeveloperProfile]) -> DeveloperMatchResponse:
    """
    Score and rank all developers for the given task.
    Returns sorted recommendations with the best match highlighted.
    """
    if not developers:
        return DeveloperMatchResponse(
            task_title=task.title,
            recommendations=[],
            best_match=None,
        )

    scores: List[DeveloperScore] = []

    for dev in developers:
        # Skip developers on leave unless no others available
        if dev.availability == "OnLeave" and len(developers) > 1:
            continue

        skill_score, matched, missing = _skill_match_score(task.skills_required, dev.skills)
        exp_score = _experience_score(dev.experience_years)
        avail_score = AVAILABILITY_SCORES.get(dev.availability, 0.0)
        work_score = _workload_score(dev.current_workload)

        # Weighted total
        total_score = (
            skill_score * WEIGHTS["skill_match"]
            + exp_score * WEIGHTS["experience"]
            + avail_score * WEIGHTS["availability"]
            + work_score * WEIGHTS["workload"]
        )

        reason = _generate_recommendation_reason(dev, matched, missing, total_score)

        scores.append(DeveloperScore(
            developer_id=dev.id,
            developer_name=dev.name,
            score=round(total_score, 4),
            skill_match_score=round(skill_score, 4),
            experience_score=round(exp_score, 4),
            availability_score=round(avail_score, 4),
            workload_score=round(work_score, 4),
            matched_skills=matched,
            missing_skills=missing,
            recommendation_reason=reason,
        ))

    # Sort by score descending
    scores.sort(key=lambda x: x.score, reverse=True)

    best_match = scores[0] if scores else None

    logger.info(
        f"Matched {len(scores)} developers for task '{task.title}'. "
        f"Best: {best_match.developer_name if best_match else 'None'} ({best_match.score if best_match else 0})"
    )

    return DeveloperMatchResponse(
        task_title=task.title,
        recommendations=scores,
        best_match=best_match,
    )
