using DOMAIN.Entities.Base;
using SHARED;

namespace DOMAIN.Entities.PayrollRetroAdjustments;

public class PayrollRetroAdjustmentDto : BaseDto
{
    public CollectionItemDto PayrollCompany { get; set; }
    public CollectionItemDto Employee { get; set; }
    public CollectionItemDto SourcePeriod { get; set; }
    public CollectionItemDto TargetRun { get; set; }
    
    public PayrollRetroChangeType ChangeType { get; set; }
    public string Reason { get; set; }
    public string OldValueJson { get; set; }
    public string NewValueJson { get; set; }
    
    public PayrollRetroAdjustmentStatus Status { get; set; }
    public bool Approved { get; set; }
}