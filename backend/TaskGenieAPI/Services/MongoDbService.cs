using MongoDB.Driver;
using TaskGenieAPI.Models;
using TaskGenieAPI.Settings;
using Microsoft.Extensions.Options;

namespace TaskGenieAPI.Services;

public class MongoDbService
{
    private readonly IMongoDatabase _database;

    public MongoDbService(IOptions<MongoDbSettings> settings)
    {
        var client = new MongoClient(settings.Value.ConnectionString);
        _database = client.GetDatabase(settings.Value.DatabaseName);
        EnsureIndexes();
    }

    // ── Collections ───────────────────────────────────────────────────────────

    public IMongoCollection<User> Users =>
        _database.GetCollection<User>("users");

    public IMongoCollection<Developer> Developers =>
        _database.GetCollection<Developer>("developers");

    public IMongoCollection<Project> Projects =>
        _database.GetCollection<Project>("projects");

    public IMongoCollection<TaskItem> Tasks =>
        _database.GetCollection<TaskItem>("tasks");

    public IMongoCollection<Assignment> Assignments =>
        _database.GetCollection<Assignment>("assignments");

    // ── Index Setup ───────────────────────────────────────────────────────────

    private void EnsureIndexes()
    {
        // Users: unique email index
        var userEmailIndex = Builders<User>.IndexKeys.Ascending(u => u.Email);
        Users.Indexes.CreateOne(new CreateIndexModel<User>(
            userEmailIndex, new CreateIndexOptions { Unique = true }));

        // Developers: email index
        var devEmailIndex = Builders<Developer>.IndexKeys.Ascending(d => d.Email);
        Developers.Indexes.CreateOne(new CreateIndexModel<Developer>(devEmailIndex));

        // Tasks: projectId index for efficient queries
        var taskProjectIndex = Builders<TaskItem>.IndexKeys.Ascending(t => t.ProjectId);
        Tasks.Indexes.CreateOne(new CreateIndexModel<TaskItem>(taskProjectIndex));

        // Tasks: status index for dashboard queries
        var taskStatusIndex = Builders<TaskItem>.IndexKeys.Ascending(t => t.Status);
        Tasks.Indexes.CreateOne(new CreateIndexModel<TaskItem>(taskStatusIndex));

        // Assignments: taskId + developerId indexes
        var assignTaskIndex = Builders<Assignment>.IndexKeys.Ascending(a => a.TaskId);
        Assignments.Indexes.CreateOne(new CreateIndexModel<Assignment>(assignTaskIndex));

        var assignDevIndex = Builders<Assignment>.IndexKeys.Ascending(a => a.DeveloperId);
        Assignments.Indexes.CreateOne(new CreateIndexModel<Assignment>(assignDevIndex));
    }
}
