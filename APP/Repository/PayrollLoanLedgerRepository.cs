using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.PayrollLoanLedgers;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public class PayrollLoanLedgerRepository(ApplicationDbContext context, IMapper mapper) : IPayrollLoanLedgerRepository
{
    public async Task<Result<Paginateable<IEnumerable<PayrollLoanLedgerDto>>>> GetLoanLedgerEntries(int page, int pageSize, string searchQuery, Guid? employeeId = null,
        Guid? payrollElementAssignmentId = null)
    {
        var query = context.PayrollLoanLedgers.AsQueryable();

        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.WhereSearch(searchQuery, q=> q.PayrollRun.Code);
        }

        if (payrollElementAssignmentId.HasValue)
        {
            query = query.Where(p => p.PayrollElementAssignmentId == payrollElementAssignmentId.Value);
        }
        
        if (employeeId.HasValue)
        {
            query = query.Where(p => p.EmployeeId == employeeId.Value);
        }
        
        return await PaginationHelper.GetPaginatedResultAsync(query,page, pageSize,
            mapper.Map<PayrollLoanLedgerDto>);
        
    }

    public async Task<Result<PayrollLoanLedgerDto>> GetLoanLedgerEntry(Guid id)
    {
        var entry = await context.PayrollLoanLedgers.FirstOrDefaultAsync(p=> p.Id == id);
        return entry is null ? Error.NotFound("PayrollLoanLedger.NotFound", "Payroll loan ledger not found") 
            : mapper.Map<PayrollLoanLedgerDto>(entry);
    }
}