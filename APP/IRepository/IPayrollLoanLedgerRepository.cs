using APP.Utils;
using DOMAIN.Entities.PayrollLoanLedgers;
using SHARED;

namespace APP.IRepository;

public interface IPayrollLoanLedgerRepository
{
    
    Task<Result<Paginateable<IEnumerable<PayrollLoanLedgerDto>>>> GetLoanLedgerEntries(int page, int pageSize,
        string searchQuery, Guid? employeeId = null, Guid? payrollElementAssignmentId = null);
    Task<Result<PayrollLoanLedgerDto>> GetLoanLedgerEntry(Guid id);
}