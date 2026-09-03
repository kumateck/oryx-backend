using DOMAIN.Entities.Attachments;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Departments;
using DOMAIN.Entities.Sites;
using DOMAIN.Entities.Users;

namespace DOMAIN.Entities.Tickets;

public class TicketDto : WithAttachment
{
    public string TicketNumber { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public TicketCategory Category { get; set; }
    public TicketPriority Priority { get; set; }
    public TicketStatus Status { get; set; }

    public UserDto ReportedBy { get; set; }
    public UserDto AssignedTo { get; set; }
    public UserDto AssignedBy { get; set; }
    public DateTime? AssignedAt { get; set; }

    public DepartmentListDto Department { get; set; }
    public SiteDto Site { get; set; }

    public DateTime? DueDate { get; set; }
    public bool IsOverdue => DueDate.HasValue && DateTime.UtcNow > DueDate.Value
        && Status != TicketStatus.Done && Status != TicketStatus.Closed;

    public DateTime? DoneAt { get; set; }
    public UserDto DoneBy { get; set; }

    public DateTime? ClosedAt { get; set; }
    public UserDto ClosedBy { get; set; }

    public int ReopenCount { get; set; }
}

public class TicketActivityDto : BaseDto
{
    public UserDto User { get; set; }
    public TicketActivityType Type { get; set; }
    public string Note { get; set; }
    public TicketStatus? OldStatus { get; set; }
    public TicketStatus? NewStatus { get; set; }
}
