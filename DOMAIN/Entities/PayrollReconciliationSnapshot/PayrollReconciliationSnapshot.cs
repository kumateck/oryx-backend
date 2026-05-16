using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.PayrollCompanies;
using DOMAIN.Entities.PayRollPeriods;
using DOMAIN.Entities.Users;

namespace DOMAIN.Entities.PayrollReconciliationSnapshot;

public class PayrollReconciliationSnapshot : BaseEntity
{
    public Guid PayrollCompanyId { get; set; }
    public PayrollCompany PayrollCompany { get; set; }

    public Guid PayrollPeriodId { get; set; }
    public PayrollPeriod PayrollPeriod { get; set; }

    public decimal NetPayTotal { get; set; }
    public decimal PaymentBatchTotal { get; set; }
    public decimal Variance { get; set; }

    public decimal PayablesGlBalance { get; set; }
    public decimal PayablesPayrollBalance { get; set; }
    public decimal PayablesVariance { get; set; }

    public decimal StatutoryComputed { get; set; }
    public decimal StatutoryRemitted { get; set; }
    public decimal StatutoryVariance { get; set; }

    public PayrollReconciliationStatus Status { get; set; } = PayrollReconciliationStatus.Clean;
    [StringLength(int.MaxValue)] public string BreakdownJson { get; set; }

    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public Guid? GeneratedById { get; set; }
    public User GeneratedBy { get; set; }
}

public enum PayrollReconciliationStatus
{
    Clean = 0,
    Variance = 1,
    Failed = 2
}