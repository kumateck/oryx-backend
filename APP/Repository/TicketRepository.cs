using APP.IRepository;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.Notifications;
using DOMAIN.Entities.Tickets;
using DOMAIN.Entities.Users;
using INFRASTRUCTURE.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public partial class TicketRepository(
    ApplicationDbContext context,
    IMapper mapper,
    UserManager<User> userManager,
    IAlertRepository alertRepository)
    : ITicketRepository
{
    public async Task<Result<Guid>> CreateTicket(CreateTicketRequest request, Guid reportedById, Guid? departmentId)
    {
        var reporter = await userManager.FindByIdAsync(reportedById.ToString());
        if (reporter is null) return Error.Validation("User.Invalid", "User Invalid");

        if (request.SiteId.HasValue)
        {
            var siteExists = await context.Sites.AnyAsync(s => s.Id == request.SiteId.Value);
            if (!siteExists) return Error.Validation("Site.Invalid", "Invalid site");
        }

        var ticketCount = await context.Tickets.IgnoreQueryFilters().CountAsync();

        var ticket = mapper.Map<Ticket>(request);
        ticket.TicketNumber = $"IT-{ticketCount + 1:D6}";
        ticket.ReportedById = reportedById;
        ticket.DepartmentId = departmentId;
        ticket.DueDate = ComputeDueDate(request.Priority, DateTime.UtcNow);

        await context.Tickets.AddAsync(ticket);
        await context.SaveChangesAsync();

        await LogActivity(ticket.Id, reportedById, TicketActivityType.StatusChange,
            "Ticket created", null, TicketStatus.Open);

        await alertRepository.ProcessAlert(
            $"New IT support ticket {ticket.TicketNumber} was raised: {ticket.Title}",
            NotificationType.TicketCreated, departmentId);

        return ticket.Id;
    }

    public async Task<Result> AssignTicket(Guid id, Guid assignedToId, Guid assignedById)
    {
        var ticket = await context.Tickets.FirstOrDefaultAsync(t => t.Id == id);
        if (ticket is null) return Error.NotFound("Ticket.NotFound", $"Ticket with ID '{id}' not found");

        if (ticket.Status is TicketStatus.Done or TicketStatus.Closed)
            return Error.Validation("Ticket.InvalidStatus",
                "Cannot assign a ticket that is done or closed. Reopen it first.");

        var assignee = await userManager.FindByIdAsync(assignedToId.ToString());
        if (assignee is null) return Error.Validation("User.Invalid", "Invalid assignee");

        var oldStatus = ticket.Status;
        ticket.AssignedToId = assignedToId;
        ticket.AssignedById = assignedById;
        ticket.AssignedAt = DateTime.UtcNow;
        if (ticket.Status == TicketStatus.Open) ticket.Status = TicketStatus.Assigned;

        context.Tickets.Update(ticket);
        await context.SaveChangesAsync();

        await LogActivity(ticket.Id, assignedById, TicketActivityType.Assignment,
            $"Assigned to {assignee.FirstName} {assignee.LastName}", oldStatus, ticket.Status);

        await alertRepository.ProcessAlert(
            $"Ticket {ticket.TicketNumber} was assigned to you: {ticket.Title}",
            NotificationType.TicketAssigned, ticket.DepartmentId, [assignee]);

        return Result.Success();
    }

    public async Task<Result> StartProgress(Guid id, Guid userId)
    {
        var ticket = await context.Tickets.FirstOrDefaultAsync(t => t.Id == id);
        if (ticket is null) return Error.NotFound("Ticket.NotFound", $"Ticket with ID '{id}' not found");

        if (ticket.AssignedToId != userId)
            return Error.Validation("Ticket.Unauthorized", "Only the assigned agent can start work on this ticket.");

        if (ticket.Status != TicketStatus.Assigned)
            return Error.Validation("Ticket.InvalidStatus", "Ticket must be assigned before work can start.");

        var oldStatus = ticket.Status;
        ticket.Status = TicketStatus.InProgress;
        context.Tickets.Update(ticket);
        await context.SaveChangesAsync();

        await LogActivity(ticket.Id, userId, TicketActivityType.StatusChange,
            "Started work on ticket", oldStatus, ticket.Status);

        return Result.Success();
    }

    public async Task<Result> MarkDone(Guid id, Guid userId)
    {
        var ticket = await context.Tickets.FirstOrDefaultAsync(t => t.Id == id);
        if (ticket is null) return Error.NotFound("Ticket.NotFound", $"Ticket with ID '{id}' not found");

        if (ticket.AssignedToId != userId)
            return Error.Validation("Ticket.Unauthorized", "Only the assigned agent can mark this ticket done.");

        if (ticket.Status is not (TicketStatus.Assigned or TicketStatus.InProgress))
            return Error.Validation("Ticket.InvalidStatus", "Ticket must be in progress to be marked done.");

        var oldStatus = ticket.Status;
        ticket.Status = TicketStatus.Done;
        ticket.DoneAt = DateTime.UtcNow;
        ticket.DoneById = userId;
        context.Tickets.Update(ticket);
        await context.SaveChangesAsync();

        await LogActivity(ticket.Id, userId, TicketActivityType.StatusChange,
            "Marked as done", oldStatus, ticket.Status);

        var reporter = await context.Users.FirstOrDefaultAsync(u => u.Id == ticket.ReportedById);
        await alertRepository.ProcessAlert(
            $"Ticket {ticket.TicketNumber} was marked done, awaiting your review: {ticket.Title}",
            NotificationType.TicketStatusChanged, ticket.DepartmentId, reporter != null ? [reporter] : null);

        return Result.Success();
    }

    public async Task<Result> CloseTicket(Guid id, Guid userId)
    {
        var ticket = await context.Tickets.FirstOrDefaultAsync(t => t.Id == id);
        if (ticket is null) return Error.NotFound("Ticket.NotFound", $"Ticket with ID '{id}' not found");

        if (ticket.Status != TicketStatus.Done)
            return Error.Validation("Ticket.InvalidStatus", "Only a ticket marked done can be closed.");

        var oldStatus = ticket.Status;
        ticket.Status = TicketStatus.Closed;
        ticket.ClosedAt = DateTime.UtcNow;
        ticket.ClosedById = userId;
        context.Tickets.Update(ticket);
        await context.SaveChangesAsync();

        await LogActivity(ticket.Id, userId, TicketActivityType.StatusChange,
            "Closed ticket", oldStatus, ticket.Status);

        var reporter = await context.Users.FirstOrDefaultAsync(u => u.Id == ticket.ReportedById);
        await alertRepository.ProcessAlert(
            $"Ticket {ticket.TicketNumber} was closed: {ticket.Title}",
            NotificationType.TicketStatusChanged, ticket.DepartmentId, reporter != null ? [reporter] : null);

        return Result.Success();
    }

    public async Task<Result> ReopenTicket(Guid id, string reason, Guid userId)
    {
        var ticket = await context.Tickets.FirstOrDefaultAsync(t => t.Id == id);
        if (ticket is null) return Error.NotFound("Ticket.NotFound", $"Ticket with ID '{id}' not found");

        if (ticket.Status is not (TicketStatus.Done or TicketStatus.Closed))
            return Error.Validation("Ticket.InvalidStatus", "Only a done or closed ticket can be reopened.");

        var isReporter = ticket.ReportedById == userId;
        var canClose = await UserHasPermissionAsync(userId, PermissionKeys.CanCloseTicket);
        if (!isReporter && !canClose)
            return Error.Validation("Ticket.Unauthorized", "Only the reporter or a supervisor can reopen this ticket.");

        var oldStatus = ticket.Status;
        ticket.Status = TicketStatus.InProgress;
        ticket.ReopenCount += 1;
        context.Tickets.Update(ticket);
        await context.SaveChangesAsync();

        await LogActivity(ticket.Id, userId, TicketActivityType.Reopened, reason, oldStatus, ticket.Status);

        if (ticket.AssignedToId.HasValue)
        {
            var assignee = await context.Users.FirstOrDefaultAsync(u => u.Id == ticket.AssignedToId.Value);
            await alertRepository.ProcessAlert(
                $"Ticket {ticket.TicketNumber} was reopened: {reason}",
                NotificationType.TicketStatusChanged, ticket.DepartmentId, assignee != null ? [assignee] : null);
        }

        return Result.Success();
    }

    public async Task<Result<Guid>> AddComment(Guid id, string comment, Guid userId)
    {
        var ticket = await context.Tickets.FirstOrDefaultAsync(t => t.Id == id);
        if (ticket is null) return Error.NotFound("Ticket.NotFound", $"Ticket with ID '{id}' not found");

        var activity = await LogActivity(ticket.Id, userId, TicketActivityType.Comment, comment, null, null);

        var recipients = new List<Guid> { ticket.ReportedById };
        if (ticket.AssignedToId.HasValue) recipients.Add(ticket.AssignedToId.Value);
        recipients.Remove(userId);

        if (recipients.Count != 0)
        {
            var users = await context.Users.Where(u => recipients.Contains(u.Id)).ToListAsync();
            await alertRepository.ProcessAlert(
                $"New comment on ticket {ticket.TicketNumber}: {ticket.Title}",
                NotificationType.TicketCommentAdded, ticket.DepartmentId, users);
        }

        return activity.Id;
    }

    private async Task<TicketActivity> LogActivity(Guid ticketId, Guid userId, TicketActivityType type,
        string note, TicketStatus? oldStatus, TicketStatus? newStatus)
    {
        var activity = new TicketActivity
        {
            TicketId = ticketId,
            UserId = userId,
            Type = type,
            Note = note,
            OldStatus = oldStatus,
            NewStatus = newStatus
        };
        await context.TicketActivities.AddAsync(activity);
        await context.SaveChangesAsync();
        return activity;
    }

    private static DateTime? ComputeDueDate(TicketPriority priority, DateTime from) => priority switch
    {
        TicketPriority.Critical => from.AddHours(4),
        TicketPriority.High => from.AddDays(1),
        TicketPriority.Medium => from.AddDays(3),
        TicketPriority.Low => from.AddDays(7),
        _ => null
    };
}
