using DOMAIN.Entities.Alerts;
using DOMAIN.Entities.Notifications;
using DOMAIN.Entities.Users;
using INFRASTRUCTURE.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace APP.Services.NotificationService;

/// <summary>Every active configuration contributes recipients; overlapping configurations do not duplicate a channel.</summary>
public class AlertProcessor(ApplicationDbContext context, UserManager<User> users, INotificationService notifications)
{
    public async Task Process(string message, NotificationType type, Guid? departmentId, List<User> assignedUsers, Guid? shipmentDocumentId)
    {
        var configurations = await context.Alerts.AsSplitQuery().Include(alert => alert.Roles).ThenInclude(item => item.Role)
            .Include(alert => alert.Users).ThenInclude(item => item.User)
            .Where(alert => alert.NotificationType == type && !alert.IsDisabled).ToListAsync();
        var channels = new Dictionary<AlertType, Dictionary<Guid, User>>();
        foreach (var configuration in configurations)
        {
            var recipients = new List<User>();
            foreach (var role in configuration.Roles)
            {
                if (string.IsNullOrWhiteSpace(role.Role?.Name)) continue;
                if (departmentId.HasValue && !await context.RoleDepartments.AnyAsync(item => item.DepartmentId == departmentId && item.RoleId == role.RoleId)) continue;
                recipients.AddRange(await users.GetUsersInRoleAsync(role.Role.Name));
            }
            recipients.AddRange(configuration.Users.Select(item => item.User).Where(user => user != null));
            if (departmentId.HasValue) recipients = recipients.Where(user => user.DepartmentId == departmentId).ToList();
            // Explicit assignees are a separate, trusted business audience, preserved from the producer.
            recipients.AddRange(assignedUsers ?? []);
            foreach (var channel in configuration.AlertTypes.Distinct())
            {
                if (!channels.TryGetValue(channel, out var audience)) channels[channel] = audience = [];
                foreach (var recipient in recipients.Where(user => !user.IsDisabled && user.DeletedAt == null)) audience[recipient.Id] = recipient;
            }
        }
        foreach (var (channel, audience) in channels)
        {
            if (audience.Count == 0) continue;
            await notifications.SendNotification(new NotificationDto {
                Message = message, Type = type, SentAt = DateTime.UtcNow, ShipmentDocumentId = shipmentDocumentId,
                Recipients = audience.Keys.Select(id => new UserDto { Id = id }).ToList(),
            }, [channel]);
        }
    }
}
