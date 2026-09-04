using DOMAIN.Entities.Tickets;

namespace APP.Repository;

public static class TicketAccessPolicy
{
    public static bool HasDirectAccess(Ticket ticket, Guid userId) =>
        ticket.ReportedById == userId || ticket.AssignedToId == userId;
}
