using DOMAIN.Entities.Base;
using SHARED;

namespace DOMAIN.Entities.PayrollLoanLedgers;


public class PayrollLoanLedgerDto : BaseDto
{
    public CollectionItemDto PayrollElementAssignment { get; set; }
    public CollectionItemDto PayrollRun { get; set; }
    public CollectionItemDto Employee { get; set; }

    public decimal OpeningBalance { get; set; }
    public decimal Repayment { get; set; }
    public decimal ClosingBalance { get; set; }

    public DateTime EffectiveDate { get; set; }
}
