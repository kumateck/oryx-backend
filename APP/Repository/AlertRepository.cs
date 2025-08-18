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

public class AlertRepository(ApplicationDbContext context, IMapper mapper, UserManager<User> userManager, INotificationService notificationService) : IAlertRepository
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
        
        if(!alert.IsConfigurable) return Error.Validation("Alert.IsConfigurable", "This alert is not configurable");

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
            if(!alert.IsConfigurable) return Error.Validation("Alert.IsConfigurable", "You cannot delete a non configurable alert");
            alert.DeletedAt = DateTime.UtcNow;
            alert.LastDeletedById = userId;
            context.Alerts.Update(alert);
            await context.SaveChangesAsync();
        }
        return Result.Success();
    }

    public async Task ProcessAlert(string message, NotificationType notificationType, Guid? departmentId, List<User> assignedUsers = null)
    {
        var alert = await context.Alerts
            .AsSplitQuery()
            .Include(alert => alert.Roles).ThenInclude(alertRole => alertRole.Role)
            .Include(alert => alert.Users).ThenInclude(alertUser => alertUser.User)
            .FirstOrDefaultAsync(item => item.NotificationType == notificationType && !item.IsDisabled);
        
        if (alert is null) return;

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

        if (assignedUsers != null && assignedUsers.Count != 0)
        {
            users.AddRange(assignedUsers);
        }
        
        users = users.Distinct().ToList();
        
        if(users.Count == 0) return;
        
        var notification = new NotificationDto
        {
            Id = Guid.NewGuid(),
            Message = message,
            Recipients = mapper.Map<List<UserDto>>(users),
            Type = notificationType,
            SentAt = DateTime.UtcNow,
        };
        await notificationService.SendNotification(notification, alert.AlertTypes);
    }

    public async Task<Result<List<NotificationDto>>> GetNotificationsForUser(Guid userId, bool unreadOnly)
    {
        var query = context.Notifications
            .Where(n => n.Recipients.Contains(userId))
            .AsQueryable();

        if (unreadOnly)
        {
            query = query.Where(n =>
                !context.NotificationReads.Any(r => r.UserId == userId && r.NotificationId == n.Id));
        }

        var notifications = await query
            .OrderByDescending(n => n.SentAt)
            .Select(n => new NotificationDto
            {
                Id = n.Id,
                Message = n.Message,
                Type = n.Type,
            })
            .ToListAsync();

        return Result.Success(notifications);
    }


    public async Task<Result> MarkNotificationAsRead(Guid id, Guid userId)
    {
        var notification = await context.Notifications.FirstOrDefaultAsync(item => item.Id == id);
        if(notification is null) return Error.NotFound("Notification", "Notification not found");

        if (!notification.Recipients.Contains(userId))
        {
            return Error.Validation("Notification.User", "You do not have permission to read this notification");
        }

        if (context.NotificationReads.Any(r => r.UserId == userId && r.NotificationId == notification.Id))
        {
            return Error.Validation("Notification.User", "You are already read this notification");
        }

        await context.NotificationReads.AddAsync(new NotificationRead
        {
            NotificationId = notification.Id,
            UserId = userId,
            ReadAt = DateTime.UtcNow
        });
        
        await context.SaveChangesAsync();
        return Result.Success();
    }
}