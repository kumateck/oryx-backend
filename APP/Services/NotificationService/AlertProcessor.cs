using DOMAIN.Entities.Alerts;
using DOMAIN.Entities.Notifications;
using DOMAIN.Entities.Products.Production;
using DOMAIN.Entities.Users;
using INFRASTRUCTURE.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Services.NotificationService;

/// <summary>Every active configuration contributes recipients; overlapping configurations do not duplicate a channel.</summary>
public class AlertProcessor(ApplicationDbContext context, UserManager<User> users, INotificationService notifications)
{
    public async Task Process(string message, NotificationType type, Guid? departmentId, List<User> assignedUsers, Guid? shipmentDocumentId, string relatedEntityType, Guid? relatedEntityId, string relatedEntityCode, string relatedEntityName, string relatedEntityStatus, string relatedEntityPriority)
    {
        var configurations = await context.Alerts.AsSplitQuery().Include(alert => alert.Roles).ThenInclude(item => item.Role)
            .Include(alert => alert.Users).ThenInclude(item => item.User)
            .Where(alert => alert.NotificationType == type && !alert.IsDisabled).ToListAsync();
        var channels = new Dictionary<AlertType, Dictionary<Guid, User>>();
        // Production departments work apart: people who sit in a different production department never
        // receive another department's production alert, while QA, stores and other departments still do.
        var home = departmentId ?? await ProductionDepartmentOf(relatedEntityType, relatedEntityId);
        var productionDepartments = home.HasValue
            ? (await context.Departments.Where(item => item.Type == DepartmentType.Production).Select(item => item.Id).ToListAsync()).ToHashSet()
            : [];
        if (home.HasValue && !productionDepartments.Contains(home.Value)) productionDepartments.Clear();
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
            recipients = recipients.Where(user => user.DepartmentId is not { } own || own == home || !productionDepartments.Contains(own)).ToList();
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
                Message = message, Type = type, SentAt = DateTime.UtcNow, ShipmentDocumentId = shipmentDocumentId, RelatedEntityType = relatedEntityType, RelatedEntityId = relatedEntityId, RelatedEntityCode = relatedEntityCode, RelatedEntityName = relatedEntityName, RelatedEntityStatus = relatedEntityStatus, RelatedEntityPriority = relatedEntityPriority,
                Recipients = audience.Keys.Select(id => new UserDto { Id = id }).ToList(),
            }, [channel]);
        }
    }

    /// <summary>
    /// The production department a production record belongs to, through its product. Products are
    /// otherwise filtered to the signed-in user's department, and alerts are sent in the background.
    /// </summary>
    private async Task<Guid?> ProductionDepartmentOf(string type, Guid? id)
    {
        if (id is not { } key) return null;
        return type switch
        {
            nameof(BatchManufacturingRecord) => await context.BatchManufacturingRecords.IgnoreQueryFilters().Where(item => item.Id == key)
                .Select(item => item.ProductionScheduleProduct.Product.DepartmentId).FirstOrDefaultAsync(),
            nameof(BatchPackagingRecord) => await context.BatchPackagingRecords.IgnoreQueryFilters().Where(item => item.Id == key)
                .Select(item => item.ProductionScheduleProduct.Product.DepartmentId).FirstOrDefaultAsync(),
            nameof(ProductionActivityStep) => await context.ProductionActivitySteps.IgnoreQueryFilters().Where(item => item.Id == key)
                .Select(item => item.ProductionActivity.ProductionScheduleProduct.Product.DepartmentId).FirstOrDefaultAsync(),
            _ => null,
        };
    }
}
