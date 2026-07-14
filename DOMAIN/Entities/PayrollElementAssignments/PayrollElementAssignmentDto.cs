using DOMAIN.Entities.Base;
using DOMAIN.Entities.PayrollElements;
using SHARED;

namespace DOMAIN.Entities.PayrollElementAssignments;

public class PayrollElementAssignmentDto : BaseDto
{
    public CollectionItemDto Employee { get; set; }
    public PayrollElementDto PayrollElement { get; set; }

    public decimal? OverrideAmount { get; set; }
    public decimal? OverrideRate { get; set; }
    public string OverrideFormula { get; set; }

    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }

    public decimal? LoanBalance { get; set; }
    public decimal? LoanInstallment { get; set; }
    public int? LoanTermMonths { get; set; }

    public PayrollElementAssignmentStatus Status { get; set; }
}