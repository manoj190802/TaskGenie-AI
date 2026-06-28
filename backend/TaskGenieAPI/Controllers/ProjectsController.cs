using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using TaskGenieAPI.Models;
using TaskGenieAPI.Services;

namespace TaskGenieAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProjectsController : ControllerBase
{
    private readonly MongoDbService _db;
    private readonly PythonBridgeService _python;
    private readonly ILogger<ProjectsController> _logger;

    public ProjectsController(MongoDbService db, PythonBridgeService python,
        ILogger<ProjectsController> logger)
    {
        _db = db;
        _python = python;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? status)
    {
        var filter = Builders<Project>.Filter.Empty;
        if (!string.IsNullOrEmpty(status))
            filter = Builders<Project>.Filter.Eq(p => p.Status, status);

        var projects = await _db.Projects.Find(filter)
            .SortByDescending(p => p.CreatedAt)
            .ToListAsync();

        // Enrich with task counts
        var projectIds = projects.Select(p => p.Id).ToList();
        var tasks = await _db.Tasks.Find(t => projectIds.Contains(t.ProjectId)).ToListAsync();

        var result = projects.Select(p => new
        {
            project = p,
            taskCounts = new
            {
                total = tasks.Count(t => t.ProjectId == p.Id),
                pending = tasks.Count(t => t.ProjectId == p.Id && t.Status == "Pending"),
                assigned = tasks.Count(t => t.ProjectId == p.Id && t.Status == "Assigned"),
                inProgress = tasks.Count(t => t.ProjectId == p.Id && t.Status == "InProgress"),
                completed = tasks.Count(t => t.ProjectId == p.Id && t.Status == "Completed"),
            }
        });

        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var project = await _db.Projects.Find(p => p.Id == id).FirstOrDefaultAsync();
        if (project == null) return NotFound(new { message = "Project not found." });

        var tasks = await _db.Tasks.Find(t => t.ProjectId == id).ToListAsync();
        return Ok(new { project, tasks });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProjectRequest request)
    {
        var userId = User.FindFirst("userId")?.Value ?? "";
        var project = new Project
        {
            Name = request.Name,
            Description = request.Description,
            DueDate = request.DueDate,
            CreatedBy = userId,
        };

        await _db.Projects.InsertOneAsync(project);
        _logger.LogInformation("Project created: {Name} by {UserId}", project.Name, userId);
        return CreatedAtAction(nameof(GetById), new { id = project.Id }, project);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateProjectRequest request)
    {
        var project = await _db.Projects.Find(p => p.Id == id).FirstOrDefaultAsync();
        if (project == null) return NotFound();

        var updates = new List<UpdateDefinition<Project>>();
        if (request.Name != null) updates.Add(Builders<Project>.Update.Set(p => p.Name, request.Name));
        if (request.Description != null) updates.Add(Builders<Project>.Update.Set(p => p.Description, request.Description));
        if (request.Status != null) updates.Add(Builders<Project>.Update.Set(p => p.Status, request.Status));
        if (request.DueDate.HasValue) updates.Add(Builders<Project>.Update.Set(p => p.DueDate, request.DueDate));
        updates.Add(Builders<Project>.Update.Set(p => p.UpdatedAt, DateTime.UtcNow));

        var combined = Builders<Project>.Update.Combine(updates);
        await _db.Projects.UpdateOneAsync(p => p.Id == id, combined);

        var updated = await _db.Projects.Find(p => p.Id == id).FirstOrDefaultAsync();
        return Ok(updated);
    }

    [HttpPost("{id}/upload-requirements")]
    public async Task<IActionResult> UploadRequirements(string id, IFormFile file)
    {
        var project = await _db.Projects.Find(p => p.Id == id).FirstOrDefaultAsync();
        if (project == null) return NotFound(new { message = "Project not found." });

        var allowedExts = new[] { ".pdf", ".docx", ".doc", ".txt" };
        var ext = Path.GetExtension(file.FileName).ToLower();
        if (!allowedExts.Contains(ext))
            return BadRequest(new { message = "Unsupported file type. Use PDF, DOCX, or TXT." });

        if (file.Length > 10 * 1024 * 1024)
            return BadRequest(new { message = "File too large. Max 10MB." });

        // Read file bytes
        using var ms = new MemoryStream();
        await file.CopyToAsync(ms);
        var fileBytes = ms.ToArray();

        try
        {
            // Call Python service to extract text
            var (text, pageCount, wordCount) = await _python.ExtractTextAsync(fileBytes, file.FileName);

            // Save to project
            var update = Builders<Project>.Update
                .Set(p => p.RequirementsText, text)
                .Set(p => p.RequirementsFileName, file.FileName)
                .Set(p => p.AiAnalyzed, false)
                .Set(p => p.UpdatedAt, DateTime.UtcNow);

            await _db.Projects.UpdateOneAsync(p => p.Id == id, update);

            return Ok(new
            {
                message = "Requirements uploaded and text extracted successfully.",
                fileName = file.FileName,
                wordCount,
                pageCount,
                textPreview = text.Length > 500 ? text[..500] + "..." : text,
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract text from requirements file for project {ProjectId}", id);
            return StatusCode(502, new { message = "Failed to extract text. Make sure the AI service is running.", detail = ex.Message });
        }
    }

    [HttpPost("{id}/analyze")]
    public async Task<IActionResult> AnalyzeRequirements(string id)
    {
        var project = await _db.Projects.Find(p => p.Id == id).FirstOrDefaultAsync();
        if (project == null) return NotFound();

        if (string.IsNullOrWhiteSpace(project.RequirementsText))
            return BadRequest(new { message = "No requirements text found. Please upload a requirements file first." });

        try
        {
            _logger.LogInformation("Starting AI analysis for project {ProjectId}", id);
            var analysis = await _python.AnalyzeRequirementsAsync(project.RequirementsText, project.Name);

            // Update project
            var projectUpdate = Builders<Project>.Update
                .Set(p => p.AiAnalyzed, true)
                .Set(p => p.ProjectSummary, analysis.ProjectSummary)
                .Set(p => p.TechStack, analysis.TechStackDetected)
                .Set(p => p.TotalEstimatedHours, analysis.TotalEstimatedHours)
                .Set(p => p.UpdatedAt, DateTime.UtcNow);

            await _db.Projects.UpdateOneAsync(p => p.Id == id, projectUpdate);

            // Create tasks from analysis
            var createdTasks = new List<TaskItem>();
            foreach (var taskDto in analysis.Tasks)
            {
                var task = new TaskItem
                {
                    ProjectId = id,
                    Title = taskDto.Title,
                    Description = taskDto.Description,
                    Category = taskDto.Category,
                    SkillsRequired = taskDto.SkillsRequired,
                    EstimatedHours = taskDto.EstimatedHours,
                    Priority = taskDto.Priority,
                    Complexity = taskDto.Complexity,
                    Status = "Pending",
                };
                await _db.Tasks.InsertOneAsync(task);
                createdTasks.Add(task);
            }

            _logger.LogInformation("AI analysis complete for project {ProjectId}: {TaskCount} tasks created",
                id, createdTasks.Count);

            return Ok(new
            {
                message = $"Analysis complete. {createdTasks.Count} tasks extracted.",
                summary = analysis.ProjectSummary,
                techStack = analysis.TechStackDetected,
                totalEstimatedHours = analysis.TotalEstimatedHours,
                tasks = createdTasks,
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI analysis failed for project {ProjectId}", id);
            return StatusCode(502, new { message = "AI analysis failed.", detail = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(string id)
    {
        var result = await _db.Projects.DeleteOneAsync(p => p.Id == id);
        if (result.DeletedCount == 0) return NotFound();
        // Clean up related tasks
        await _db.Tasks.DeleteManyAsync(t => t.ProjectId == id);
        return Ok(new { message = "Project deleted." });
    }
}
