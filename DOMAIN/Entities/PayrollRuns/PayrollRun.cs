using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Approvals;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.PayGroups;
using DOMAIN.Entities.PayrollCompanies;
using DOMAIN.Entities.PayRollPeriods;
using DOMAIN.Entities.PayrollRunEmployees;
using DOMAIN.Entities.Users;

namespace DOMAIN.Entities.PayrollRuns;



public class PayrollRun : BaseEntity, IRequireApproval
{
    [StringLength(50)] public string Code { get; set; }
    public PayrollRunType RunType { get; set; } = PayrollRunType.Regular;
    public PayrollRunStatus RunStatus { get; set; } = PayrollRunStatus.Draft;

    public int EmployeeCount { get; set; }
    public decimal GrossTotal { get; set; }
    public decimal NetTotal { get; set; }
    public decimal EmployerCostTotal { get; set; }

    [StringLength(2000)] public string Notes { get; set; }
    [StringLength(128)] public string Checksum { get; set; }

    public DateTime? LockedAt { get; set; }
    public Guid? LockedById { get; set; }
    public User LockedBy { get; set; }

    public DateTime? PrecheckCompletedAt { get; set; }
    public DateTime? CalculatedAt { get; set; }
    public DateTime? ValidatedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? PaymentReleasedAt { get; set; }
    public DateTime? PostedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public DateTime? CancelledAt { get; set; }

    public Guid PayrollCompanyId { get; set; }
    public PayrollCompany PayrollCompany { get; set; }

    public Guid PayrollPeriodId { get; set; }
    public PayrollPeriod PayrollPeriod { get; set; }

    public Guid PayGroupId { get; set; }
    public PayGroup PayGroup { get; set; }

    public Guid? ParentRunId { get; set; }
    public PayrollRun ParentRun { get; set; }

    public List<PayrollRunEmployee> RunEmployees { get; set; } = [];
    public List<PayrollRunApproval> Approvals { get; set; } = [];

    public bool Approved { get; set; }
}

public class PayrollRunApproval : ResponsibleApprovalStage
{
    public Guid Id { get; set; }

    public Guid PayrollRunId { get; set; }
    public PayrollRun PayrollRun { get; set; }

    public Guid ApprovalId { get; set; }
    public Approval Approval { get; set; }
}

public enum PayrollRunType
{
    Regular = 0,
    OffCycle = 1,
    Retro = 2,
    Termination = 3,
    Bonus = 4,
    Correction = 5
}

public enum PayrollRunStatus
{
    Draft = 0,
    PrecheckFailed = 1,
    PrecheckPassed = 2,
    Calculated = 3,
    ValidationFailed = 4,
    ReadyForApproval = 5,
    Approved = 6,
    PaymentReleased = 7,
    Posted = 8,
    Closed = 9,
    Cancelled = 10
}