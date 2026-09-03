using System.ComponentModel.DataAnnotations;

namespace DOMAIN.Entities.Tickets;

public class CreateTicketRequest
{
    [Required, StringLength(255)]
    public string Title { get; set; }

    [Required, StringLength(2000)]
    public string Description { get; set; }

    [Required]
    public TicketCategory Category { get; set; }

    public TicketPriority Priority { get; set; } = TicketPriority.Medium;

    public Guid? SiteId { get; set; }
}

public class AssignTicketRequest
{
    [Required] public Guid AssignedToId { get; set; }
}

public class ReopenTicketRequest
{
    [Required, StringLength(1000)]
    public string Reason { get; set; }
}

public class AddTicketCommentRequest
{
    [Required, StringLength(2000)]
    public string Comment { get; set; }
}
