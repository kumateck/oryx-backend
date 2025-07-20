using DOMAIN.Entities.Users;

namespace DOMAIN.Entities.Notifications;

public class NotificationDto
{
    public Guid Id { get; set; }
    public string Message { get; set; }
    public NotificationType Type { get; set; }
    public List<UserDto> Recipients { get; set; } = [];
    public DateTime SentAt { get; set; }
}

public enum NotificationType
{
    ProductionStageChanged = 0,
    ShiftAssigned = 1,
    ShipmentArrived = 2,
    MaterialAboveMaxStock = 3,
    MaterialBelowMinStock = 4,
    MaterialReachedReorderLevel = 5,
    StockRequisitionCreated = 6,
    PartialRequisitionCreated = 7,
    PartialRequestProduction = 8,
    OvertimeRequest = 9,
    LeaveRequest = 10,
    StaffRequest = 11,
    AuditLogEvent = 12,
    BmrBprRequested = 13,
    BmrBprApproved = 14
}
