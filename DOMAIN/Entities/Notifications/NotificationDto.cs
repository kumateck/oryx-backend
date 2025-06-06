using DOMAIN.Entities.Users;

namespace DOMAIN.Entities.Notifications;

public class NotificationDto
{
    public string Message { get; set; }
    public List<UserDto> Recipients { get; set; } = [];
}

public enum NotificationType
{
    ProductionStageChanged,
    ShiftAssigned,
    ShipmentArrived,
    MaterialAboveMaxStock,
    MaterialBelowMinStock,
    MaterialReachedReorderLevel,
    StockRequisitionCreated,
    PartialRequisitionCreated,
    PartialRequestProduction,
    OvertimeRequest,
    LeaveRequest,
    StaffRequest,
    AuditLogEvent,
    BmrBprRequested,
    BmrBprApproved
}
