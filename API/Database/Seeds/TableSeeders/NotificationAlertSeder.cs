using APP.Utils;
using DOMAIN.Entities.Alerts;
using DOMAIN.Entities.Notifications;
using INFRASTRUCTURE.Context;

namespace API.Database.Seeds.TableSeeders;

public class NotificationAlertSeder : ISeeder
{
    public void Handle(IServiceScope scope)
    {
        var dbContext = scope.ServiceProvider.GetService<ApplicationDbContext>();

        if (dbContext is null) return;

        if (dbContext.Alerts.Any()) return; // avoid duplication

        var roles = dbContext.Roles.ToList();

        var prodManager = roles.FirstOrDefault(r => r.Name == RoleUtils.ProductionManger);
        var warehouseManager = roles.FirstOrDefault(r => r.Name == RoleUtils.WarehouseManger);
        
        if (prodManager is null || warehouseManager is null) return;
        
        var allAlertTypes = Enum.GetValues<AlertType>().ToList();
        
       var alerts = new List<Alert>
        {
            new()
            {
                Title = "Production stage changed",
                NotificationType = NotificationType.ProductionStageChanged,
                Roles = [new AlertRole { RoleId = prodManager.Id }],
                AlertTypes = allAlertTypes,
                IsConfigurable = false,
                TimeFrame = TimeSpan.Zero
            },
            new()
            {
                Title = "You have been assigned a shift",
                NotificationType = NotificationType.ShiftAssigned,
                AlertTypes = allAlertTypes,
                IsConfigurable = false,
                TimeFrame = TimeSpan.Zero
            },
            new()
            {
                Title = "Shipment has arrived",
                NotificationType = NotificationType.ShipmentArrived,
                AlertTypes = allAlertTypes,
                Roles =
                [
                    new AlertRole { RoleId = prodManager.Id },
                    new AlertRole { RoleId = warehouseManager.Id }
                ],
                IsConfigurable = false,
                TimeFrame = TimeSpan.Zero
            },
            new()
            {
                Title = "Material exceeds maximum stock",
                NotificationType = NotificationType.MaterialAboveMaxStock,
                AlertTypes = allAlertTypes,
                Roles =
                [
                    new AlertRole { RoleId = prodManager.Id },
                    new AlertRole { RoleId = warehouseManager.Id }
                ],
                IsConfigurable = false,
                TimeFrame = TimeSpan.Zero
            },
            new()
            {
                Title = "Material below minimum stock",
                NotificationType = NotificationType.MaterialBelowMinStock,
                AlertTypes = allAlertTypes,
                Roles =
                [
                    new AlertRole { RoleId = prodManager.Id },
                    new AlertRole { RoleId = warehouseManager.Id }
                ],
                IsConfigurable = false,
                TimeFrame = TimeSpan.Zero
            },
            new()
            {
                Title = "Material has reached reorder level",
                NotificationType = NotificationType.MaterialReachedReorderLevel,
                AlertTypes = allAlertTypes,
                Roles =
                [
                    new AlertRole { RoleId = prodManager.Id },
                    new AlertRole{ RoleId = warehouseManager.Id }
                ],
                IsConfigurable = false,
                TimeFrame = TimeSpan.Zero
            },
            new()
            {
                Title = "New stock requisition created",
                NotificationType = NotificationType.StockRequisitionCreated,
                AlertTypes = allAlertTypes,
                Roles = [new AlertRole { RoleId = warehouseManager.Id }],
                IsConfigurable = false,
                TimeFrame = TimeSpan.Zero
            },
            new()
            {
                Title = "Partial stock requisition created",
                NotificationType = NotificationType.PartialRequisitionCreated,
                AlertTypes = allAlertTypes,
                Roles = [new AlertRole { RoleId = warehouseManager.Id }],
                IsConfigurable = false,
                TimeFrame = TimeSpan.Zero
            },
            new()
            {
                Title = "Partial requisition from production created",
                NotificationType = NotificationType.PartialRequestProduction,
                AlertTypes = allAlertTypes,
                Roles = [new AlertRole { RoleId = prodManager.Id }],
                IsConfigurable = false,
                TimeFrame = TimeSpan.Zero
            }
        };
       
        dbContext.Alerts.AddRange(alerts);
        
        var configurableAlerts = new List<Alert>
        {
            new()
            {
                Title = "New Overtime Request Submitted",
                NotificationType = NotificationType.OvertimeRequest,
                AlertTypes = allAlertTypes,
                IsConfigurable = true,
                TimeFrame = TimeSpan.Zero
            },
            new()
            {
                Title = "New Leave Request Submitted",
                NotificationType = NotificationType.LeaveRequest,
                AlertTypes = allAlertTypes,
                IsConfigurable = true,
                TimeFrame = TimeSpan.Zero
            },
            new()
            {
                Title = "New Staff Request Submitted",
                NotificationType = NotificationType.StaffRequest,
                AlertTypes = allAlertTypes,
                IsConfigurable = true,
                TimeFrame = TimeSpan.Zero
            },
            new()
            {
                Title = "New Audit Log Entry",
                NotificationType = NotificationType.AuditLogEvent,
                AlertTypes = allAlertTypes,
                IsConfigurable = true,
                TimeFrame = TimeSpan.Zero
            },
            new()
            {
                Title = "New BMR or BPR Request Submitted",
                NotificationType = NotificationType.BmrBprRequested,
                AlertTypes = allAlertTypes,
                IsConfigurable = true,
                TimeFrame = TimeSpan.Zero
            },
            new()
            {
                Title = "BMR or BPR Request Approved",
                NotificationType = NotificationType.BmrBprApproved,
                AlertTypes = allAlertTypes,
                IsConfigurable = true,
                TimeFrame = TimeSpan.Zero
            }
        };

        dbContext.Alerts.AddRange(configurableAlerts);
        dbContext.SaveChanges();
    }
}
