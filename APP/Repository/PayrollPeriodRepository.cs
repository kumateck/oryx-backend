using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.PayRollPeriods;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public class PayrollPeriodRepository(ApplicationDbContext context, IMapper mapper) : IPayrollPeriodRepository
{
    public async Task<Result<Guid>> CreatePayrollPeriod(CreatePayrollPeriodRequest request)
    {
        var overlapping = await context.PayrollPeriods
            .AnyAsync(p=> p.PayrollCompanyId == request.PayrollCompanyId
            && p.PayrollCalendarId == request.PayrollCalendarId
            && p.PeriodStart <= request.PeriodEnd
            && p.PeriodEnd >= request.PeriodStart);
        
        if (overlapping)
            return Error.Validation("PayrollPeriod.Overlapping", "An overlapping payroll period exists.");
        
        var period = mapper.Map<PayrollPeriod>(request);
        await context.PayrollPeriods.AddAsync(period);
        await context.SaveChangesAsync();
        
        return period.Id;
        
    }

    public async Task<Result<Paginateable<IEnumerable<PayrollPeriodDto>>>> GetPayrollPeriods(int page, int pageSize,
        string searchQuery, Guid? payrollCompanyId = null,
        PayrollPeriodStatus? status = null)
    {
        var query = context.PayrollPeriods.AsQueryable();
        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            query = query.WhereSearch(searchQuery, q=> q.Code);
        }
        
        if (payrollCompanyId.HasValue) query = query.Where(p => p.PayrollCompanyId == payrollCompanyId.Value);
        
        if (Enum.TryParse<PayrollPeriodStatus>(searchQuery, true, out var periodStatus))
        {
            query = query.Where(p => p.Status == periodStatus);
        }
        
        return await PaginationHelper.GetPaginatedResultAsync(query, page, pageSize, mapper.Map<PayrollPeriodDto>);
    }

    public async Task<Result<PayrollPeriodDto>> GetPayrollPeriod(Guid id)
    {
        var  period = await context.PayrollPeriods.FirstOrDefaultAsync(p => p.Id == id);
        return period is null ? Error.NotFound("PayrollPeriod.NotFound", "Payroll period not found")
            : mapper.Map<PayrollPeriodDto>(period);
    }

    public async Task<Result> UpdatePayrollPeriod(Guid id, CreatePayrollPeriodRequest request)
    {
        var period = await context.PayrollPeriods.FirstOrDefaultAsync(p => p.Id == id);
        if (period is null) return Error.NotFound("PayrollPeriod.NotFound", "Payroll period not found");
        
        if (period.Status != PayrollPeriodStatus.Open) 
            return Error.Validation("PayrollPeriod.NotOpen", "Only open periods can be updated");
        
        mapper.Map(request, period);
        context.PayrollPeriods.Update(period);
        
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> ClosePayrollPeriod(ClosePayrollPeriodRequest request, Guid userId)
    {   
        var period = await context.PayrollPeriods.FirstOrDefaultAsync(p => p.Id == request.PeriodId); 
        if (period is null) return Error.NotFound("PayrollPeriod.NotFound",
            "Payroll period not found");

        switch (period.Status)
        {
            case PayrollPeriodStatus.Open:
                period.Status = PayrollPeriodStatus.SoftClosed;
                period.SoftClosedAt = DateTime.UtcNow;
                break;
            case PayrollPeriodStatus.SoftClosed:
                period.Status = PayrollPeriodStatus.HardClosed;
                period.HardClosedAt = DateTime.UtcNow;
                break;
            case PayrollPeriodStatus.HardClosed:
            default:
                return Error.Validation("PayrollPeriod.AlreadyClosed",
                    "Period is already hard-closed.");
        }
        
        period.ClosedById = userId;
             
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> ReopenPayrollPeriod(ReopenPayrollPeriodRequest request)
    {
        var period = await context.PayrollPeriods.FirstOrDefaultAsync(p => p.Id == request.PeriodId); 
        if (period is null) return Error.NotFound("PayrollPeriod.NotFound",
            "Payroll period not found");
        
        if (period.Status == PayrollPeriodStatus.Open) 
            return Error.Validation("PayrollPeriod.AlreadyOpen", "Period is already open");
        
        period.Status = PayrollPeriodStatus.Open;
        period.ClosedById = null;
        period.HardClosedAt = null;
        period.SoftClosedAt = null;
        
        await context.SaveChangesAsync();
        return Result.Success();

    }

    public async Task<Result> DeletePayrollPeriod(Guid id, Guid userId)
    {
        var period = await context.PayrollPeriods.FirstOrDefaultAsync(p => p.Id == id);
        if (period is null) return Error.NotFound("PayrollPeriod.NotFound","Payroll period not found");
        
        var hasRuns = await context.PayrollRuns.AnyAsync(p => p.PayrollPeriodId == id);
        if (hasRuns) return Error.Validation("PayrollPeriod.InUse",
            "Period has associated payroll runs and cannot be deleted");
        
        period.DeletedAt = DateTime.UtcNow;
        period.LastDeletedById = userId;
        
        await context.SaveChangesAsync();
        return Result.Success();
    }
}