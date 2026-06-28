using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using TaskGenieAPI.Services;
using TaskGenieAPI.Settings;

Mongo2Go.MongoDbRunner? runner = null;
try
{
    runner = Mongo2Go.MongoDbRunner.Start();
    Console.WriteLine($"🚀 Started in-memory MongoDB runner on: {runner.ConnectionString}");
}
catch (Exception ex)
{
    Console.WriteLine($"⚠️ Could not start in-memory MongoDB runner: {ex.Message}");
}

var builder = WebApplication.CreateBuilder(args);

// ── Configuration ─────────────────────────────────────────────────────────────
builder.Services.Configure<MongoDbSettings>(settings =>
{
    if (runner != null)
    {
        settings.ConnectionString = runner.ConnectionString;
    }
    else
    {
        settings.ConnectionString = builder.Configuration.GetSection("MongoDbSettings:ConnectionString").Value ?? "mongodb://localhost:27017";
    }
    settings.DatabaseName = builder.Configuration.GetSection("MongoDbSettings:DatabaseName").Value ?? "taskgenie";
});
builder.Services.Configure<JwtSettings>(
    builder.Configuration.GetSection("JwtSettings"));
builder.Services.Configure<PythonAIServiceSettings>(
    builder.Configuration.GetSection("PythonAIService"));

// ── Services ──────────────────────────────────────────────────────────────────
builder.Services.AddSingleton<MongoDbService>();
builder.Services.AddSingleton<JwtService>();
builder.Services.AddHttpClient<PythonBridgeService>();

// ── Auth ──────────────────────────────────────────────────────────────────────
var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>()!;
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSettings.Secret)),
            ValidateIssuer = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtSettings.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
        };
    });

builder.Services.AddAuthorization();

// ── CORS ──────────────────────────────────────────────────────────────────────
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? new[] { "http://localhost:4200" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// ── Controllers & JSON ────────────────────────────────────────────────────────
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Use camelCase (standard REST convention) so Angular models align correctly
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });

builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

// ── Middleware ────────────────────────────────────────────────────────────────
app.UseHttpsRedirection();
app.UseCors("AllowAngular");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// ── Seed default admin user ───────────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<MongoDbService>();
    var adminExists = await db.Users
        .Find(u => u.Role == "Admin")
        .AnyAsync();

    if (!adminExists)
    {
        await db.Users.InsertOneAsync(new TaskGenieAPI.Models.User
        {
            Name = "System Admin",
            Email = "admin@taskgenie.ai",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
            Role = "Admin",
        });
        Console.WriteLine("✅ Default admin created: admin@taskgenie.ai / Admin@123");
    }
}

app.Lifetime.ApplicationStopping.Register(() =>
{
    runner?.Dispose();
    Console.WriteLine("🛑 In-memory MongoDB runner stopped");
});

app.Run();
