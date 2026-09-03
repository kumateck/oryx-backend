using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Departments;
using DOMAIN.Entities.Sites;
using DOMAIN.Entities.Users;

namespace DOMAIN.Entities.Tickets;

public class Ticket : BaseEntity
{
    [StringLength(50)] public string TicketNumber { get; set; }
    [StringLength(255)] public string Title { get; set; }
    [StringLength(2000)] public string Description { get; set; }
    public TicketCategory Category { get; set; }
    public TicketPriority Priority { get; set; } = TicketPriority.Medium;
    public TicketStatus Status { get; set; } = TicketStatus.Open;

    public Guid ReportedById { get; set; }
    public User ReportedBy { get; set; }

    public Guid? AssignedToId { get; set; }
    public User AssignedTo { get; set; }
    public Guid? AssignedById { get; set; }
    public User AssignedBy { get; set; }
    public DateTime? AssignedAt { get; set; }

    public Guid? DepartmentId { get; set; }
    public Department Department { get; set; }
    public Guid? SiteId { get; set; }
    public Site Site { get; set; }

    public DateTime? DueDate { get; set; }

    public DateTime? DoneAt { get; set; }
    public Guid? DoneById { get; set; }
    public User DoneBy { get; set; }

    public DateTime? ClosedAt { get; set; }
    public Guid? ClosedById { get; set; }
    public User ClosedBy { get; set; }

    public int ReopenCount { get; set; }

    public List<TicketActivity> Activities { get; set; } = [];
}

public enum TicketStatus
{
    Open,
    Assigned,
    InProgress,
    Done,
    Closed
}

public enum TicketPriority
{
    Low,
    Medium,
    High,
    Critical
}

public enum TicketCategory
{
    Hardware,
    Software,
    Network,
    AccountAccess,
    Other
}
