using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using TaskGenieAPI.Models;
using TaskGenieAPI.Services;

namespace TaskGenieAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AssignmentsController : ControllerBase
{
    private readonly MongoDbService _db;
    private readonly ILogger<AssignmentsController> _logger;

    public AssignmentsController(MongoDbService db, ILogger<AssignmentsController> logger)
    {
        _db = db;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? developerId, [FromQuery] string? projectId,
        [FromQuery] string? status)
    {
        var filter = Builders<Assignment>.Filter.Empty;

        if (!string.IsNullOrEmpty(developerId))
            filter &= Builders<Assignment>.Filter.Eq(a => a.DeveloperId, developerId);
        if (!string.IsNullOrEmpty(projectId))
            filter &= Builders<Assignment>.Filter.Eq(a => a.ProjectId, projectId);
        if (!string.IsNullOrEmpty(status))
            filter &= Builders<Assignment>.Filter.Eq(a => a.Status, status);

        var assignments = await _db.Assignments.Find(filter)
            .SortByDescending(a => a.AssignedAt)
            .ToListAsync();

        return Ok(assignments);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var assignment = await _db.Assignments.Find(a => a.Id == id).FirstOrDefaultAsync();
        if (assignment == null) return NotFound();
        return Ok(assignment);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAssignmentRequest request)
    {
        var task = await _db.Tasks.Find(t => t.Id == request.TaskId).FirstOrDefaultAsync();
        if (task == null) return NotFound(new { message = "Task not found." });

        var developer = await _db.Developers.Find(d => d.Id == request.DeveloperId && d.IsActive)
            .FirstOrDefaultAsync();
        if (developer == null) return NotFound(new { message = "Developer not found." });

        if (developer.Availability == "OnLeave")
            return BadRequest(new { message = "Developer is on leave and cannot be assigned." });

        var project = await _db.Projects.Find(p => p.Id == task.ProjectId).FirstOrDefaultAsync();

        var assignedByUserId = User.FindFirst("userId")?.Value ?? "";
        var assignedByName = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value ?? "";

        var assignment = new Assignment
        {
            TaskId = request.TaskId,
            TaskTitle = task.Title,
            ProjectId = task.ProjectId,
            ProjectName = project?.Name ?? "",
            DeveloperId = request.DeveloperId,
            DeveloperName = developer.Name,
            AssignedBy = assignedByUserId,
            AssignedByName = assignedByName,
            Notes = request.Notes,
            AiAssisted = request.AiAssisted,
            AiScore = request.AiScore,
            History = new List<AssignmentHistoryEntry>
            {
                new()
                {
                    Action = "Assigned",
                    ToDeveloperId = developer.Id,
                    ToDeveloperName = developer.Name,
                    PerformedBy = assignedByName,
                    Reason = request.Notes,
                }
            }
        };

        await _db.Assignments.InsertOneAsync(assignment);

        // Update task status and assigned developer
        var taskUpdate = Builders<TaskItem>.Update
            .Set(t => t.Status, "Assigned")
            .Set(t => t.AssignedDeveloperId, developer.Id)
            .Set(t => t.AssignedDeveloperName, developer.Name)
            .Set(t => t.UpdatedAt, DateTime.UtcNow);
        await _db.Tasks.UpdateOneAsync(t => t.Id == request.TaskId, taskUpdate);

        // Update developer workload (+10 per task, capped at 100)
        var newWorkload = Math.Min(developer.CurrentWorkload + 10, 100);
        var devUpdate = Builders<Developer>.Update.Set(d => d.CurrentWorkload, newWorkload);
        if (newWorkload >= 80)
            devUpdate = Builders<Developer>.Update.Combine(
                devUpdate,
                Builders<Developer>.Update.Set(d => d.Availability, "Busy"));
        await _db.Developers.UpdateOneAsync(d => d.Id == request.DeveloperId, devUpdate);

        _logger.LogInformation("Task {TaskId} assigned to developer {DeveloperName} by {AssignedBy}",
            request.TaskId, developer.Name, assignedByName);

        return CreatedAtAction(nameof(GetById), new { id = assignment.Id }, assignment);
    }

    [HttpPost("{id}/reassign")]
    public async Task<IActionResult> Reassign(string id, [FromBody] ReassignRequest request)
    {
        var assignment = await _db.Assignments.Find(a => a.Id == id).FirstOrDefaultAsync();
        if (assignment == null) return NotFound();

        var newDev = await _db.Developers.Find(d => d.Id == request.NewDeveloperId && d.IsActive)
            .FirstOrDefaultAsync();
        if (newDev == null) return NotFound(new { message = "New developer not found." });

        if (newDev.Availability == "OnLeave")
            return BadRequest(new { message = "New developer is on leave." });

        var performedByName = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value ?? "";

        // Add history entry
        var historyEntry = new AssignmentHistoryEntry
        {
            Action = "Reassigned",
            FromDeveloperId = assignment.DeveloperId,
            FromDeveloperName = assignment.DeveloperName,
            ToDeveloperId = newDev.Id,
            ToDeveloperName = newDev.Name,
            PerformedBy = performedByName,
            Reason = request.Reason,
        };

        // Reduce old developer workload
        var oldDev = await _db.Developers.Find(d => d.Id == assignment.DeveloperId).FirstOrDefaultAsync();
        if (oldDev != null)
        {
            var reducedWorkload = Math.Max(oldDev.CurrentWorkload - 10, 0);
            var oldDevUpdate = Builders<Developer>.Update
                .Set(d => d.CurrentWorkload, reducedWorkload);
            if (reducedWorkload < 80 && oldDev.Availability == "Busy")
                oldDevUpdate = Builders<Developer>.Update.Combine(
                    oldDevUpdate,
                    Builders<Developer>.Update.Set(d => d.Availability, "Available"));
            await _db.Developers.UpdateOneAsync(d => d.Id == assignment.DeveloperId, oldDevUpdate);
        }

        // Update assignment
        var assignmentUpdate = Builders<Assignment>.Update
            .Set(a => a.DeveloperId, newDev.Id!)
            .Set(a => a.DeveloperName, newDev.Name)
            .Set(a => a.Status, "Assigned")
            .Push(a => a.History, historyEntry);
        await _db.Assignments.UpdateOneAsync(a => a.Id == id, assignmentUpdate);

        // Update task
        var taskUpdate = Builders<TaskItem>.Update
            .Set(t => t.AssignedDeveloperId, newDev.Id)
            .Set(t => t.AssignedDeveloperName, newDev.Name)
            .Set(t => t.UpdatedAt, DateTime.UtcNow);
        await _db.Tasks.UpdateOneAsync(t => t.Id == assignment.TaskId, taskUpdate);

        // Increase new developer workload
        var newWorkload = Math.Min(newDev.CurrentWorkload + 10, 100);
        await _db.Developers.UpdateOneAsync(d => d.Id == request.NewDeveloperId,
            Builders<Developer>.Update.Set(d => d.CurrentWorkload, newWorkload));

        return Ok(new { message = "Task reassigned successfully.", newDeveloper = newDev.Name });
    }

    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateStatus(string id, [FromBody] object body)
    {
        using var doc = System.Text.Json.JsonDocument.Parse(body.ToString()!);
        var status = doc.RootElement.TryGetProperty("status", out var s) ? s.GetString() : null;
        if (string.IsNullOrEmpty(status)) return BadRequest(new { message = "Status required." });

        var result = await _db.Assignments.UpdateOneAsync(
            a => a.Id == id,
            Builders<Assignment>.Update.Set(a => a.Status, status));

        if (result.MatchedCount == 0) return NotFound();

        // Sync task status
        var assignment = await _db.Assignments.Find(a => a.Id == id).FirstOrDefaultAsync();
        if (assignment != null)
        {
            var taskStatus = status switch
            {
                "InProgress" => "InProgress",
                "Completed" => "Completed",
                "Cancelled" => "Pending",
                _ => "Assigned",
            };
            await _db.Tasks.UpdateOneAsync(t => t.Id == assignment.TaskId,
                Builders<TaskItem>.Update
                    .Set(t => t.Status, taskStatus)
                    .Set(t => t.UpdatedAt, DateTime.UtcNow));
        }

        return Ok(new { message = "Status updated." });
    }
}
