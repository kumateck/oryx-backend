using APP.Extensions;
using APP.IRepository;
using APP.Services.Notification;
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

public class AlertRepository(ApplicationDbContext context, IMapper mapper, UserManager<User> userManager, INotificationService notificationService) : IAlertRepository
{
    public async Task<Result<Guid>> CreateAlert(CreateAlertRequest request)
    {
        var alert = mapper.Map<Alert>(request);
        await context.Alerts.AddAsync(alert);
        await context.SaveChangesAsync();
        return alert.Id;
    }
    
    public async Task<Result<AlertDto>> GetAlert(Guid alertId)
    {
        return mapper.Map<AlertDto>(await context.Alerts
            .FirstOrDefaultAsync(item => item.Id == alertId));
    }

    public async Task<Result<Paginateable<IEnumerable<AlertDto>>>> GetAlerts(int page, int pageSize, string searchQuery, bool withDisabled = false)
    {
        var query = context.Alerts
            .OrderByDescending(a => a.CreatedAt)
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
        var alert = await context.Alerts.FirstOrDefaultAsync(alert => alert.Id == alertId);
        if (alert == null) return Result.Success();
        if(!alert.IsConfigurable) return Error.Validation("Alert.IsConfigurable", "This alert is not configurable");
        
        alert.Title = request.Title;
        alert.NotificationType = request.NotificationType;
        alert.TimeFrame = request.TimeFrame;
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

    public async Task ProcessAlert(string message, NotificationType notificationType, Guid? departmentId)
    {
        var alert = await context.Alerts
            .AsSplitQuery()
            .Include(alert => alert.Roles).ThenInclude(alertRole => alertRole.Role)
            .Include(alert => alert.Users).ThenInclude(alertUser => alertUser.User)
            .FirstOrDefaultAsync(item => item.NotificationType == notificationType);

        List<User> users = [];
        var roles = alert.Roles;
        foreach (var role in roles)
        {
            if (string.IsNullOrEmpty(role.Role?.Name)) continue;

            if (departmentId != null)
            {
                var roleDepartment = await context.RoleDepartments
                    .FirstOrDefaultAsync(r => r.DepartmentId == departmentId && r.RoleId == role.RoleId);
                if(roleDepartment is null) continue;
            }
            var user = await userManager.GetUsersInRoleAsync(role.Role.Name);
            users.AddRange(user);
        }
        users.AddRange(alert.Users.Select(u => u.User));

        if (departmentId != null)
        {
            users = users.Where(u => u.DepartmentId == departmentId).ToList();
        }
        
        if(users.Count == 0) return;
        
        var notification = new NotificationDto
        {
            Message = message,
            Recipients = mapper.Map<List<UserDto>>(users)
        };
        await notificationService.SendNotification(notification, alert.AlertTypes);
    }
}