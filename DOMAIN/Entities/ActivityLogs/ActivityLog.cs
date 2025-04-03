using DOMAIN.Entities.Users;

namespace DOMAIN.Entities.ActivityLogs;

public class CreateActivityLog
{
    public Guid UserId { get; set; }
    public string Action { get; set; }
    public string Module { get; set; }
    public string SubModule { get; set; }
    public ActionType ActionType { get; set; }
    public string IpAddress { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class ActivityLog 
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; }
    public string Action { get; set; }
    public string Module { get; set; }
    public string SubModule { get; set; }
    public ActionType ActionType { get; set; }
    public string IpAddress { get; set; }
    public DateTime CreatedAt { get; set; }
}


public class ActivityLogDto
{
    public Guid Id { get; set; }
    public UserDto User { get; set; }
    public string Action { get; set; }
    public string Module { get; set; }
    public string SubModule { get; set; }
    public ActionType ActionType { get; set; }
    public string IpAddress { get; set; }
    public DateTime CreatedAt { get; set; }
}

public enum ActionType
{
    Create,
    Read,
    Update,
    Delete
}
