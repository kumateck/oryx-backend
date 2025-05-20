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
    public async Task<Result> CreateCompanyWorkingDays(List<CompanyWorkingDaysRequest> companyWorkingDays)
    {
        if (companyWorkingDays == null || companyWorkingDays.Count == 0)
        {
            return Error.Validation("CompanyWorkingDays.Invalid", "At least one working day must be selected.");
        }

        var existingDays = await context.CompanyWorkingDays.ToListAsync();

        if (existingDays.Count == 0)
        {
            var newEntities = mapper.Map<List<CompanyWorkingDays>>(companyWorkingDays);
            await context.CompanyWorkingDays.AddRangeAsync(newEntities);
        }
        else
        {
            foreach (var request in companyWorkingDays)
            {
                var existing = existingDays.FirstOrDefault(e => e.Day == request.Day);
                if (existing != null)
                {
                    mapper.Map(request, existing);
                }
                else
                {
                    // Add new if not present
                    var newEntity = mapper.Map<CompanyWorkingDays>(request);
                    await context.CompanyWorkingDays.AddAsync(newEntity);
                }
            }
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

}