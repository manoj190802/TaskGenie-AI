using System.Text.Json;
using Microsoft.Extensions.Options;
using TaskGenieAPI.Models;
using TaskGenieAPI.Settings;

namespace TaskGenieAPI.Services;

public class PythonBridgeService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PythonBridgeService> _logger;

    public PythonBridgeService(HttpClient httpClient,
        IOptions<PythonAIServiceSettings> settings,
        ILogger<PythonBridgeService> logger)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri(settings.Value.BaseUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(120);
        _logger = logger;
    }

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true,
    };

    // ── Text Extraction ───────────────────────────────────────────────────────

    public async Task<(string text, int? pageCount, int wordCount)> ExtractTextAsync(
        byte[] fileBytes, string fileName)
    {
        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(fileBytes);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(
            GetMimeType(fileName));
        content.Add(fileContent, "file", fileName);

        var response = await _httpClient.PostAsync("/extract-text", content);

        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync();
            _logger.LogError("Python service extract-text failed: {Error}", err);
            throw new Exception($"Text extraction failed: {err}");
        }

        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(json);

        return (
            result.GetProperty("text").GetString() ?? "",
            result.TryGetProperty("page_count", out var pc) && pc.ValueKind != JsonValueKind.Null
                ? pc.GetInt32() : null,
            result.GetProperty("word_count").GetInt32()
        );
    }

    // ── Requirement Analysis ──────────────────────────────────────────────────

    public async Task<AnalysisResultDto> AnalyzeRequirementsAsync(string text, string projectName)
    {
        var payload = new { text, project_name = projectName };
        var json = JsonSerializer.Serialize(payload, _jsonOptions);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("/analyze-requirements", content);

        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync();
            _logger.LogError("Python service analyze-requirements failed: {Error}", err);
            throw new Exception($"AI analysis failed: {err}");
        }

        var resultJson = await response.Content.ReadAsStringAsync();
        return DeserializeAnalysisResult(resultJson);
    }

    // ── Developer Matching ────────────────────────────────────────────────────

    public async Task<MatchResultDto> MatchDevelopersAsync(
        AnalyzedTaskDto task, List<DeveloperProfileDto> developers)
    {
        var payload = new
        {
            task = new
            {
                title = task.Title,
                description = task.Description,
                category = task.Category,
                skills_required = task.SkillsRequired,
                estimated_hours = task.EstimatedHours,
                priority = task.Priority,
                complexity = task.Complexity,
            },
            developers = developers.Select(d => new
            {
                id = d.Id,
                name = d.Name,
                skills = d.Skills,
                experience_years = d.ExperienceYears,
                availability = d.Availability,
                current_workload = d.CurrentWorkload,
            })
        };

        var json = JsonSerializer.Serialize(payload, _jsonOptions);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("/match-developers", content);

        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync();
            _logger.LogError("Python service match-developers failed: {Error}", err);
            throw new Exception($"Developer matching failed: {err}");
        }

        var resultJson = await response.Content.ReadAsStringAsync();
        return DeserializeMatchResult(resultJson);
    }

    // ── Health Check ──────────────────────────────────────────────────────────

    public async Task<bool> IsHealthyAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/health");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    // ── Deserialization Helpers ───────────────────────────────────────────────

    private static AnalysisResultDto DeserializeAnalysisResult(string json)
    {
        var root = JsonSerializer.Deserialize<JsonElement>(json);

        var tasks = new List<AnalyzedTaskDto>();
        if (root.TryGetProperty("tasks", out var tasksEl))
        {
            foreach (var t in tasksEl.EnumerateArray())
            {
                tasks.Add(new AnalyzedTaskDto
                {
                    Title = t.GetProperty("title").GetString() ?? "",
                    Description = t.GetProperty("description").GetString() ?? "",
                    Category = t.GetProperty("category").GetString() ?? "Full Stack",
                    SkillsRequired = t.TryGetProperty("skills_required", out var sr)
                        ? sr.EnumerateArray().Select(s => s.GetString() ?? "").ToList()
                        : new List<string>(),
                    EstimatedHours = t.TryGetProperty("estimated_hours", out var eh) ? eh.GetInt32() : 8,
                    Priority = t.TryGetProperty("priority", out var pr) ? pr.GetString() ?? "Medium" : "Medium",
                    Complexity = t.TryGetProperty("complexity", out var cx) ? cx.GetString() ?? "Medium" : "Medium",
                });
            }
        }

        return new AnalysisResultDto
        {
            ProjectSummary = root.TryGetProperty("project_summary", out var ps)
                ? ps.GetString() ?? "" : "",
            Tasks = tasks,
            TotalEstimatedHours = root.TryGetProperty("total_estimated_hours", out var th)
                ? th.GetInt32() : 0,
            TechStackDetected = root.TryGetProperty("tech_stack_detected", out var ts)
                ? ts.EnumerateArray().Select(s => s.GetString() ?? "").ToList()
                : new List<string>(),
        };
    }

    private static MatchResultDto DeserializeMatchResult(string json)
    {
        var root = JsonSerializer.Deserialize<JsonElement>(json);

        var recs = new List<DeveloperScoreDto>();
        if (root.TryGetProperty("recommendations", out var recsEl))
        {
            foreach (var r in recsEl.EnumerateArray())
            {
                recs.Add(DeserializeScore(r));
            }
        }

        DeveloperScoreDto? best = null;
        if (root.TryGetProperty("best_match", out var bm) && bm.ValueKind != JsonValueKind.Null)
            best = DeserializeScore(bm);

        return new MatchResultDto
        {
            TaskTitle = root.TryGetProperty("task_title", out var tt) ? tt.GetString() ?? "" : "",
            Recommendations = recs,
            BestMatch = best,
        };
    }

    private static DeveloperScoreDto DeserializeScore(JsonElement el) => new()
    {
        DeveloperId = el.TryGetProperty("developer_id", out var di) ? di.GetString() ?? "" : "",
        DeveloperName = el.TryGetProperty("developer_name", out var dn) ? dn.GetString() ?? "" : "",
        Score = el.TryGetProperty("score", out var sc) ? sc.GetDouble() : 0,
        SkillMatchScore = el.TryGetProperty("skill_match_score", out var sm) ? sm.GetDouble() : 0,
        ExperienceScore = el.TryGetProperty("experience_score", out var es) ? es.GetDouble() : 0,
        AvailabilityScore = el.TryGetProperty("availability_score", out var av) ? av.GetDouble() : 0,
        WorkloadScore = el.TryGetProperty("workload_score", out var ws) ? ws.GetDouble() : 0,
        MatchedSkills = el.TryGetProperty("matched_skills", out var ms)
            ? ms.EnumerateArray().Select(s => s.GetString() ?? "").ToList() : new(),
        MissingSkills = el.TryGetProperty("missing_skills", out var mis)
            ? mis.EnumerateArray().Select(s => s.GetString() ?? "").ToList() : new(),
        RecommendationReason = el.TryGetProperty("recommendation_reason", out var rr)
            ? rr.GetString() ?? "" : "",
    };

    private static string GetMimeType(string fileName) => Path.GetExtension(fileName).ToLower() switch
    {
        ".pdf" => "application/pdf",
        ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        ".doc" => "application/msword",
        ".txt" => "text/plain",
        _ => "application/octet-stream",
    };
}
