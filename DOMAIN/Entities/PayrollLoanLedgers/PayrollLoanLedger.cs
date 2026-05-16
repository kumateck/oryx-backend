using DOMAIN.Entities.Base;
using DOMAIN.Entities.Employees;
using DOMAIN.Entities.PayrollElementAssignments;
using DOMAIN.Entities.PayrollRuns;

namespace DOMAIN.Entities.PayrollLoanLedgers;


public class PayrollLoanLedger : BaseEntity
{
    public Guid PayrollElementAssignmentId { get; set; }
    public PayrollElementAssignment PayrollElementAssignment { get; set; }

    public Guid PayrollRunId { get; set; }
    public PayrollRun PayrollRun { get; set; }

    public Guid EmployeeId { get; set; }
    public Employee Employee { get; set; }

    public decimal OpeningBalance { get; set; }
    public decimal Repayment { get; set; }
    public decimal ClosingBalance { get; set; }

    public DateTime EffectiveDate { get; set; }
}
