using DOMAIN.Entities.ActivityLogs;
using MongoDB.Driver;

namespace INFRASTRUCTURE.Context;

public class MongoDbContext
{
    private readonly IMongoDatabase _database;

    public MongoDbContext()
    {
        var connectionString = Environment.GetEnvironmentVariable("MONGO_DB_CONNECTION_STRING")
                               ?? "mongodb://root:SNsiilzMRr9UCzz@164.90.142.68:27018";

        var client = new MongoClient(connectionString);
        var environment = Environment.GetEnvironmentVariable("Environment");
        var dbName = environment == "dev" ? "logs" : "demo_logs";
        _database = client.GetDatabase(dbName);
    }

    public IMongoCollection<ActivityLog> ActivityLogs => _database.GetCollection<ActivityLog>("activity_logs");
}