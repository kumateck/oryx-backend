using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.PayrollRetroAdjustments;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public class PayrollRetroAdjustmentRepository(ApplicationDbContext context, IMapper mapper) : IPayrollRetroAdjustmentRepository
{
    public async Task<Result<Guid>> CreatePayrollRetroAdjustment(CreatePayrollRetroAdjustmentRequest request)
    {
        var employee = await context.Employees.AnyAsync(q => q.Id == request.EmployeeId);
        if (!employee) return Error.NotFound("Employee.NotFound", "Employee not found");
        
        var sourcePeriod = await context.PayrollPeriods.AnyAsync(q => q.Id == request.SourcePeriodId);
        if (!sourcePeriod) return Error.NotFound("SourcePeriod.NotFound", "SourcePeriod not found");
        
        var adjustment = mapper.Map<PayrollRetroAdjustment>(request);
        await context.PayrollRetroAdjustments.AddAsync(adjustment);
        await context.SaveChangesAsync();
        return adjustment.Id;
    }

    public async Task<Result<Paginateable<IEnumerable<PayrollRetroAdjustmentDto>>>> GetRetroAdjustments(int page,
        int pageSize, string searchQuery, Guid? payrollCompanyId = null, Guid? employeeId = null,
        PayrollRetroAdjustmentStatus? status = null)
    {
        var query = context.PayrollRetroAdjustments.AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            query = query.WhereSearch(searchQuery, q => q.Employee.FirstName);
        }

        if (Enum.TryParse<PayrollRetroAdjustmentStatus>(searchQuery, true, out var statusEnum))
        {
            query = query.Where(q => q.Status == statusEnum);
        }
        
        if  (payrollCompanyId.HasValue) query = query.Where(q => q.PayrollCompanyId == payrollCompanyId);
        if (employeeId.HasValue) query = query.Where(q => q.EmployeeId == employeeId);
        if (status.HasValue) query = query.Where(q => q.Status == status);
        
        return await PaginationHelper.GetPaginatedResultAsync(query, page, pageSize, mapper.Map<PayrollRetroAdjustmentDto>);
    }

    public async Task<Result<PayrollRetroAdjustmentDto>> GetRetroAdjustment(Guid id)
    {
        var adjustment = await context.PayrollRetroAdjustments.FirstOrDefaultAsync(q => q.Id == id);
        return adjustment is null ? 
            Error.NotFound("PayrollRetroAdjustment.NotFound","Payroll retro adjustment not found") 
            : mapper.Map<PayrollRetroAdjustmentDto>(adjustment);
    }

    public async Task<Result> MaterializeRetroAdjustment(Guid id, MaterializeRetroAdjustmentRequest request, Guid userId)
    {
        var adjustment = await context.PayrollRetroAdjustments.FirstOrDefaultAsync(p => p.Id == id);
        if (adjustment is null)
            return Error.NotFound("PayrollRetroAdjustment.NotFound", "Payroll retro adjustment not found");
        
        if (adjustment.Status != PayrollRetroAdjustmentStatus.Approved)
            return Error.Validation("PayrollRetroAdjustment.NotApproved",
                "Only approved adjustments can be materialized");
        
        var targetRun = await context.PayrollRuns.AnyAsync(r => r.Id == request.TargetRunId);
        if (!targetRun) return Error.NotFound("PayrollRun.NotFound", "Target payroll run not found");

        adjustment.Status = PayrollRetroAdjustmentStatus.Materialized;
        adjustment.TargetRunId = request.TargetRunId;
        
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> DeleteRetroAdjustment(Guid id, Guid userId)
    {
        var adjustment = await context.PayrollRetroAdjustments.FirstOrDefaultAsync(p => p.Id == id);
        if (adjustment is null)
            return Error.NotFound("PayrollRetroAdjustment.NotFound", "Payroll retro adjustment not found");
        
        adjustment.LastDeletedById = userId;
        adjustment.DeletedAt = DateTime.UtcNow;
        
        await context.SaveChangesAsync();
        return Result.Success();
    }
}