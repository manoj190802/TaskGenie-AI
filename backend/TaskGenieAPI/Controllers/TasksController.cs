using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using TaskGenieAPI.Models;
using TaskGenieAPI.Services;

namespace TaskGenieAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TasksController : ControllerBase
{
    private readonly MongoDbService _db;
    private readonly PythonBridgeService _python;
    private readonly ILogger<TasksController> _logger;

    public TasksController(MongoDbService db, PythonBridgeService python, ILogger<TasksController> logger)
    {
        _db = db;
        _python = python;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? projectId, [FromQuery] string? status,
        [FromQuery] string? category)
    {
        var filter = Builders<TaskItem>.Filter.Empty;

        if (!string.IsNullOrEmpty(projectId))
            filter &= Builders<TaskItem>.Filter.Eq(t => t.ProjectId, projectId);
        if (!string.IsNullOrEmpty(status))
            filter &= Builders<TaskItem>.Filter.Eq(t => t.Status, status);
        if (!string.IsNullOrEmpty(category))
            filter &= Builders<TaskItem>.Filter.Eq(t => t.Category, category);

        var tasks = await _db.Tasks.Find(filter)
            .SortByDescending(t => t.CreatedAt)
            .ToListAsync();

        return Ok(tasks);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var task = await _db.Tasks.Find(t => t.Id == id).FirstOrDefaultAsync();
        if (task == null) return NotFound(new { message = "Task not found." });
        return Ok(task);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTaskRequest request)
    {
        var project = await _db.Projects.Find(p => p.Id == request.ProjectId).FirstOrDefaultAsync();
        if (project == null)
            return BadRequest(new { message = "Project not found." });

        var task = new TaskItem
        {
            ProjectId = request.ProjectId,
            Title = request.Title,
            Description = request.Description,
            Category = request.Category,
            SkillsRequired = request.SkillsRequired,
            EstimatedHours = request.EstimatedHours,
            Priority = request.Priority,
            Complexity = request.Complexity,
        };

        await _db.Tasks.InsertOneAsync(task);
        return CreatedAtAction(nameof(GetById), new { id = task.Id }, task);
    }

    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateStatus(string id, [FromBody] UpdateTaskStatusRequest request)
    {
        var task = await _db.Tasks.Find(t => t.Id == id).FirstOrDefaultAsync();
        if (task == null) return NotFound();

        var updates = new List<UpdateDefinition<TaskItem>>
        {
            Builders<TaskItem>.Update.Set(t => t.Status, request.Status),
            Builders<TaskItem>.Update.Set(t => t.UpdatedAt, DateTime.UtcNow),
        };

        if (request.Status == "Completed")
            updates.Add(Builders<TaskItem>.Update.Set(t => t.CompletedAt, DateTime.UtcNow));

        var combined = Builders<TaskItem>.Update.Combine(updates);
        await _db.Tasks.UpdateOneAsync(t => t.Id == id, combined);

        // Also update assignment status if exists
        if (request.Status == "Completed")
        {
            await _db.Assignments.UpdateOneAsync(
                a => a.TaskId == id && a.Status != "Cancelled",
                Builders<Assignment>.Update.Set(a => a.Status, "Completed"));
        }

        return Ok(new { message = "Status updated." });
    }

    [HttpPost("{id}/recommend")]
    public async Task<IActionResult> GetRecommendations(string id)
    {
        var task = await _db.Tasks.Find(t => t.Id == id).FirstOrDefaultAsync();
        if (task == null) return NotFound();

        // Get all active, non-on-leave developers
        var developers = await _db.Developers
            .Find(d => d.IsActive)
            .ToListAsync();

        if (!developers.Any())
            return Ok(new { recommendations = new List<object>(), message = "No developers available." });

        try
        {
            var taskDto = new AnalyzedTaskDto
            {
                Title = task.Title,
                Description = task.Description,
                Category = task.Category,
                SkillsRequired = task.SkillsRequired,
                EstimatedHours = task.EstimatedHours,
                Priority = task.Priority,
                Complexity = task.Complexity,
            };

            var devProfiles = developers.Select(d => new DeveloperProfileDto
            {
                Id = d.Id!,
                Name = d.Name,
                Skills = d.Skills,
                ExperienceYears = d.ExperienceYears,
                Availability = d.Availability,
                CurrentWorkload = d.CurrentWorkload,
            }).ToList();

            var matchResult = await _python.MatchDevelopersAsync(taskDto, devProfiles);

            // Store top 3 recommendations in the task
            var topRecs = matchResult.Recommendations.Take(3).Select(r => new AiRecommendation
            {
                DeveloperId = r.DeveloperId,
                DeveloperName = r.DeveloperName,
                Score = r.Score,
                MatchedSkills = r.MatchedSkills,
                MissingSkills = r.MissingSkills,
                Reason = r.RecommendationReason,
            }).ToList();

            await _db.Tasks.UpdateOneAsync(
                t => t.Id == id,
                Builders<TaskItem>.Update.Set(t => t.AiRecommendations, topRecs));

            return Ok(matchResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get recommendations for task {TaskId}", id);
            return StatusCode(502, new { message = "Recommendation service unavailable.", detail = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(string id)
    {
        var result = await _db.Tasks.DeleteOneAsync(t => t.Id == id);
        if (result.DeletedCount == 0) return NotFound();
        return Ok(new { message = "Task deleted." });
    }
}
