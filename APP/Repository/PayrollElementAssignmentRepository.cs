using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.PayrollElementAssignments;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public class PayrollElementAssignmentRepository(ApplicationDbContext context, IMapper mapper) : IPayrollElementAssignmentRepository
{
    public async Task<Result<Guid>> CreatePayrollElementAssignment(CreatePayrollElementAssignment request)
    {
        var employee = await context.Employees.AnyAsync(x => x.Id == request.EmployeeId);
        if (!employee) return Error.NotFound("Employee.NotFound","Employee not found");
        
        var element = await context.PayrollElements.AnyAsync(x => x.Id == request.PayrollElementId);
        if (!element) return Error.NotFound("PayrollElement.NotFound", "PayrollElement not found");
        
        if (request.EffectiveFrom > request.EffectiveTo) return 
            Error.Validation("EffectiveFrom", "EffectiveFrom cannot be after EffectiveTo");
        
        var assignment = mapper.Map<PayrollElementAssignment>(request);
        await context.PayrollElementAssignments.AddAsync(assignment);
        await context.SaveChangesAsync();
        
        return assignment.Id;
    }

    public async Task<Result<Paginateable<IEnumerable<PayrollElementAssignmentDto>>>> GetPayrollElementAssignments(
        int page, int pageSize, string searchQuery, Guid? employeeId = null, Guid? payrollElementId = null,
        PayrollElementAssignmentStatus? status = null)
    {
        var query = context.PayrollElementAssignments
            .Include(p => p.PayrollElement)
            .Include(p => p.Employee)
            .AsQueryable();
        
        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.WhereSearch(searchQuery);
        }

        if (employeeId.HasValue)
        {
            query = query.Where(x => x.EmployeeId == employeeId.Value);
        }

        if (payrollElementId.HasValue)
        {
            query = query.Where(x => x.PayrollElementId == payrollElementId.Value);
        }

        if (Enum.TryParse<PayrollElementAssignmentStatus>(searchQuery,true, out var statusEnum))
        {
            query = query.Where(x => x.Status == statusEnum);
        }
        
        return await PaginationHelper.GetPaginatedResultAsync(query, page, pageSize,
            mapper.Map<PayrollElementAssignmentDto>);
    }

    public async Task<Result<PayrollElementAssignmentDto>> GetPayrollElementAssignment(Guid id)
    {
        var assignment = await context.PayrollElementAssignments
            .Include(p => p.PayrollElement)
            .Include(p => p.Employee)
            .FirstOrDefaultAsync(x => x.Id == id);
        
        return assignment is null ? 
            Error.NotFound("PayrollElementAssignment.NotFound", "PayrollElement not found")
            : mapper.Map<PayrollElementAssignmentDto>(assignment);
    }

    public async Task<Result> UpdatePayrollElementAssignment(Guid id, CreatePayrollElementAssignment request)
    {
        var assignment = await context.PayrollElementAssignments.FirstOrDefaultAsync(x => x.Id == id);
        if (assignment is null) return Error.NotFound("PayrollElementAssignment.NotFound", "PayrollElement not found");
        
        mapper.Map(request, assignment);
        context.PayrollElementAssignments.Update(assignment);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> DeletePayrollElementAssignment(Guid id, Guid userId)
    {
        var assignment = await context.PayrollElementAssignments.FirstOrDefaultAsync(p => p.Id == id);
        if (assignment is null)
            return Error.NotFound("PayrollElementAssignment.NotFound", "Payroll element assignment not found");
        
        assignment.DeletedAt = DateTime.UtcNow;
        assignment.LastDeletedById = userId;
        
        await context.SaveChangesAsync();
        return Result.Success();
    }
}