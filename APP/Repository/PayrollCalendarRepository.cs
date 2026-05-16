using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.PayrollCalendars;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public class PayrollCalendarRepository(ApplicationDbContext context, IMapper mapper) : IPayrollCalenderRepository
{
    public async Task<Result<Guid>> CreatePayrollCalendar(CreatePayrollCalendarRequest request)
    {
        var company = await context.PayrollCompanies.AnyAsync(p => p.Id == request.PayrollCompanyId);
        if (!company) return Error.NotFound("PayrollCompany.NotFound", "Payroll company not found");
        
        var calendar = mapper.Map<PayrollCalendar>(request);
        await context.PayrollCalendars.AddAsync(calendar);
        await context.SaveChangesAsync();
        
        return calendar.Id;
    }

    public async Task<Result<Paginateable<IEnumerable<PayrollCalendarDto>>>> GetPayrollCalendars(int page, int pageSize, string searchQuery, Guid? payrollCompanyId = null)
    {
        var query = context.PayrollCalendars.AsQueryable();
        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            query = query.WhereSearch(searchQuery,p => p.Name);
        }
        
        return await PaginationHelper
            .GetPaginatedResultAsync(query, page, pageSize, mapper.Map<PayrollCalendarDto>);
    }

    public async Task<Result<PayrollCalendarDto>> GetPayrollCalendar(Guid id)
    {
        var company = await context.PayrollCalendars.FirstOrDefaultAsync(p => p.Id == id);

        return company is null ? Error.NotFound("PayrollCalendar.NotFound", "Payroll calendar not found") 
            : mapper.Map<PayrollCalendarDto>(company);
    }

    public async Task<Result> UpdatePayrollCalendar(Guid id, CreatePayrollCalendarRequest request)
    {
        var calendar = await context.PayrollCalendars.FirstOrDefaultAsync(p => p.Id == id);
        
        if (calendar is null) return Error.NotFound("PayrollCalendar.NotFound", "Payroll calendar not found");
        
        mapper.Map(request, calendar);
        context.PayrollCalendars.Update(calendar);
        await context.SaveChangesAsync();
        
        return Result.Success();
    }

    public async Task<Result> DeletePayrollCalendar(Guid id, Guid userId)
    {
        var calendar = await context.PayrollCalendars.FirstOrDefaultAsync(p => p.Id == id);
        if (calendar is null) return Error.NotFound("PayrollCalendar.NotFound", "Payroll calendar not found");
        
        calendar.DeletedAt = DateTime.UtcNow;
        calendar.LastDeletedById = userId;
        
        await context.SaveChangesAsync();
        return Result.Success();
        
    }
}