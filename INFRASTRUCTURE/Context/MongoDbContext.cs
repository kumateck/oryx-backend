using DOMAIN.Entities.ActivityLogs;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;

namespace INFRASTRUCTURE.Context;

public class MongoDbContext
{
    private readonly IMongoDatabase _database;

    public MongoDbContext(IConfiguration configuration)
    {
        var connectionString = Environment.GetEnvironmentVariable("MongoDbConnection");
        
        var client = new MongoClient(connectionString);
        _database = client.GetDatabase("OryxLogs");
    }

    // Example MongoDB collections as properties
    public IMongoCollection<ActivityLog> ActivityLogs => _database.GetCollection<ActivityLog>("activity_logs");
}
