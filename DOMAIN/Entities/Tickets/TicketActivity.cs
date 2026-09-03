using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Users;

namespace DOMAIN.Entities.Tickets;

/// <summary>
/// Single feed of everything that happens on a ticket: comments, assignments and
/// status transitions. The ticket detail page filters this feed client-side into
/// a comment thread and a separate status-history timeline.
/// </summary>
public class TicketActivity : BaseEntity
{
    public Guid TicketId { get; set; }
    public Ticket Ticket { get; set; }

    public Guid UserId { get; set; }
    public User User { get; set; }

    public TicketActivityType Type { get; set; }

    [StringLength(2000)] public string Note { get; set; }

    public TicketStatus? OldStatus { get; set; }
    public TicketStatus? NewStatus { get; set; }
}

public enum TicketActivityType
{
    Comment,
    StatusChange,
    Assignment,
    Reopened
}
