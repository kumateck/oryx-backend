using DOMAIN.Entities.Base;

namespace DOMAIN.Entities.ProductionSchedules.Packing;

public class FinalPacking : BaseEntity
{
    public Guid ProductionScheduleId { get; set; }
    public Guid ProductId { get; set; }
    public Guid ProductionActivityStepId { get; set; }
    
}