using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.PayrollRunEmployees;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public class PayrollRunEmployeeRepository(ApplicationDbContext context, IMapper mapper) : IPayrollRunEmployeeRepository
{
    public async Task<Result<Paginateable<IEnumerable<PayrollRunEmployeeDto>>>> GetRunEmployees(Guid payrollRunId, int page, int pageSize, string searchQuery, bool? isExcluded = null)
    {
        var query = context.PayrollRunEmployees
            .Include(r => r.ResultLines)
            .Where(r => r.PayrollRunId == payrollRunId)
            .AsQueryable();

        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.WhereSearch(searchQuery,r => r.PayrollRun.Code);
        }
        
        if (isExcluded.HasValue) query = query.Where(r => r.IsExcluded == isExcluded.Value);

        return await PaginationHelper.GetPaginatedResultAsync(query, page, pageSize,
            mapper.Map<PayrollRunEmployeeDto>);

    }

    public async Task<Result<PayrollRunEmployeeDto>> GetRunEmployee(Guid id)
    {
        var runEmployee = await context.PayrollRunEmployees
            .Include(re => re.ResultLines)
            .FirstOrDefaultAsync(p => p.Id == id);
        return runEmployee is null ? 
            Error.NotFound("PayrollRunEmployee.NotFound","Payroll run employee not found")
            : mapper.Map<PayrollRunEmployeeDto>(runEmployee);
    }
}