using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.CompanyWorkingDays;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public class CompanyWorkingDaysRepository(ApplicationDbContext context, IMapper mapper) : ICompanyWorkingDaysRepository
{
    public async Task<Result> CreateCompanyWorkingDays(List<CompanyWorkingDaysRequest> companyWorkingDays, Guid userId)
    {
        if (companyWorkingDays.Count == 0)
        {
            return Error.Validation("CompanyWorkingDays.Invalid", "At least one working day must be selected.");
        }
        
        var existingRecords = await context.CompanyWorkingDays.ToListAsync();
        context.CompanyWorkingDays.RemoveRange(existingRecords);
        await context.SaveChangesAsync();

        foreach (var companyWorkingDay in companyWorkingDays)
        {
            var companyWorkingDayEntity = mapper.Map<CompanyWorkingDays>(companyWorkingDay);
            companyWorkingDayEntity.Day = companyWorkingDay.Day;
            companyWorkingDayEntity.CreatedById = userId;
            companyWorkingDayEntity.CreatedAt = DateTime.UtcNow;
            await context.CompanyWorkingDays.AddAsync(companyWorkingDayEntity);
        }
        
        
        
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result<Paginateable<IEnumerable<CompanyWorkingDaysDto>>>> GetCompanyWorkingDays(int page, int pageSize, string searchQuery)
    {
        var query = context.CompanyWorkingDays.AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            query = query.WhereSearch(searchQuery, d => d.Day.ToString());
        }
        return await PaginationHelper.GetPaginatedResultAsync(
            query,
            page,
            pageSize,
            mapper.Map<CompanyWorkingDaysDto>);
    }

    public async Task<Result<CompanyWorkingDaysDto>> GetCompanyWorkingDay(Guid id)
    {
        var workingDay = await context.CompanyWorkingDays.FirstOrDefaultAsync(d => d.Id == id);
        if (workingDay is null)
        {
            return Error.NotFound("CompanyWorkingDays.NotFound", "Company working days not found");
        }
        var companyWorkingDaysDto = mapper.Map<CompanyWorkingDaysDto>(workingDay);
        return Result.Success(companyWorkingDaysDto);
    }

    public async Task<Result> UpdateCompanyWorkingDays(Guid id, CompanyWorkingDaysRequest companyWorkingDaysDto, Guid userId)
    {
        var workingDay = await context.CompanyWorkingDays.FirstOrDefaultAsync(c => c.Id == id);

        if (workingDay is null)
        {
            return Error.NotFound("CompanyWorkingDays.NotFound", "Company working days not found");
        }
        
        mapper.Map(companyWorkingDaysDto, workingDay);
        workingDay.LastUpdatedById = userId;
        workingDay.UpdatedAt = DateTime.UtcNow;
        
        context.CompanyWorkingDays.Update(workingDay);
        await context.SaveChangesAsync();
        return Result.Success();
    }
}