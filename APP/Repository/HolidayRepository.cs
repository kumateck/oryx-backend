using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.Holidays;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public class HolidayRepository(ApplicationDbContext context, IMapper mapper) : IHolidayRepository
{
    public async Task<Result<Guid>> CreateHoliday(CreateHolidayRequest request, Guid userId)
    {
        var holiday = await context.Holidays.FirstOrDefaultAsync(h => h.Name == request.Name);

        if (holiday is not null)
        {
            return Error.Validation("Holiday.Exists", "Holiday already exists.");
        }
        
        var holidayEntity = mapper.Map<Holiday>(request);
        
        await context.Holidays.AddAsync(holidayEntity);
        await context.SaveChangesAsync();
        
        return holidayEntity.Id;
        
    }

    public async Task<Result<Paginateable<IEnumerable<HolidayDto>>>> GetHolidays(int page, int pageSize, string searchQuery)
    {
        var query = context.Holidays
            .Where(h => h.DeletedAt == null)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            query = query.WhereSearch(searchQuery, q => q.Name, q => q.Description);
        }

        return await 
            PaginationHelper.GetPaginatedResultAsync(query, page, pageSize, mapper.Map<HolidayDto>);
    }

    public async Task<Result<HolidayDto>> GetHoliday(Guid id)
    {
        var holiday = await context.Holidays.FirstOrDefaultAsync(h => h.Id == id);
        
        return holiday is null ? 
            Error.NotFound("Holiday.NotFound", "Holiday not found") : 
            Result.Success(mapper.Map<HolidayDto>(holiday));
    }

    public async Task<Result> UpdateHoliday(CreateHolidayRequest request, Guid id, Guid userId)
    {
        var holiday = await context.Holidays.FirstOrDefaultAsync(h => h.Id == id);

        if (holiday is null)
        {
            return Error.NotFound("Holiday.NotFound", "Holiday not found");
        }
        
        mapper.Map(request, holiday);
        context.Holidays.Update(holiday);
        
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> DeleteHoliday(Guid id, Guid userId)
    {
        var holiday = await context.Holidays.FirstOrDefaultAsync(h => h.Id == id);
        
        if (holiday is null)
        {
            return Error.NotFound("Holiday.NotFound", "Holiday not found");
        }
        
        context.Holidays.Update(holiday);
        await context.SaveChangesAsync();
        return Result.Success();
    }
}