using APP.Extensions;
using APP.IRepository;
using APP.Services.NotificationService;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.Alerts;
using DOMAIN.Entities.Notifications;
using DOMAIN.Entities.Users;
using INFRASTRUCTURE.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public partial class AlertRepository(ApplicationDbContext context, IMapper mapper, UserManager<User> userManager, INotificationService notificationService) : IAlertRepository
{
    public async Task<Result<Guid>> CreateAlert(CreateAlertRequest request)
    {
        var alert = mapper.Map<Alert>(request);
        alert.Roles.AddRange(request.RoleIds.Select(item => new AlertRole
        {
            RoleId = item,
        }));
        alert.Users.AddRange(request.UserIds.Select(item => new AlertUser
        {
            UserId = item,
        }));
        await context.Alerts.AddAsync(alert);
        await context.SaveChangesAsync();
        return alert.Id;
    }

    public async Task<Result<AlertDto>> GetAlert(Guid alertId)
    {
        var alert = await context.Alerts
            .AsSplitQuery()
            .Include(a => a.Roles).ThenInclude(r => r.Role)
            .Include(a => a.Users).ThenInclude(u => u.User)
            .FirstOrDefaultAsync(item => item.Id == alertId);

        return mapper.Map<AlertDto>(alert);
    }

    public async Task<Result<Paginateable<IEnumerable<AlertDto>>>> GetAlerts(int page, int pageSize, string searchQuery, bool withDisabled = false)
    {
        var query = context.Alerts
            .AsSplitQuery()
            .OrderByDescending(a => a.CreatedAt)
            .Include(a => a.Roles).ThenInclude(r => r.Role)
            .Include(a => a.Users).ThenInclude(u => u.User)
            .AsQueryable();

        if (!withDisabled)
        {
            query = query.Where(item => !item.IsDisabled);
        }

        if (!string.IsNullOrEmpty(searchQuery))
        {

            query = query.WhereSearch(searchQuery, q => q.Title, q => q.NotificationType.ToString());
        }

        return await PaginationHelper.GetPaginatedResultAsync(query, page, pageSize, mapper.Map<AlertDto>);
    }

    public async Task<Result> UpdateAlert(CreateAlertRequest request, Guid userId, Guid alertId)
    {
        var alert = await context.Alerts
            .AsSplitQuery()
            .Include(alert => alert.Roles)
            .Include(alert => alert.Users).FirstOrDefaultAsync(alert => alert.Id == alertId);
        if (alert == null) return Result.Success();

        if (!alert.IsConfigurable) return Error.Validation("Alert.IsConfigurable", "This alert is not configurable");

        context.AlertRoles.RemoveRange(alert.Roles);
        context.AlertUsers.RemoveRange(alert.Users);

        if (request.RoleIds.Count != 0)
        {
            alert.Roles.AddRange(request.RoleIds.Select(r => new AlertRole
            {
                RoleId = r
            }));
        }

        if (request.UserIds.Count != 0)
        {
            alert.Users.AddRange(request.UserIds.Select(u => new AlertUser
            {
                UserId = u
            }));
        }

        alert.Title = request.Title;
        alert.TimeFrame = request.TimeFrame;
        alert.AlertTypes = request.AlertTypes;
        context.Alerts.Update(alert);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> ToggleDisable(Guid id)
    {
        var alert = await context.Alerts.FirstOrDefaultAsync(item => item.Id == id);
        if (alert != null)
        {
            alert.IsDisabled = !alert.IsDisabled;
            context.Alerts.Update(alert);
            await context.SaveChangesAsync();
        }
        return Result.Success();
    }

    public async Task<Result> DeleteAlert(Guid id, Guid userId)
    {
        var alert = await context.Alerts.FirstOrDefaultAsync(item => item.Id == id);
        if (alert != null)
        {
            if (!alert.IsConfigurable) return Error.Validation("Alert.IsConfigurable", "You cannot delete a non configurable alert");
            alert.DeletedAt = DateTime.UtcNow;
            alert.LastDeletedById = userId;
            context.Alerts.Update(alert);
            await context.SaveChangesAsync();
        }
        return Result.Success();
    }

    public Task ProcessAlert(string message, NotificationType notificationType, Guid? departmentId, List<User> assignedUsers = null, Guid? shipmentDocumentId = null, string relatedEntityType = null, Guid? relatedEntityId = null, string relatedEntityCode = null, string relatedEntityName = null, string relatedEntityStatus = null, string relatedEntityPriority = null) =>
        new AlertProcessor(context, userManager, notificationService).Process(message, notificationType, departmentId, assignedUsers, shipmentDocumentId, relatedEntityType, relatedEntityId, relatedEntityCode, relatedEntityName, relatedEntityStatus, relatedEntityPriority);
}
