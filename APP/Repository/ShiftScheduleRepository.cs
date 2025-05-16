using System.Globalization;
using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.LeaveRequests;
using DOMAIN.Entities.ShiftAssignments;
using DOMAIN.Entities.ShiftSchedules;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public class ShiftScheduleRepository(ApplicationDbContext context, IMapper mapper) : IShiftScheduleRepository
{
    public async Task<Result<Guid>> CreateShiftSchedule(CreateShiftScheduleRequest request, Guid userId)
    {
        var shiftSchedule = await context.ShiftSchedules
            .FirstOrDefaultAsync(s => s.ScheduleName == request.ScheduleName && s.LastDeletedById == null);
        if (shiftSchedule is not null)
        {
            return Error.Validation("ShiftSchedule.Exists", "Shift schedule already exists.");
        }

        if (request.Frequency is ScheduleFrequency.Weekly or ScheduleFrequency.Biweekly && request.StartDate == null)
        {
            return Error.Validation("ShiftSchedule.StartDayRequired", "Start day is required for weekly and biweekly frequencies.");

        }

        if (!IsValidStartTime(request.StartTime))
        {
            return Error.Validation("ShiftSchedule.InvalidStartTime", "Start time must be in 12-hour format.");
        }
        
        var shiftTypes = await context.ShiftTypes
            .Where(shift => request.ShiftTypeIds.Contains(shift.Id))
            .ToListAsync();
        
        if (shiftTypes.Count != request.ShiftTypeIds.Count)
        {
            return Error.Validation("ShiftSchedule.InvalidShiftTypes", "One or more shift type IDs are invalid.");
        }
        
        var shiftScheduleEntity = mapper.Map<ShiftSchedule>(request);
        
        shiftScheduleEntity.ShiftTypes = shiftTypes;
        
        await context.ShiftSchedules.AddAsync(shiftScheduleEntity);
        await context.SaveChangesAsync();
        
        return shiftScheduleEntity.Id;
    }
    
    private static bool IsValidStartTime(string input)
    {
        return DateTime.TryParseExact(
            input,
            "h:mm tt", // supports 12-hour format with AM/PM
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out _);
    }

    public async Task<Result<Paginateable<IEnumerable<ShiftScheduleDto>>>> GetShiftSchedules(int page, int pageSize, string searchQuery)
    {
        var query = context.ShiftSchedules
            .Include(schedule => schedule.Department)
            .Include(schedule => schedule.ShiftTypes)
            .Where(sc => sc.LastDeletedById == null)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            query = query.WhereSearch(searchQuery);
        }

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            query = query.WhereSearch(searchQuery, q => q.Department.Name);
        }

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            query = query.WhereSearch(searchQuery, q => q.Frequency.ToString());
        }

        return await PaginationHelper.GetPaginatedResultAsync(
            query,
            page,
            pageSize,
            mapper.Map<ShiftScheduleDto>);
    }

    public async Task<Result<ShiftScheduleDto>> GetShiftSchedule(Guid id)
    {
        var shiftSchedule = await context.ShiftSchedules
            .Include(schedule => schedule.Department)
            .Include(schedule => schedule.ShiftTypes)
            .FirstOrDefaultAsync(s => s.Id == id && s.LastDeletedById == null);
        
        return shiftSchedule is null ? 
            Error.NotFound("ShiftSchedule.NotFound", "Shift schedule is not found") : 
            Result.Success(mapper.Map<ShiftScheduleDto>(shiftSchedule));
    }

  public async Task<Result> AssignEmployeesToShift(AssignShiftRequest request)
{
    var shiftSchedule = await context.ShiftSchedules
        .Include(s => s.ShiftTypes)
        .FirstOrDefaultAsync(s => s.Id == request.ShiftScheduleId);

    if (shiftSchedule is null)
        return Error.NotFound("Shift.NotFound", "Shift schedule not found.");

    if (shiftSchedule.StartDate is null)
        return Error.Validation("Shift.Invalid", "Shift Start Day is required.");

    var shiftDay = shiftSchedule.StartDate.Value;
    var employeeIds = request.EmployeeIds.Distinct().ToList();

    // 🔹 Get employees on approved leave for this day of the week
    var employeesOnLeave = await context.LeaveRequests
        .Where(l =>
            employeeIds.Contains(l.EmployeeId) &&
            l.LeaveStatus == LeaveStatus.Approved &&
            l.StartDate.DayOfWeek <= shiftDay &&
            l.EndDate.DayOfWeek >= shiftDay)
        .Select(l => l.EmployeeId)
        .Distinct()
        .ToListAsync();

    // 🔹 Check for restricted holidays on this day
    var isRestrictedHoliday = await context.Holidays
        .AnyAsync(h => h.Date.DayOfWeek == shiftDay);

    // 🔹 Load potential conflicting shift assignments
    var conflictingAssignments = await context.ShiftAssignments
        .Include(sa => sa.ShiftSchedules)
            .ThenInclude(ss => ss.ShiftTypes)
        .Where(sa =>
            employeeIds.Contains(sa.EmployeeId) &&
            sa.ShiftSchedules.StartDate == shiftDay)
        .ToListAsync();

    // 🔹 Detect overlaps in-memory (EF can't handle nested Any comparisons)
    var conflictingEmployeeIds = conflictingAssignments
        .Where(sa => sa.ShiftSchedules.ShiftTypes.Any(existing =>
            shiftSchedule.ShiftTypes.Any(newShift =>
                existing.StartTime < newShift.EndTime &&
                existing.EndTime > newShift.StartTime)))
        .Select(sa => sa.EmployeeId)
        .Distinct()
        .ToList();

    // 🔹 Filter valid employees for assignment
    var validAssignments = employeeIds
        .Where(id =>
            !employeesOnLeave.Contains(id) &&
            !isRestrictedHoliday &&
            !conflictingEmployeeIds.Contains(id))
        .Select(id => new ShiftAssignment
        {
            Id = Guid.NewGuid(),
            EmployeeId = id,
            ShiftScheduleId = shiftSchedule.Id
        })
        .ToList();

    if (validAssignments.Count == 0)
        return Error.Validation("Employees.NotFound", "No valid employees to assign due to leave, holidays, or conflicts.");

    await context.ShiftAssignments.AddRangeAsync(validAssignments);
    await context.SaveChangesAsync();

    return Result.Success();
}
    
    public async Task<Result> UpdateShiftSchedule(Guid id, CreateShiftScheduleRequest request, Guid userId)
    {
        var shiftSchedule = await context.ShiftSchedules
            .FirstOrDefaultAsync(s => s.Id == id && s.LastDeletedById == null);
        if (shiftSchedule is null)
        {
            return Error.NotFound("ShiftSchedule.NotFound", "Shift schedule is not found");
        }

        var shiftTypes = await context.ShiftTypes
            .Where(shift => request.ShiftTypeIds.Contains(shift.Id))
            .ToListAsync();
        if (shiftTypes.Count != request.ShiftTypeIds.Count)
        {
            return Error.Validation("ShiftSchedule.InvalidShiftTypes", "One or more shift type IDs are invalid.");
        }
        shiftSchedule.ShiftTypes.Clear();
        shiftSchedule.ShiftTypes.AddRange(shiftTypes);
        mapper.Map(request, shiftSchedule);
        
        context.ShiftSchedules.Update(shiftSchedule);
        await context.SaveChangesAsync();
        return Result.Success();
    }
    

    public async Task<Result> DeleteShiftSchedule(Guid id, Guid userId)
    {
        var shiftSchedule = await context.ShiftSchedules
            .FirstOrDefaultAsync(s => s.Id == id && s.LastDeletedById == null);
        
        if (shiftSchedule is null)
        {
            return Error.NotFound("ShiftSchedule.NotFound", "Shift schedule is not found");
        }
        
        context.ShiftSchedules.Update(shiftSchedule);
        await context.SaveChangesAsync();
        return Result.Success();
    }
}