using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using TaskGenieAPI.Models;
using TaskGenieAPI.Services;
using BCrypt.Net;

namespace TaskGenieAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly MongoDbService _db;
    private readonly JwtService _jwt;
    private readonly ILogger<AuthController> _logger;

    public AuthController(MongoDbService db, JwtService jwt, ILogger<AuthController> logger)
    {
        _db = db;
        _jwt = jwt;
        _logger = logger;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        // Validate role
        var allowedRoles = new[] { "Admin", "ProjectManager" };
        if (!allowedRoles.Contains(request.Role))
            return BadRequest(new { message = "Invalid role. Use Admin or ProjectManager." });

        // Check if email exists
        var existing = await _db.Users
            .Find(u => u.Email == request.Email.ToLower())
            .FirstOrDefaultAsync();

        if (existing != null)
            return Conflict(new { message = "Email already registered." });

        var user = new User
        {
            Name = request.Name,
            Email = request.Email.ToLower(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = request.Role,
        };

        await _db.Users.InsertOneAsync(user);
        _logger.LogInformation("New user registered: {Email} as {Role}", user.Email, user.Role);

        var token = _jwt.GenerateToken(user);
        return Ok(new AuthResponse
        {
            Token = token,
            UserId = user.Id!,
            Name = user.Name,
            Email = user.Email,
            Role = user.Role,
            ExpiresAt = _jwt.GetExpiry(),
        });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await _db.Users
            .Find(u => u.Email == request.Email.ToLower() && u.IsActive)
            .FirstOrDefaultAsync();

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return Unauthorized(new { message = "Invalid email or password." });

        var token = _jwt.GenerateToken(user);
        _logger.LogInformation("User logged in: {Email}", user.Email);

        return Ok(new AuthResponse
        {
            Token = token,
            UserId = user.Id!,
            Name = user.Name,
            Email = user.Email,
            Role = user.Role,
            ExpiresAt = _jwt.GetExpiry(),
        });
    }

    [HttpGet("me")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<IActionResult> GetMe()
    {
        var userId = User.FindFirst("userId")?.Value;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var user = await _db.Users.Find(u => u.Id == userId).FirstOrDefaultAsync();
        if (user == null) return NotFound();

        return Ok(new { user.Id, user.Name, user.Email, user.Role, user.CreatedAt });
    }
}
