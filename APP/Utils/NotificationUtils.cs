using DOMAIN.Entities.Notifications;

namespace APP.Utils;

public class NotificationUtils
{
    public static IEnumerable<NotificationType> CongigurableNotifications()
    {
        return
        [
            NotificationType.OvertimeRequest,
            NotificationType.LeaveRequest,
            NotificationType.StaffRequest,
            NotificationType.AuditLogEvent,
            NotificationType.BmrBprRequested,
            NotificationType.BmrBprApproved
        ];
    }

    public static IEnumerable<NotificationType> NonCongigurableNotifications()
    {
        return
        [

            NotificationType.ProductionStageChanged,
            NotificationType.ShiftAssigned,
            NotificationType.ShipmentArrived,
            NotificationType.MaterialAboveMaxStock,
            NotificationType.MaterialBelowMinStock,
            NotificationType.MaterialReachedReorderLevel,
            NotificationType.StockRequisitionCreated,
            NotificationType.PartialRequisitionCreated,
            NotificationType.PartialRequestProduction
        ];
    }
}

