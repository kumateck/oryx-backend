using DOMAIN.Entities.Approvals;
using DOMAIN.Entities.Base;

namespace DOMAIN.Entities.Payroll;

public class PayrollRun : BaseEntity, IRequireApproval
{
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public PayrollRunStatus Status { get; set; } = PayrollRunStatus.Draft;
    public bool Approved { get; set; }

    public List<Payslip> Payslips { get; set; } = [];
    public List<PayrollRunApproval> Approvals { get; set; } = [];
}

public class PayrollRunApproval : ResponsibleApprovalStage
{
    public Guid Id { get; set; }
    public Guid PayrollRunId { get; set; }
    public PayrollRun PayrollRun { get; set; }
    public Guid ApprovalId { get; set; }
    public Approval Approval { get; set; }
}

public enum PayrollRunStatus
{
    Draft,
    PendingApproval,
    Approved,
    Processed,
    Cancelled
}
