using APP.Extensions;
using APP.Utils;
using DOMAIN.Entities.Tickets;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public partial class TicketRepository
{
    public async Task<Result<Paginateable<IEnumerable<TicketDto>>>> GetTickets(
        Guid currentUserId,
        int page,
        int pageSize,
        string searchQuery = null,
        TicketStatus? status = null,
        TicketPriority? priority = null,
        TicketCategory? category = null,
        Guid? assignedToId = null,
        Guid? reportedById = null)
    {
        var query = context.Tickets
            .AsSplitQuery()
            .Include(ticket => ticket.ReportedBy)
            .Include(ticket => ticket.AssignedTo)
            .Include(ticket => ticket.AssignedBy)
            .Include(ticket => ticket.DoneBy)
            .Include(ticket => ticket.ClosedBy)
            .Include(ticket => ticket.Department)
            .Include(ticket => ticket.Site)
            .AsQueryable();

        var canViewAll = await UserHasPermissionAsync(
            currentUserId,
            PermissionKeys.CanViewAllTickets);
        if (!canViewAll)
        {
            query = query.Where(ticket =>
                ticket.ReportedById == currentUserId ||
                ticket.AssignedToId == currentUserId);
        }

        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.WhereSearch(
                searchQuery,
                ticket => ticket.TicketNumber,
                ticket => ticket.Title,
                ticket => ticket.Description);
        }

        if (status.HasValue) query = query.Where(ticket => ticket.Status == status.Value);
        if (priority.HasValue) query = query.Where(ticket => ticket.Priority == priority.Value);
        if (category.HasValue) query = query.Where(ticket => ticket.Category == category.Value);
        if (assignedToId.HasValue) query = query.Where(ticket => ticket.AssignedToId == assignedToId.Value);
        if (reportedById.HasValue) query = query.Where(ticket => ticket.ReportedById == reportedById.Value);

        return await PaginationHelper.GetPaginatedResultAsync(query, page, pageSize, MapTicket);
    }

    public async Task<Result<TicketDto>> GetTicket(Guid id, Guid currentUserId)
    {
        var ticket = await context.Tickets
            .AsSplitQuery()
            .Include(item => item.ReportedBy)
            .Include(item => item.AssignedTo)
            .Include(item => item.AssignedBy)
            .Include(item => item.DoneBy)
            .Include(item => item.ClosedBy)
            .Include(item => item.Department)
            .Include(item => item.Site)
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id);

        if (ticket is null || !await CanAccessTicketAsync(ticket, currentUserId))
            return Error.NotFound("Ticket.NotFound", $"Ticket with ID '{id}' not found");

        return MapTicket(ticket);
    }

    public async Task<Result<IEnumerable<TicketActivityDto>>> GetTicketActivity(
        Guid id,
        Guid currentUserId)
    {
        var ticket = await context.Tickets
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id);
        if (ticket is null || !await CanAccessTicketAsync(ticket, currentUserId))
            return Error.NotFound("Ticket.NotFound", $"Ticket with ID '{id}' not found");

        var activities = await context.TicketActivities
            .Include(activity => activity.User)
            .Where(activity => activity.TicketId == id)
            .OrderBy(activity => activity.CreatedAt)
            .ToListAsync();

        return mapper.Map<List<TicketActivityDto>>(activities);
    }

    private async Task<bool> CanAccessTicketAsync(Ticket ticket, Guid userId)
    {
        if (TicketAccessPolicy.HasDirectAccess(ticket, userId)) return true;
        return await UserHasPermissionAsync(userId, PermissionKeys.CanViewAllTickets);
    }

    private async Task<bool> UserHasPermissionAsync(Guid userId, string permissionKey)
    {
        var roleIds = await context.UserRoles.IgnoreQueryFilters()
            .Where(role => role.UserId == userId)
            .Select(role => role.RoleId)
            .ToListAsync();

        var claimIds = await context.RoleClaims.IgnoreQueryFilters()
            .Where(claim => roleIds.Contains(claim.RoleId)
                && claim.ClaimType == AppConstants.Permission
                && claim.ClaimValue == permissionKey)
            .Select(claim => claim.Id)
            .ToListAsync();

        if (claimIds.Count == 0) return false;
        return await context.PermissionTypes.AnyAsync(permission =>
            claimIds.Contains(permission.RoleClaimId) &&
            permission.Key == permissionKey);
    }

    private TicketDto MapTicket(Ticket ticket) =>
        mapper.Map<TicketDto>(ticket, options =>
            options.Items[AppConstants.ModelType] = nameof(Ticket));
}
