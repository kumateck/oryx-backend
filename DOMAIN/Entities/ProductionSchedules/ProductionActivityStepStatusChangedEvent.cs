using DOMAIN.Entities.Base;

namespace DOMAIN.Entities.ProductionSchedules;

public class ProductionActivityStepStatusChangedEvent
{
    public Guid ProductionActivityStepId { get; set; }
    public Guid ProductionActivityId { get; set; }
    public Guid ProductionScheduleProductId { get; set; }
    public Guid ProductionScheduleId { get; set; }
    public ProductionStatus Status { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
}
