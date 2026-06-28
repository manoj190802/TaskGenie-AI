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
            Priority = request.Priority,
            ClientName = request.ClientName,
            CreatedBy = userId,
        };

        await _db.Projects.InsertOneAsync(project);
        _logger.LogInformation("Project created: {Name} by {UserId}", project.Name, userId);
        return CreatedAtAction(nameof(GetById), new { id = project.Id }, project);
    }

    // ── Create + Upload + Analyze in one wizard step ────────────────────────────
    [HttpPost("create-with-upload")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> CreateWithUpload(
        [FromForm] string name,
        [FromForm] string? description,
        [FromForm] string? priority,
        [FromForm] string? clientName,
        [FromForm] string? dueDate,
        IFormFile? file)
    {
        if (string.IsNullOrWhiteSpace(name))
            return BadRequest(new { message = "Project name is required." });

        var userId = User.FindFirst("userId")?.Value ?? "";

        // 1. Create the project
        var project = new Project
        {
            Name = name.Trim(),
            Description = description?.Trim() ?? string.Empty,
            Priority = priority ?? "Medium",
            ClientName = clientName?.Trim() ?? string.Empty,
            DueDate = DateTime.TryParse(dueDate, out var dt) ? dt : (DateTime?)null,
            CreatedBy = userId,
        };
        await _db.Projects.InsertOneAsync(project);
        _logger.LogInformation("Project created via wizard: {Name}", project.Name);

        string? extractedText = null;
        string? fileName = null;
        int wordCount = 0;
        string? uploadError = null;

        // 2. Upload & extract text if file provided
        if (file != null && file.Length > 0)
        {
            var allowedExts = new[] { ".pdf", ".docx", ".doc", ".txt" };
            var ext = Path.GetExtension(file.FileName).ToLower();

            if (!allowedExts.Contains(ext))
            {
                uploadError = "Unsupported file type. Use PDF, DOCX, DOC, or TXT.";
            }
            else if (file.Length > 10 * 1024 * 1024)
            {
                uploadError = "File too large. Maximum size is 10 MB.";
            }
            else
            {
                try
                {
                    using var ms = new MemoryStream();
                    await file.CopyToAsync(ms);
                    var fileBytes = ms.ToArray();

                    var (text, _, wc) = await _python.ExtractTextAsync(fileBytes, file.FileName);
                    extractedText = text;
                    fileName = file.FileName;
                    wordCount = wc;

                    var docUpdate = Builders<Project>.Update
                        .Set(p => p.RequirementsText, text)
                        .Set(p => p.RequirementsFileName, file.FileName)
                        .Set(p => p.UpdatedAt, DateTime.UtcNow);
                    await _db.Projects.UpdateOneAsync(p => p.Id == project.Id, docUpdate);
                    project.RequirementsText = text;
                    project.RequirementsFileName = file.FileName;
                }
                catch (Exception ex)
                {
                    uploadError = $"Text extraction failed: {ex.Message}";
                }
            }
        }

        // 3. Run AI analysis if text extracted
        List<object> createdTasks = new();
        string? projectSummary = null;
        List<string> techStack = new();
        int totalHours = 0;
        string? analyzeError = null;

        if (!string.IsNullOrWhiteSpace(extractedText))
        {
            try
            {
                var analysis = await _python.AnalyzeRequirementsAsync(extractedText, name);
                projectSummary = analysis.ProjectSummary;
                techStack = analysis.TechStackDetected;
                totalHours = analysis.TotalEstimatedHours;

                var projectUpdate = Builders<Project>.Update
                    .Set(p => p.AiAnalyzed, true)
                    .Set(p => p.ProjectSummary, analysis.ProjectSummary)
                    .Set(p => p.TechStack, analysis.TechStackDetected)
                    .Set(p => p.TotalEstimatedHours, analysis.TotalEstimatedHours)
                    .Set(p => p.UpdatedAt, DateTime.UtcNow);
                await _db.Projects.UpdateOneAsync(p => p.Id == project.Id, projectUpdate);

                // Fetch all available developers for matching
                var allDevs = await _db.Developers
                    .Find(d => d.IsActive && d.Availability != "OnLeave")
                    .ToListAsync();

                foreach (var taskDto in analysis.Tasks)
                {
                    var task = new TaskItem
                    {
                        ProjectId = project.Id!,
                        Title = taskDto.Title,
                        Description = taskDto.Description,
                        Category = taskDto.Category,
                        SkillsRequired = taskDto.SkillsRequired,
                        EstimatedHours = taskDto.EstimatedHours,
                        Priority = taskDto.Priority,
                        Complexity = taskDto.Complexity,
                        Status = "Pending",
                    };

                    // Score developers for this task
                    if (allDevs.Count > 0)
                    {
                        var scored = allDevs.Select(dev =>
                        {
                            var skillMatch = task.SkillsRequired.Count == 0 ? 1.0
                                : (double)task.SkillsRequired.Count(s =>
                                    dev.Skills.Any(ds => ds.Equals(s, StringComparison.OrdinalIgnoreCase)))
                                    / task.SkillsRequired.Count;
                            var expScore = Math.Min(dev.ExperienceYears / 10.0, 1.0);
                            var availScore = dev.Availability == "Available" ? 1.0 : 0.5;
                            var workloadScore = 1.0 - (dev.CurrentWorkload / 100.0);
                            var total = skillMatch * 0.40 + expScore * 0.25 + availScore * 0.20 + workloadScore * 0.15;

                            return new AiRecommendation
                            {
                                DeveloperId = dev.Id!,
                                DeveloperName = dev.Name,
                                Score = Math.Round(total, 3),
                                MatchedSkills = task.SkillsRequired
                                    .Where(s => dev.Skills.Any(ds => ds.Equals(s, StringComparison.OrdinalIgnoreCase)))
                                    .ToList(),
                                MissingSkills = task.SkillsRequired
                                    .Where(s => !dev.Skills.Any(ds => ds.Equals(s, StringComparison.OrdinalIgnoreCase)))
                                    .ToList(),
                                Reason = $"Skill match {skillMatch:P0}, {dev.ExperienceYears}yr exp, {dev.Availability}",
                            };
                        })
                        .OrderByDescending(r => r.Score)
                        .ToList();

                        task.AiRecommendations = scored;
                    }

                    await _db.Tasks.InsertOneAsync(task);
                    createdTasks.Add(new
                    {
                        task.Id, task.Title, task.Description, task.Category,
                        task.SkillsRequired, task.EstimatedHours, task.Priority,
                        task.Complexity, task.Status, task.AiRecommendations,
                    });
                }

                _logger.LogInformation("Wizard analysis complete for {ProjectId}: {Count} tasks", project.Id, createdTasks.Count);
            }
            catch (Exception ex)
            {
                analyzeError = $"AI analysis failed: {ex.Message}";
                _logger.LogError(ex, "Wizard AI analysis failed for {ProjectId}", project.Id);
            }
        }

        return Ok(new
        {
            projectId = project.Id,
            project,
            uploadStatus = uploadError == null ? "success" : "error",
            uploadError,
            fileName,
            wordCount,
            analyzeStatus = analyzeError == null && createdTasks.Count > 0 ? "success" : (analyzeError != null ? "error" : "skipped"),
            analyzeError,
            projectSummary,
            techStack,
            totalEstimatedHours = totalHours,
            tasks = createdTasks,
        });
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
        if (request.Priority != null) updates.Add(Builders<Project>.Update.Set(p => p.Priority, request.Priority));
        if (request.ClientName != null) updates.Add(Builders<Project>.Update.Set(p => p.ClientName, request.ClientName));
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
