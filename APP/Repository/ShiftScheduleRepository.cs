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
    public async Task<Result<Guid>> CreateShiftSchedule(CreateShiftScheduleRequest request)
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

    public async Task<Result<IEnumerable<ShiftScheduleDto>>> GetShiftScheduleRangeView(Guid shiftScheduleId, DateTime startDate, DateTime endDate)
    {
        var schedule = await context.ShiftAssignments
            .Where(s => s.ShiftScheduleId == shiftScheduleId 
            && s.ScheduleDate >= startDate && s.ScheduleDate <= endDate)
            .Include(sa => sa.Employee)
            .Include(sa => sa.ShiftCategory)
            .Include(sa => sa.ShiftSchedules)
            .AsQueryable()
            .ToListAsync();
        
        var mapped = mapper.Map<IEnumerable<ShiftScheduleDto>>(schedule);
        return Result.Success(mapped);
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
    
    var shiftCategory = await context.ShiftCategories
        .FirstOrDefaultAsync(sh => sh.Name == request.ShiftName && sh.LastDeletedById == null);

    if (shiftCategory == null)
    {
        return Error.NotFound("ShiftCategory.NotFound", "Shift category not found.");
    }

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
    
    var conflictingEmployeeIds = conflictingAssignments
        .Where(sa => sa.ShiftSchedules.ShiftTypes.Any(existing =>
            shiftSchedule.ShiftTypes.Any(newShift =>
                ConvertTime(existing.StartTime) < ConvertTime(newShift.EndTime) &&
                ConvertTime(existing.EndTime) > ConvertTime(newShift.StartTime))))
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
            ShiftScheduleId = shiftSchedule.Id,
            ShiftCategoryId = request.ShiftCategoryId,
            ScheduleDate = request.ScheduleDate
        })
        .ToList();

    if (validAssignments.Count == 0)
        return Error.Validation("Employees.NotFound", "No valid employees to assign due to leave, holidays, or conflicts.");

    await context.ShiftAssignments.AddRangeAsync(validAssignments);
    await context.SaveChangesAsync();

    return Result.Success();
}

private static TimeOnly ConvertTime(string time)
{
    return TimeOnly.ParseExact(time, "hh:mm tt", CultureInfo.InvariantCulture);
}
    
    public async Task<Result> UpdateShiftSchedule(Guid id, CreateShiftScheduleRequest request)
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

  public async Task<Result> UpdateShiftAssignment(Guid id, UpdateShiftAssignment request)
{
    var shiftSchedule = await context.ShiftSchedules
        .Include(s => s.ShiftTypes)
        .FirstOrDefaultAsync(s => s.Id == id);

    if (shiftSchedule == null)
        return Error.NotFound("Shift.NotFound", "Shift schedule not found.");

    if (shiftSchedule.StartDate == null)
        return Error.Validation("Shift.Invalid", "Shift Start Day is required.");

    var shiftDay = request.ScheduleDate.Date;

    // 🔹 Remove employees from shift
    bool? isRemovable = request.RemoveEmployeeIds.Count != 0;

    if (isRemovable == true)
    {
        var assignmentsToRemove = await context.ShiftAssignments
            .Where(sa =>
                request.RemoveEmployeeIds.Contains(sa.EmployeeId) &&
                sa.ShiftScheduleId == id &&
                sa.ScheduleDate.Date == shiftDay)
            .ToListAsync();

        if (assignmentsToRemove.Count != 0)
            context.ShiftAssignments.RemoveRange(assignmentsToRemove);
    }

    // 🔹 Add new employees to shift
    bool? isAdded = request.AddEmployeeIds.Count != 0;

    if (isAdded == true)
    {
        var employeeIds = request.AddEmployeeIds.Distinct().ToList();

        // Check leave, holidays, conflicts
        var employeesOnLeave = await context.LeaveRequests
            .Where(l =>
                employeeIds.Contains(l.EmployeeId) &&
                l.LeaveStatus == LeaveStatus.Approved &&
                l.StartDate.Date <= shiftDay &&
                l.EndDate.Date >= shiftDay)
            .Select(l => l.EmployeeId)
            .ToListAsync();

        var isHoliday = await context.Holidays
            .AnyAsync(h => h.Date.Date == shiftDay);

        var conflictingAssignments = await context.ShiftAssignments
            .Include(sa => sa.ShiftSchedules)
                .ThenInclude(ss => ss.ShiftTypes)
            .Where(sa =>
                employeeIds.Contains(sa.EmployeeId) &&
                sa.ScheduleDate.Date == shiftDay)
            .ToListAsync();

        var conflictingIds = conflictingAssignments
            .Where(sa => sa.ShiftSchedules.ShiftTypes.Any(existing =>
                shiftSchedule.ShiftTypes.Any(newShift =>
                    ConvertTime(existing.StartTime) < ConvertTime(newShift.EndTime) &&
                    ConvertTime(existing.EndTime) > ConvertTime(newShift.StartTime))))
            .Select(sa => sa.EmployeeId)
            .ToList();

        var validNewAssignments = employeeIds
            .Where(id =>
                !employeesOnLeave.Contains(id) &&
                !isHoliday &&
                !conflictingIds.Contains(id))
            .Select(id => new ShiftAssignment
            {
                Id = Guid.NewGuid(),
                EmployeeId = id,
                ShiftScheduleId = shiftSchedule.Id,
                ShiftCategoryId = request.ShiftCategoryId,
                ScheduleDate = shiftDay
            })
            .ToList();

        await context.ShiftAssignments.AddRangeAsync(validNewAssignments);
    }

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