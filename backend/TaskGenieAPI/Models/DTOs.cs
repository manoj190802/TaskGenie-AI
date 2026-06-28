namespace TaskGenieAPI.Models;

// ── Auth DTOs ─────────────────────────────────────────────────────────────────

public class RegisterRequest
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Role { get; set; } = "ProjectManager";
}

public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class AuthResponse
{
    public string Token { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}

// ── Developer DTOs ────────────────────────────────────────────────────────────

public class CreateDeveloperRequest
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public List<string> Skills { get; set; } = new();
    public int ExperienceYears { get; set; }
    public string Availability { get; set; } = "Available";
    public int CurrentWorkload { get; set; } = 0;
    public string Bio { get; set; } = string.Empty;
}

public class UpdateDeveloperRequest
{
    public string? Name { get; set; }
    public List<string>? Skills { get; set; }
    public int? ExperienceYears { get; set; }
    public string? Availability { get; set; }
    public int? CurrentWorkload { get; set; }
    public string? Bio { get; set; }
}

// ── Project DTOs ──────────────────────────────────────────────────────────────

public class CreateProjectRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime? DueDate { get; set; }
}

public class UpdateProjectRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Status { get; set; }
    public DateTime? DueDate { get; set; }
}

// ── Task DTOs ─────────────────────────────────────────────────────────────────

public class CreateTaskRequest
{
    public string ProjectId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = "Full Stack";
    public List<string> SkillsRequired { get; set; } = new();
    public int EstimatedHours { get; set; }
    public string Priority { get; set; } = "Medium";
    public string Complexity { get; set; } = "Medium";
}

public class UpdateTaskStatusRequest
{
    public string Status { get; set; } = string.Empty;
}

// ── Assignment DTOs ───────────────────────────────────────────────────────────

public class CreateAssignmentRequest
{
    public string TaskId { get; set; } = string.Empty;
    public string DeveloperId { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public bool AiAssisted { get; set; } = false;
    public double? AiScore { get; set; }
}

public class ReassignRequest
{
    public string NewDeveloperId { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}

// ── AI Bridge DTOs ────────────────────────────────────────────────────────────

public class AnalyzeRequirementsRequest
{
    public string Text { get; set; } = string.Empty;
    public string ProjectName { get; set; } = "Unnamed Project";
}

public class AnalyzedTaskDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public List<string> SkillsRequired { get; set; } = new();
    public int EstimatedHours { get; set; }
    public string Priority { get; set; } = "Medium";
    public string Complexity { get; set; } = "Medium";
}

public class AnalysisResultDto
{
    public string ProjectSummary { get; set; } = string.Empty;
    public List<AnalyzedTaskDto> Tasks { get; set; } = new();
    public int TotalEstimatedHours { get; set; }
    public List<string> TechStackDetected { get; set; } = new();
}

public class DeveloperProfileDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public List<string> Skills { get; set; } = new();
    public int ExperienceYears { get; set; }
    public string Availability { get; set; } = "Available";
    public int CurrentWorkload { get; set; }
}

public class DeveloperScoreDto
{
    public string DeveloperId { get; set; } = string.Empty;
    public string DeveloperName { get; set; } = string.Empty;
    public double Score { get; set; }
    public double SkillMatchScore { get; set; }
    public double ExperienceScore { get; set; }
    public double AvailabilityScore { get; set; }
    public double WorkloadScore { get; set; }
    public List<string> MatchedSkills { get; set; } = new();
    public List<string> MissingSkills { get; set; } = new();
    public string RecommendationReason { get; set; } = string.Empty;
}

public class MatchResultDto
{
    public string TaskTitle { get; set; } = string.Empty;
    public List<DeveloperScoreDto> Recommendations { get; set; } = new();
    public DeveloperScoreDto? BestMatch { get; set; }
}

// ── Dashboard / Report DTOs ───────────────────────────────────────────────────

public class DashboardStatsDto
{
    public int TotalProjects { get; set; }
    public int ActiveProjects { get; set; }
    public int TotalTasks { get; set; }
    public int PendingTasks { get; set; }
    public int AssignedTasks { get; set; }
    public int InProgressTasks { get; set; }
    public int CompletedTasks { get; set; }
    public int TotalDevelopers { get; set; }
    public int AvailableDevelopers { get; set; }
    public int TotalAssignments { get; set; }
    public List<RecentAssignmentDto> RecentAssignments { get; set; } = new();
    public List<CategoryBreakdownDto> TasksByCategory { get; set; } = new();
}

public class RecentAssignmentDto
{
    public string AssignmentId { get; set; } = string.Empty;
    public string TaskTitle { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public string DeveloperName { get; set; } = string.Empty;
    public DateTime AssignedAt { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class CategoryBreakdownDto
{
    public string Category { get; set; } = string.Empty;
    public int Count { get; set; }
}
