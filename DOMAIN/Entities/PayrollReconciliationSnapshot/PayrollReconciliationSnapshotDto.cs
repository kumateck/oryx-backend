using DOMAIN.Entities.Base;
using DOMAIN.Entities.Users;
using SHARED;

namespace DOMAIN.Entities.PayrollReconciliationSnapshot;

public class PayrollReconciliationSnapshotDto : BaseDto
{
    public CollectionItemDto PayrollCompany { get; set; }
    public CollectionItemDto PayrollPeriod { get; set; }

    public decimal NetPayTotal { get; set; }
    public decimal PaymentBatchTotal { get; set; }
    public decimal Variance { get; set; }

    public decimal PayablesGlBalance { get; set; }
    public decimal PayablesPayrollBalance { get; set; }
    public decimal PayablesVariance { get; set; }

    public decimal StatutoryComputed { get; set; }
    public decimal StatutoryRemitted { get; set; }
    public decimal StatutoryVariance { get; set; }

    public PayrollReconciliationStatus Status { get; set; } 
    public string BreakdownJson { get; set; }

    public DateTime GeneratedAt { get; set; }
    public UserDto GeneratedBy { get; set; }
}
