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
    public async Task<Result<Guid>> CreateHoliday(CreateHolidayRequest request)
    {
        var holiday = await context.Holidays.FirstOrDefaultAsync(h => h.Name == request.Name && h.DeletedAt == null);

        if (holiday is not null)
        {
            return Error.Validation("Holiday.Exists", "Holiday already exists.");
        }
        
        var holidayEntity = mapper.Map<Holiday>(request);
        
        
        await context.Holidays.AddAsync(holidayEntity);
        await context.SaveChangesAsync();
        
        return holidayEntity.Id;
        
    }

    public async Task<Result<IEnumerable<HolidayDto>>> GetHolidays(string searchQuery)
    {
        var query = context.Holidays
            .AsSplitQuery()
            .Where(h => h.DeletedAt == null);

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            query = query.WhereSearch(searchQuery, q => q.Name, q => q.Description);
        }

        var holidays = await query.ToListAsync();
        var holidayDtos = mapper.Map<IEnumerable<HolidayDto>>(holidays);

        return Result.Success(holidayDtos);
    }

    public async Task<Result<HolidayDto>> GetHoliday(Guid id)
    {
        var holiday = await context.Holidays
            .AsSplitQuery()
            .FirstOrDefaultAsync(h => h.Id == id && h.DeletedAt == null);
        
        return holiday is null ? 
            Error.NotFound("Holiday.NotFound", "Holiday not found") : 
            Result.Success(mapper.Map<HolidayDto>(holiday));
    }

    public async Task<Result> UpdateHoliday(CreateHolidayRequest request, Guid id)
    {
        var holiday = await context.Holidays
            .FirstOrDefaultAsync(h => h.Id == id && h.DeletedAt == null);

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
        var holiday = await context.Holidays.FirstOrDefaultAsync(h => h.Id == id && h.DeletedAt == null);
        
        if (holiday is null)
        {
            return Error.NotFound("Holiday.NotFound", "Holiday not found");
        }
        
        holiday.DeletedAt = DateTime.UtcNow;
        holiday.LastDeletedById = userId;
        
        context.Holidays.Update(holiday);
        await context.SaveChangesAsync();
        return Result.Success();
    }
}