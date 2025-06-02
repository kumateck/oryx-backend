using System.Globalization;
using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.ShiftTypes;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public class ShiftTypeRepository(ApplicationDbContext context, IMapper mapper) : IShiftTypeRepository
{
    public async Task<Result<Guid>> CreateShiftType(CreateShiftTypeRequest request)
    {
        var existingShiftType = await context.ShiftTypes
            .FirstOrDefaultAsync(s => s.ShiftName == request.ShiftName && s.DeletedAt == null);

        if (existingShiftType != null)
        {
            return Error.Validation("ShiftType.Exists", "Shift type already exists.");
        }
        
        if (!DateTime.TryParseExact(request.StartTime, "hh:mm tt", CultureInfo.InvariantCulture, DateTimeStyles.None, out _) ||
            !DateTime.TryParseExact(request.EndTime, "hh:mm tt", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
        {
            return Error.Validation("ShiftType.InvalidTime", "Start and End times must be in 12-hour format (e.g., 08:30 AM).");
        }

      
        if (request.ApplicableDays.Count == 0)
        {
            return Error.Validation("ShiftType.InvalidDays", "At least one day must be selected.");
        }
        
        var shiftType = mapper.Map<ShiftType>(request);
 
        await context.ShiftTypes.AddAsync(shiftType);
        await context.SaveChangesAsync();
        
        return shiftType.Id;
    }

    private static TimeOnly ConvertTime(string time)
    {
        return TimeOnly.ParseExact(time, "hh:mm tt", CultureInfo.InvariantCulture);
    }

    public async Task<Result<Paginateable<IEnumerable<ShiftTypeDto>>>> GetShiftTypes(int page, int pageSize, string searchQuery)
    {
        var query = context.ShiftTypes
            .Where(st => st.LastDeletedById == null)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            query = query.WhereSearch(searchQuery, q => q.ShiftName);
        }

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            query = query.WhereSearch(searchQuery, q => q.RotationType.ToString());
        }

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            if (Enum.TryParse<DayOfWeek>(searchQuery, true, out var day))
            {
                query = query.Where(q => q.ApplicableDays.Contains(day));
            }
        }
        
        return await PaginationHelper.GetPaginatedResultAsync(
            query,
            page,
            pageSize,
            mapper.Map<ShiftTypeDto>);
    }

    public async Task<Result<ShiftTypeDto>> GetShiftType(Guid id)
    {
        var shiftType = await context.ShiftTypes.FirstOrDefaultAsync(s => s.Id == id && s.DeletedAt == null);
        if (shiftType is null)
        {
            return Error.NotFound("ShiftType.NotFound", "Shift type is not found");
        }
        var shiftTypeDto = mapper.Map<ShiftTypeDto>(shiftType);
        return Result.Success(shiftTypeDto);
    }

    public async Task<Result> UpdateShiftType(Guid id, CreateShiftTypeRequest request)
    {
        var shiftType = await context.ShiftTypes
            .FirstOrDefaultAsync(s => s.Id == id && s.DeletedAt == null);
        
        if (shiftType is null)
        {
            return Error.NotFound("ShiftType.NotFound", "Shift type is not found");
        }
        
        if (!request.StartTime.Contains("AM") || request.StartTime.Contains("PM") 
            || !request.EndTime.Contains("AM") || request.EndTime.Contains("PM"))
        {
            return Error.Validation("ShiftType.InvalidTime", "Start time must be in 12 hour format.");
        }

        mapper.Map(request, shiftType);
        
        context.ShiftTypes.Update(shiftType);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> DeleteShiftType(Guid id, Guid userId)
    {
        var shiftType = await context.ShiftTypes
            .FirstOrDefaultAsync(s => s.Id == id && s.LastDeletedById == null);

        if (shiftType is null)
        {
            return Error.NotFound("ShiftType.NotFound", "Shift type is not found");
        }

        var shiftSchedule = await context.ShiftSchedules.FirstOrDefaultAsync(sc => sc.ShiftTypes.Contains(shiftType));

        if (shiftSchedule is not null)
        {
            return Error.Validation("ShiftType.InUse", "Shift type is in use by a shift schedule.");
        }
        
        shiftType.LastDeletedById = userId;
        shiftType.DeletedAt = DateTime.UtcNow;
        
        context.ShiftTypes.Update(shiftType);
        await context.SaveChangesAsync();
        return Result.Success();
    }
}