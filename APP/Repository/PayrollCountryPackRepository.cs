using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.PayrollCountryPack;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public class PayrollCountryPackRepository(ApplicationDbContext context, IMapper mapper) : IPayrollCountryPackRepository
{
    public async Task<Result<Guid>> CreatePayrollCountryPack(CreatePayrollCountryPackRequest request)
    {
        var exists = await context.PayrollCountryPacks.AnyAsync(p => p.Name == request.Name);
        if (exists) return Error.Validation("PayrollCountryPack.Exists", "Payroll country pack name already exists");
        
        var pack = mapper.Map<PayrollCountryPack>(request);
        await context.PayrollCountryPacks.AddAsync(pack);
        await context.SaveChangesAsync();
        return pack.Id;
    }

    public async Task<Result<Paginateable<IEnumerable<PayrollCountryPackDto>>>> GetPayrollCountryPacks(int page, int pageSize, string searchQuery, Guid? countryId = null)
    {
        var query = context.PayrollCountryPacks.AsQueryable();
        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.WhereSearch(searchQuery, q => q.Name, q => q.Code);
        }
        
        if (countryId.HasValue) query = query.Where(q => q.CountryId == countryId.Value);

        return await PaginationHelper
            .GetPaginatedResultAsync(query, page, pageSize, mapper.Map<PayrollCountryPackDto>);
    }

    public async Task<Result<PayrollCountryPackDto>> GetPayrollCountryPack(Guid id)
    {
        var countryPack = await context.PayrollCountryPacks.FirstOrDefaultAsync(p => p.Id == id);
        return countryPack is null ? Error.NotFound("PayrollCountryPack.NotFound","Payroll country pack not found") 
            : mapper.Map<PayrollCountryPackDto>(countryPack);
    }

    public async Task<Result> UpdatePayrollCountryPack(Guid id, CreatePayrollCountryPackRequest request)
    {
        var pack = await context.PayrollCountryPacks.FirstOrDefaultAsync(p => p.Id == id);
        
        if (pack is null) return Error.NotFound("PayrollCountryPack.NotFound", "Payroll country pack not found");
        
        mapper.Map(request, pack);
        context.PayrollCountryPacks.Update(pack);
        await context.SaveChangesAsync();
        
        return Result.Success();
    }

    public async Task<Result> DeletePayrollCountryPack(Guid id, Guid userId)
    {
        var pack = await context.PayrollCountryPacks.FirstOrDefaultAsync(p => p.Id == id);
        if (pack is null) return Error.NotFound("PayrollCountryPack.NotFound", "Payroll country pack not found");
        
        pack.LastDeletedById = userId;
        pack.DeletedAt = DateTime.UtcNow;
        
        await context.SaveChangesAsync();
        return Result.Success();
    }
}