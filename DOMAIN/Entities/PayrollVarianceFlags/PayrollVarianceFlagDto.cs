using DOMAIN.Entities.Base;
using DOMAIN.Entities.Users;
using SHARED;

namespace DOMAIN.Entities.PayrollVarianceFlags;


public class PayrollVarianceFlagDto : BaseDto
{
    public CollectionItemDto PayrollRun { get; set; }
    public CollectionItemDto Employee { get; set; }

    public PayrollVarianceMetric Metric { get; set; }
    public decimal PreviousValue { get; set; }
    public decimal CurrentValue { get; set; }
    public decimal DeltaPercent { get; set; }
    public decimal ThresholdPercent { get; set; }

    public PayrollVarianceSeverity Severity { get; set; }
    public PayrollVarianceDisposition Disposition { get; set; }

    public UserDto Reviewer { get; set; }
    public string ReviewerComment { get; set; }
    public DateTime? ReviewedAt { get; set; }
}

public class ReviewVarianceFlagRequest
{
    public PayrollVarianceDisposition Disposition { get; set; }
    public string ReviewerComment { get; set; }
}