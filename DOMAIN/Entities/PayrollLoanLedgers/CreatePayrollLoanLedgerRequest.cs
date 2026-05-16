namespace DOMAIN.Entities.PayrollLoanLedgers;

public class CreatePayrollLoanLedgerRequest
{
    public Guid PayrollElementAssignmentId { get; set; }
    public Guid PayrollRunId { get; set; }
    public Guid EmployeeId { get; set; }
    public decimal OpeningBalance { get; set; }
    public decimal Repayment { get; set; }
    public decimal ClosingBalance { get; set; }
    public DateTime EffectiveDate { get; set; }
}

