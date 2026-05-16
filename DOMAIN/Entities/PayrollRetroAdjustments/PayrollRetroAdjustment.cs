using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Approvals;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Employees;
using DOMAIN.Entities.PayrollCompanies;
using DOMAIN.Entities.PayRollPeriods;
using DOMAIN.Entities.PayrollRuns;

namespace DOMAIN.Entities.PayrollRetroAdjustments;

public class PayrollRetroAdjustment : BaseEntity, IRequireApproval
{
    public Guid PayrollCompanyId { get; set; }
    public PayrollCompany PayrollCompany { get; set; }

    public Guid EmployeeId { get; set; }
    public Employee Employee { get; set; }

    public Guid SourcePeriodId { get; set; }
    public PayrollPeriod SourcePeriod { get; set; }

    public Guid? TargetRunId { get; set; }
    public PayrollRun TargetRun { get; set; }

    public PayrollRetroChangeType ChangeType { get; set; }
    [StringLength(2000)] public string Reason { get; set; }
    [StringLength(int.MaxValue)] public string OldValueJson { get; set; }
    [StringLength(int.MaxValue)] public string NewValueJson { get; set; }

    public PayrollRetroAdjustmentStatus Status { get; set; } = PayrollRetroAdjustmentStatus.Draft;

    public List<PayrollRetroAdjustmentApproval> Approvals { get; set; } = [];
    public bool Approved { get; set; }
}

public class PayrollRetroAdjustmentApproval : ResponsibleApprovalStage
{
    public Guid Id { get; set; }

    public Guid PayrollRetroAdjustmentId { get; set; }
    public PayrollRetroAdjustment PayrollRetroAdjustment { get; set; }

    public Guid ApprovalId { get; set; }
    public Approval Approval { get; set; }
}

public enum PayrollRetroChangeType
{
    BackdatedHire = 0,
    BaseSalaryChange = 1,
    ElementChange = 2,
    AssignmentChange = 3,
    AttendanceCorrection = 4,
    LeaveCorrection = 5,
    ConfigurationChange = 6
}

public enum PayrollRetroAdjustmentStatus
{
    Draft = 0,
    PendingApproval = 1,
    Approved = 2,
    Materialized = 3,
    Rejected = 4,
    Cancelled = 5
}