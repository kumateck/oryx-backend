using APP.Repository;
using DOMAIN.Entities.Tickets;
using Xunit;

namespace APP.Tests.Repository;

public class TicketAccessPolicyTests
{
    private readonly Guid reporterId = Guid.NewGuid();
    private readonly Guid assigneeId = Guid.NewGuid();

    [Fact]
    public void ReporterHasDirectAccess()
    {
        var ticket = CreateTicket();

        Assert.True(TicketAccessPolicy.HasDirectAccess(ticket, reporterId));
    }

    [Fact]
    public void AssigneeHasDirectAccess()
    {
        var ticket = CreateTicket();

        Assert.True(TicketAccessPolicy.HasDirectAccess(ticket, assigneeId));
    }

    [Fact]
    public void UnrelatedUserDoesNotHaveDirectAccess()
    {
        var ticket = CreateTicket();

        Assert.False(TicketAccessPolicy.HasDirectAccess(ticket, Guid.NewGuid()));
    }

    private Ticket CreateTicket() => new()
    {
        ReportedById = reporterId,
        AssignedToId = assigneeId,
    };
}
