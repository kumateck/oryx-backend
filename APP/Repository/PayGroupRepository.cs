using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.PayGroups;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public class PayGroupRepository(ApplicationDbContext context, IMapper mapper) : IPayGroupRepository
{
    public async Task<Result<Guid>> CreatePayGroup(CreatePayGroupRequest request)
    {
        var exists = await context.PayGroups.AnyAsync(p => p.Code == request.Code 
        && p.PayrollCompanyId == request.PayrollCompanyId);
        if (exists)
            return Error.Conflict("PayGroup.DuplicateCode", $"A pay group with code" +
                                                            $" '{request.Code}' already exists for this company.");
        
        var calendarExists = await context.PayrollCalendars.AnyAsync(p => p.Id == request.PayrollCalendarId);
        if (!calendarExists) return Error.NotFound("PayrollCalendar.NotFound", "Payroll calendar not found");

        var currencyExists = await context.Currencies.AnyAsync(c => c.Id == request.DefaultCurrencyId);
        if (!currencyExists) return Error.NotFound("Currency.NotFound", "Currency not found");
        
        var payGroup = mapper.Map<PayGroup>(request);
        await context.PayGroups.AddAsync(payGroup);
        await context.SaveChangesAsync();
        return payGroup.Id;
    }

    public async Task<Result<Paginateable<IEnumerable<PayGroupDto>>>> GetPayGroups(int page, int pageSize, string searchQuery, Guid? payrollCompanyId = null)
    {
        var query = context.PayGroups.AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchQuery))
            query = query.WhereSearch(searchQuery, q => q.Name, q => q.Code);

        if (payrollCompanyId.HasValue)
            query = query.Where(pg => pg.PayrollCompanyId == payrollCompanyId.Value);

        return await PaginationHelper.GetPaginatedResultAsync(query, page, pageSize, mapper.Map<PayGroupDto>);
    }
    

    public async Task<Result<PayGroupDto>> GetPayGroup(Guid id)
    {
        var payGroup = await context.PayGroups.FirstOrDefaultAsync(p => p.Id == id);
        return payGroup is null ? Error.NotFound("PayGroup.NotFound","PayGroup not found")
            : mapper.Map<PayGroupDto>(payGroup);
    }

    public async Task<Result> UpdatePayGroup(Guid id, CreatePayGroupRequest request)
    {
        var payGroup = await context.PayGroups.FirstOrDefaultAsync(pg => pg.Id == id);
        if (payGroup is null)
            return Error.NotFound("PayGroup.NotFound", "Pay group not found");

        mapper.Map(request, payGroup);
        context.PayGroups.Update(payGroup);
        await context.SaveChangesAsync();

        return Result.Success();
    }
 
    public async Task<Result> DeletePayGroup(Guid id, Guid userId)
    {
        var payGroup = await context.PayGroups.FirstOrDefaultAsync(pg => pg.Id == id);
        if (payGroup is null)
            return Error.NotFound("PayGroup.NotFound", "Pay group not found");

        var hasProfiles = await context.EmployeePayrollProfiles.AnyAsync(p => p.PayGroupId == id);
        if (hasProfiles)
            return Error.Validation("PayGroup.InUse", "Pay group has associated employee profiles and cannot be deleted.");

        payGroup.DeletedAt = DateTime.UtcNow;
        payGroup.LastDeletedById = userId;

        context.PayGroups.Update(payGroup);
        await context.SaveChangesAsync();

        return Result.Success();
    }
}