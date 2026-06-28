using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using TaskGenieAPI.Models;
using TaskGenieAPI.Services;

namespace TaskGenieAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DevelopersController : ControllerBase
{
    private readonly MongoDbService _db;
    private readonly ILogger<DevelopersController> _logger;

    public DevelopersController(MongoDbService db, ILogger<DevelopersController> logger)
    {
        _db = db;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? availability, [FromQuery] string? skill)
    {
        var filter = Builders<Developer>.Filter.Eq(d => d.IsActive, true);

        if (!string.IsNullOrEmpty(availability))
            filter &= Builders<Developer>.Filter.Eq(d => d.Availability, availability);

        if (!string.IsNullOrEmpty(skill))
            filter &= Builders<Developer>.Filter.AnyEq(d => d.Skills, skill);

        var developers = await _db.Developers.Find(filter)
            .SortBy(d => d.Name)
            .ToListAsync();

        return Ok(developers);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var dev = await _db.Developers.Find(d => d.Id == id && d.IsActive).FirstOrDefaultAsync();
        if (dev == null) return NotFound(new { message = "Developer not found." });
        return Ok(dev);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateDeveloperRequest request)
    {
        // Check duplicate email
        var existing = await _db.Developers.Find(d => d.Email == request.Email.ToLower()).FirstOrDefaultAsync();
        if (existing != null)
            return Conflict(new { message = "Developer with this email already exists." });

        var dev = new Developer
        {
            Name = request.Name,
            Email = request.Email.ToLower(),
            Skills = request.Skills,
            ExperienceYears = request.ExperienceYears,
            Availability = request.Availability,
            CurrentWorkload = request.CurrentWorkload,
            Bio = request.Bio,
        };

        await _db.Developers.InsertOneAsync(dev);
        _logger.LogInformation("Developer created: {Name}", dev.Name);
        return CreatedAtAction(nameof(GetById), new { id = dev.Id }, dev);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateDeveloperRequest request)
    {
        var dev = await _db.Developers.Find(d => d.Id == id && d.IsActive).FirstOrDefaultAsync();
        if (dev == null) return NotFound(new { message = "Developer not found." });

        var updates = new List<UpdateDefinition<Developer>>();
        if (request.Name != null) updates.Add(Builders<Developer>.Update.Set(d => d.Name, request.Name));
        if (request.Skills != null) updates.Add(Builders<Developer>.Update.Set(d => d.Skills, request.Skills));
        if (request.ExperienceYears.HasValue) updates.Add(Builders<Developer>.Update.Set(d => d.ExperienceYears, request.ExperienceYears.Value));
        if (request.Availability != null) updates.Add(Builders<Developer>.Update.Set(d => d.Availability, request.Availability));
        if (request.CurrentWorkload.HasValue) updates.Add(Builders<Developer>.Update.Set(d => d.CurrentWorkload, request.CurrentWorkload.Value));
        if (request.Bio != null) updates.Add(Builders<Developer>.Update.Set(d => d.Bio, request.Bio));

        if (!updates.Any()) return Ok(dev);

        var combined = Builders<Developer>.Update.Combine(updates);
        await _db.Developers.UpdateOneAsync(d => d.Id == id, combined);

        var updated = await _db.Developers.Find(d => d.Id == id).FirstOrDefaultAsync();
        return Ok(updated);
    }

    [HttpPatch("{id}/availability")]
    public async Task<IActionResult> UpdateAvailability(string id, [FromBody] object body)
    {
        var dev = await _db.Developers.Find(d => d.Id == id && d.IsActive).FirstOrDefaultAsync();
        if (dev == null) return NotFound();

        // Parse body
        using var doc = System.Text.Json.JsonDocument.Parse(body.ToString()!);
        var availability = doc.RootElement.TryGetProperty("availability", out var av) ? av.GetString() : null;
        var workload = doc.RootElement.TryGetProperty("currentWorkload", out var wl) ? (int?)wl.GetInt32() : null;

        var updates = new List<UpdateDefinition<Developer>>();
        if (availability != null) updates.Add(Builders<Developer>.Update.Set(d => d.Availability, availability));
        if (workload.HasValue) updates.Add(Builders<Developer>.Update.Set(d => d.CurrentWorkload, workload.Value));

        if (updates.Any())
        {
            var combined = Builders<Developer>.Update.Combine(updates);
            await _db.Developers.UpdateOneAsync(d => d.Id == id, combined);
        }

        return Ok(new { message = "Availability updated." });
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(string id)
    {
        var update = Builders<Developer>.Update.Set(d => d.IsActive, false);
        var result = await _db.Developers.UpdateOneAsync(d => d.Id == id, update);
        if (result.MatchedCount == 0) return NotFound();
        return Ok(new { message = "Developer deactivated." });
    }

    [HttpGet("{id}/assignments")]
    public async Task<IActionResult> GetAssignments(string id)
    {
        var assignments = await _db.Assignments
            .Find(a => a.DeveloperId == id)
            .SortByDescending(a => a.AssignedAt)
            .ToListAsync();
        return Ok(assignments);
    }
}
