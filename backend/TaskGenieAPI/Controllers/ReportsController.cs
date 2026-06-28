using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using TaskGenieAPI.Models;
using TaskGenieAPI.Services;

namespace TaskGenieAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly MongoDbService _db;

    public ReportsController(MongoDbService db) => _db = db;

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboardStats()
    {
        var allProjects = await _db.Projects.Find(_ => true).ToListAsync();
        var allTasks = await _db.Tasks.Find(_ => true).ToListAsync();
        var allDevelopers = await _db.Developers.Find(d => d.IsActive).ToListAsync();
        var allAssignments = await _db.Assignments.Find(_ => true).ToListAsync();

        var recentAssignments = allAssignments
            .OrderByDescending(a => a.AssignedAt)
            .Take(10)
            .Select(a => new RecentAssignmentDto
            {
                AssignmentId = a.Id!,
                TaskTitle = a.TaskTitle,
                ProjectName = a.ProjectName,
                DeveloperName = a.DeveloperName,
                AssignedAt = a.AssignedAt,
                Status = a.Status,
            }).ToList();

        var tasksByCategory = allTasks
            .GroupBy(t => t.Category)
            .Select(g => new CategoryBreakdownDto { Category = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToList();

        return Ok(new DashboardStatsDto
        {
            TotalProjects = allProjects.Count,
            ActiveProjects = allProjects.Count(p => p.Status == "Active"),
            TotalTasks = allTasks.Count,
            PendingTasks = allTasks.Count(t => t.Status == "Pending"),
            AssignedTasks = allTasks.Count(t => t.Status == "Assigned"),
            InProgressTasks = allTasks.Count(t => t.Status == "InProgress"),
            CompletedTasks = allTasks.Count(t => t.Status == "Completed"),
            TotalDevelopers = allDevelopers.Count,
            AvailableDevelopers = allDevelopers.Count(d => d.Availability == "Available"),
            TotalAssignments = allAssignments.Count,
            RecentAssignments = recentAssignments,
            TasksByCategory = tasksByCategory,
        });
    }

    [HttpGet("developer-workload")]
    public async Task<IActionResult> GetDeveloperWorkload()
    {
        var developers = await _db.Developers.Find(d => d.IsActive).ToListAsync();
        var assignments = await _db.Assignments.Find(_ => true).ToListAsync();

        var result = developers.Select(dev =>
        {
            var devAssignments = assignments.Where(a => a.DeveloperId == dev.Id).ToList();
            return new
            {
                developer = new { dev.Id, dev.Name, dev.Availability, dev.CurrentWorkload, dev.Skills },
                totalAssignments = devAssignments.Count,
                activeAssignments = devAssignments.Count(a => a.Status is "Assigned" or "InProgress"),
                completedAssignments = devAssignments.Count(a => a.Status == "Completed"),
            };
        }).OrderByDescending(x => x.activeAssignments);

        return Ok(result);
    }

    [HttpGet("task-summary")]
    public async Task<IActionResult> GetTaskSummary([FromQuery] string? projectId)
    {
        var filter = Builders<TaskItem>.Filter.Empty;
        if (!string.IsNullOrEmpty(projectId))
            filter = Builders<TaskItem>.Filter.Eq(t => t.ProjectId, projectId);

        var tasks = await _db.Tasks.Find(filter).ToListAsync();

        return Ok(new
        {
            total = tasks.Count,
            byStatus = tasks.GroupBy(t => t.Status)
                .ToDictionary(g => g.Key, g => g.Count()),
            byCategory = tasks.GroupBy(t => t.Category)
                .ToDictionary(g => g.Key, g => g.Count()),
            byPriority = tasks.GroupBy(t => t.Priority)
                .ToDictionary(g => g.Key, g => g.Count()),
            totalEstimatedHours = tasks.Sum(t => t.EstimatedHours),
            aiAssistedAssignments = await _db.Assignments.CountDocumentsAsync(a => a.AiAssisted),
        });
    }

    [HttpGet("assignment-history")]
    public async Task<IActionResult> GetAssignmentHistory(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var total = await _db.Assignments.CountDocumentsAsync(_ => true);
        var assignments = await _db.Assignments.Find(_ => true)
            .SortByDescending(a => a.AssignedAt)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync();

        return Ok(new
        {
            data = assignments,
            total,
            page,
            pageSize,
            totalPages = (int)Math.Ceiling((double)total / pageSize),
        });
    }

    [HttpGet("ai-stats")]
    public async Task<IActionResult> GetAiStats()
    {
        var total = await _db.Assignments.CountDocumentsAsync(_ => true);
        var aiAssisted = await _db.Assignments.CountDocumentsAsync(a => a.AiAssisted);

        var aiAssignments = await _db.Assignments
            .Find(a => a.AiAssisted && a.AiScore.HasValue)
            .ToListAsync();

        var avgScore = aiAssignments.Any()
            ? aiAssignments.Average(a => a.AiScore!.Value) : 0;

        return Ok(new
        {
            totalAssignments = total,
            aiAssistedCount = aiAssisted,
            manualCount = total - aiAssisted,
            aiAdoptionRate = total > 0 ? (double)aiAssisted / total * 100 : 0,
            averageAiScore = Math.Round(avgScore, 4),
        });
    }
}
