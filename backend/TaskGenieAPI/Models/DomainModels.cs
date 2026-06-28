using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace TaskGenieAPI.Models;

public class User
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("email")]
    public string Email { get; set; } = string.Empty;

    [BsonElement("passwordHash")]
    public string PasswordHash { get; set; } = string.Empty;

    [BsonElement("role")]
    public string Role { get; set; } = "ProjectManager"; // Admin | ProjectManager

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("isActive")]
    public bool IsActive { get; set; } = true;
}

public class Developer
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("email")]
    public string Email { get; set; } = string.Empty;

    [BsonElement("skills")]
    public List<string> Skills { get; set; } = new();

    [BsonElement("experienceYears")]
    public int ExperienceYears { get; set; }

    [BsonElement("availability")]
    public string Availability { get; set; } = "Available"; // Available | Busy | OnLeave

    [BsonElement("currentWorkload")]
    public int CurrentWorkload { get; set; } = 0; // 0-100 percentage

    [BsonElement("bio")]
    public string Bio { get; set; } = string.Empty;

    [BsonElement("avatarUrl")]
    public string AvatarUrl { get; set; } = string.Empty;

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("isActive")]
    public bool IsActive { get; set; } = true;
}

public class Project
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("description")]
    public string Description { get; set; } = string.Empty;

    [BsonElement("status")]
    public string Status { get; set; } = "Active"; // Active | OnHold | Completed | Cancelled

    [BsonElement("createdBy")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string CreatedBy { get; set; } = string.Empty;

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("dueDate")]
    public DateTime? DueDate { get; set; }

    [BsonElement("requirementsText")]
    public string RequirementsText { get; set; } = string.Empty;

    [BsonElement("requirementsFileName")]
    public string RequirementsFileName { get; set; } = string.Empty;

    [BsonElement("aiAnalyzed")]
    public bool AiAnalyzed { get; set; } = false;

    [BsonElement("projectSummary")]
    public string ProjectSummary { get; set; } = string.Empty;

    [BsonElement("techStack")]
    public List<string> TechStack { get; set; } = new();

    [BsonElement("totalEstimatedHours")]
    public int TotalEstimatedHours { get; set; }

    [BsonElement("priority")]
    public string Priority { get; set; } = "Medium"; // Low | Medium | High

    [BsonElement("clientName")]
    public string ClientName { get; set; } = string.Empty;
}

public class TaskItem
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("projectId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string ProjectId { get; set; } = string.Empty;

    [BsonElement("title")]
    public string Title { get; set; } = string.Empty;

    [BsonElement("description")]
    public string Description { get; set; } = string.Empty;

    [BsonElement("category")]
    public string Category { get; set; } = "Full Stack"; // Frontend | Backend | Full Stack | Testing | DevOps | Design

    [BsonElement("skillsRequired")]
    public List<string> SkillsRequired { get; set; } = new();

    [BsonElement("estimatedHours")]
    public int EstimatedHours { get; set; }

    [BsonElement("priority")]
    public string Priority { get; set; } = "Medium"; // Low | Medium | High

    [BsonElement("complexity")]
    public string Complexity { get; set; } = "Medium";

    [BsonElement("status")]
    public string Status { get; set; } = "Pending"; // Pending | Assigned | InProgress | Completed | Cancelled

    [BsonElement("assignedDeveloperId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? AssignedDeveloperId { get; set; }

    [BsonElement("assignedDeveloperName")]
    public string? AssignedDeveloperName { get; set; }

    [BsonElement("aiRecommendations")]
    public List<AiRecommendation> AiRecommendations { get; set; } = new();

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("completedAt")]
    public DateTime? CompletedAt { get; set; }
}

public class AiRecommendation
{
    [BsonElement("developerId")]
    public string DeveloperId { get; set; } = string.Empty;

    [BsonElement("developerName")]
    public string DeveloperName { get; set; } = string.Empty;

    [BsonElement("score")]
    public double Score { get; set; }

    [BsonElement("matchedSkills")]
    public List<string> MatchedSkills { get; set; } = new();

    [BsonElement("missingSkills")]
    public List<string> MissingSkills { get; set; } = new();

    [BsonElement("reason")]
    public string Reason { get; set; } = string.Empty;
}

public class Assignment
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("taskId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string TaskId { get; set; } = string.Empty;

    [BsonElement("taskTitle")]
    public string TaskTitle { get; set; } = string.Empty;

    [BsonElement("projectId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string ProjectId { get; set; } = string.Empty;

    [BsonElement("projectName")]
    public string ProjectName { get; set; } = string.Empty;

    [BsonElement("developerId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string DeveloperId { get; set; } = string.Empty;

    [BsonElement("developerName")]
    public string DeveloperName { get; set; } = string.Empty;

    [BsonElement("assignedBy")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string AssignedBy { get; set; } = string.Empty;

    [BsonElement("assignedByName")]
    public string AssignedByName { get; set; } = string.Empty;

    [BsonElement("assignedAt")]
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("status")]
    public string Status { get; set; } = "Assigned"; // Assigned | InProgress | Completed | Cancelled | Reassigned

    [BsonElement("notes")]
    public string Notes { get; set; } = string.Empty;

    [BsonElement("aiAssisted")]
    public bool AiAssisted { get; set; } = false;

    [BsonElement("aiScore")]
    public double? AiScore { get; set; }

    [BsonElement("history")]
    public List<AssignmentHistoryEntry> History { get; set; } = new();
}

public class AssignmentHistoryEntry
{
    [BsonElement("action")]
    public string Action { get; set; } = string.Empty; // Assigned | Reassigned | StatusChanged | Completed

    [BsonElement("fromDeveloperId")]
    public string? FromDeveloperId { get; set; }

    [BsonElement("fromDeveloperName")]
    public string? FromDeveloperName { get; set; }

    [BsonElement("toDeveloperId")]
    public string? ToDeveloperId { get; set; }

    [BsonElement("toDeveloperName")]
    public string? ToDeveloperName { get; set; }

    [BsonElement("performedBy")]
    public string PerformedBy { get; set; } = string.Empty;

    [BsonElement("performedAt")]
    public DateTime PerformedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("reason")]
    public string Reason { get; set; } = string.Empty;
}
