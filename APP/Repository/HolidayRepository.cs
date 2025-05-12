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

        var shiftSchedules = await context.ShiftSchedules
            .Where(sc => request.ShiftSchedules.Contains(sc.Id))
            .ToListAsync();

        if (shiftSchedules.Count != request.ShiftSchedules.Count)
        {
            return Error.Validation("Holiday.InvalidShiftSchedules", "One or more shift schedule IDs are invalid.");
        }
        
        var holidayEntity = mapper.Map<Holiday>(request);
        
        holidayEntity.ShiftSchedules = shiftSchedules;
        
        await context.Holidays.AddAsync(holidayEntity);
        await context.SaveChangesAsync();
        
        return holidayEntity.Id;
        
    }

    public async Task<Result<Paginateable<IEnumerable<HolidayDto>>>> GetHolidays(int page, int pageSize, string searchQuery)
    {
        var query = context.Holidays
            .AsSplitQuery()
            .Include(h => h.ShiftSchedules)
                .ThenInclude(h=> h.ShiftTypes)
            .Include(h => h.ShiftSchedules)
                .ThenInclude(h=> h.Department)
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
        var holiday = await context.Holidays
            .AsSplitQuery()
            .Include(h => h.ShiftSchedules)
                .ThenInclude(h=> h.ShiftTypes)
            .Include(h => h.ShiftSchedules)
                .ThenInclude(h=> h.Department)
            .FirstOrDefaultAsync(h => h.Id == id && h.DeletedAt == null);
        
        return holiday is null ? 
            Error.NotFound("Holiday.NotFound", "Holiday not found") : 
            Result.Success(mapper.Map<HolidayDto>(holiday));
    }

    public async Task<Result> UpdateHoliday(CreateHolidayRequest request, Guid id)
    {
        var holiday = await context.Holidays
            .Include(h => h.ShiftSchedules)
            .FirstOrDefaultAsync(h => h.Id == id && h.DeletedAt == null);

        if (holiday is null)
        {
            return Error.NotFound("Holiday.NotFound", "Holiday not found");
        }
        
        var shiftSchedules = await context.ShiftSchedules
            .Where(sc => request.ShiftSchedules.Contains(sc.Id))
            .ToListAsync();

        if (shiftSchedules.Count != request.ShiftSchedules.Count)
        {
            return Error.Validation("Holiday.InvalidShiftSchedules", "One or more shift schedule IDs are invalid.");
        }

        mapper.Map(request, holiday);
        
        holiday.ShiftSchedules.Clear();
        holiday.ShiftSchedules.AddRange(shiftSchedules);
        
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