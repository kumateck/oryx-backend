using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.PayrollCompanies;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public class PayrollCompanyRepository(ApplicationDbContext context, IMapper mapper) : IPayrollCompanyRepository
{
    public async Task<Result<Guid>> CreatePayrollCompany(CreatePayrollCompanyRequest request)
    {
        var currency = await context.Currencies.AnyAsync(c => c.Id == request.CurrencyId);
        if (!currency) return Error.NotFound("Currency.NotFound", "Currency not found");        
        
        var country = await context.Countries.AnyAsync(c => c.Id == request.CountryId);
        if (!country) return Error.NotFound("Country.NotFound", "Country not found");
        
        var site = await context.Sites.AnyAsync(c => c.Id == request.SiteId);
        if (!site) return Error.NotFound("Site.NotFound", "Site not found");
        
        // review the employer statutory id
        // var ssnitNumber = await context.Employees.AnyAsync(c => c.SsnitNumber == request.StatutoryEmployerId);
        // if (ssnitNumber) return Error.NotFound("StatutoryEmployee.NotFound", "Statutory employee not found");
        //
        var payrollCompany = mapper.Map<PayrollCompany>(request);
        await context.PayrollCompanies.AddAsync(payrollCompany);
        await context.SaveChangesAsync();
        return payrollCompany.Id;
    }

    public async Task<Result<Paginateable<IEnumerable<PayrollCompanyDto>>>> GetPayrollCompanies(int page, int pageSize, string searchQuery)
    {
        var query = context.PayrollCompanies.AsQueryable();
        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.WhereSearch(searchQuery, q => q.Code,
                q => q.Name, q=> q.LegalName);
        }

        return await PaginationHelper.GetPaginatedResultAsync(query, page, pageSize, mapper.Map<PayrollCompanyDto>);
    }

    public async Task<Result<PayrollCompanyDto>> GetPayrollCompany(Guid payrollCompanyId)
    {
        var payrollCompany = await context.PayrollCompanies.FirstOrDefaultAsync(p => p.Id == payrollCompanyId);
        return payrollCompany is null ? Error.NotFound("PayrollCompany.NotFound", "Payroll Company not found")
            : mapper.Map<PayrollCompanyDto>(payrollCompany);
    }

    public async Task<Result> UpdatePayrollCompany(Guid payrollCompanyId, CreatePayrollCompanyRequest request)
    {
        var payrollCompany = await context.PayrollCompanies.FirstOrDefaultAsync(p => p.Id == payrollCompanyId);

        if (payrollCompany is null) return Error.NotFound("PayrollCompany.NotFound", "Payroll company not found");
        
        mapper.Map(request, payrollCompany);
        
        await context.SaveChangesAsync();
        return Result.Success();

    }

    public async Task<Result> DeletePayrollCompany(Guid payrollCompanyId, Guid id)
    {
        var payrollCompany = await context.PayrollCompanies.FirstOrDefaultAsync(p => p.Id == payrollCompanyId);

        if (payrollCompany is null) return Error.NotFound("PayrollCompany.NotFound", "Payroll company not found");
        
        var hasPayGroups = await context.PayrollCompanies.AnyAsync(p => p.PayGroups.Count > 0);
        if (hasPayGroups) return Error.Validation("PayrollCompany.InUse", "Payroll company has associated " +
                                                                          "pay groups and cannot be deleted");

        payrollCompany.LastDeletedById = id;
        payrollCompany.DeletedAt = DateTime.UtcNow;
        
        await context.SaveChangesAsync();
        return Result.Success();

    }
}