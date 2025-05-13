using DOMAIN.Entities.Users;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace DOMAIN.Entities.ActivityLogs;

public class CreateActivityLog
{
    public Guid? UserId { get; set; }
    public UserDto User { get; set; }
    public string Action { get; set; }
    public string Module { get; set; }
    public string SubModule { get; set; }
    public ActionType ActionType { get; set; }
    public string IpAddress { get; set; }
    public string Url { get; set; }
    public string HttpMethod { get; set; }
    public int StatusCode { get; set; }
    public long ExecutionTimeMs { get; set; }
    public string UserAgent { get; set; }
    public string QueryParams { get; set; }
    public string Headers { get; set; }
    public string Payload { get; set; }
    public string ResponsePayload { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class ActivityLog
{
    [BsonElement("id")]
    public ObjectId Id { get; set; }
    public Guid? UserId { get; set; }
    public UserDto User { get; set; }
    public string Action { get; set; }
    public string Module { get; set; }
    public string SubModule { get; set; }
    public ActionType ActionType { get; set; }
    public string IpAddress { get; set; }
    public string Url { get; set; }
    public string HttpMethod { get; set; }
    public int StatusCode { get; set; }
    public long ExecutionTimeMs { get; set; }
    public string UserAgent { get; set; }
    public string QueryParams { get; set; }
    public string Headers { get; set; }
    public string Payload { get; set; }
    public string ResponsePayload { get; set; }
    public DateTime CreatedAt { get; set; }
}


public class ActivityLogDto
{
    public UserDto User { get; set; }
    public string Action { get; set; }
    public string Module { get; set; }
    public string SubModule { get; set; }
    public ActionType ActionType { get; set; }
    public string IpAddress { get; set; }
    public string Url { get; set; }
    public string HttpMethod { get; set; }
    public int StatusCode { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class PrevStateCaptureRequest
{
    public string Method { get; init; }
    public string Model { get; init; }
    public string IpAddress { get; init; }
    public string UserId { get; init; }
    public string RequestBody { get; init; }
}

public enum ActionType
{
    Create,
    Read,
    Update,
    Delete
}
