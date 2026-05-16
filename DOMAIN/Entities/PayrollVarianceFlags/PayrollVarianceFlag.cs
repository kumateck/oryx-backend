using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Employees;
using DOMAIN.Entities.PayrollRuns;
using DOMAIN.Entities.Users;

namespace DOMAIN.Entities.PayrollVarianceFlags;


public class PayrollVarianceFlag : BaseEntity
{
    public Guid PayrollRunId { get; set; }
    public PayrollRun PayrollRun { get; set; }

    public Guid EmployeeId { get; set; }
    public Employee Employee { get; set; }

    public PayrollVarianceMetric Metric { get; set; }
    public decimal PreviousValue { get; set; }
    public decimal CurrentValue { get; set; }
    public decimal DeltaPercent { get; set; }
    public decimal ThresholdPercent { get; set; }

    public PayrollVarianceSeverity Severity { get; set; }
    public PayrollVarianceDisposition Disposition { get; set; } = PayrollVarianceDisposition.PendingReview;

    public Guid? ReviewerId { get; set; }
    public User Reviewer { get; set; }
    [StringLength(2000)] public string ReviewerComment { get; set; }
    public DateTime? ReviewedAt { get; set; }
}

public enum PayrollVarianceMetric
{
    NetPay = 0,
    Gross = 1,
    Tax = 2,
    EmployerCost = 3,
    ElementAmount = 4
}

public enum PayrollVarianceSeverity
{
    Critical = 0,
    High = 1,
    Medium = 2,
    Low = 3
}

public enum PayrollVarianceDisposition
{
    PendingReview = 0,
    AcceptedExpected = 1,
    AcceptedException = 2,
    Rejected = 3
}