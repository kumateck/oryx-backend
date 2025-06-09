using System.Globalization;
using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.Employees;
using DOMAIN.Entities.LeaveRequests;
using DOMAIN.Entities.ShiftAssignments;
using DOMAIN.Entities.ShiftSchedules;
using DOMAIN.Entities.ShiftTypes;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver.Linq;
using SHARED;

namespace APP.Repository;

public class ShiftScheduleRepository(ApplicationDbContext context, IMapper mapper) : IShiftScheduleRepository
{
    public async Task<Result<Guid>> CreateShiftSchedule(CreateShiftScheduleRequest request)
    {
        var shiftSchedule = await EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(context.ShiftSchedules, s => s.ScheduleName == request.ScheduleName && s.LastDeletedById == null);
        if (shiftSchedule is not null)
        {
            return Error.Validation("ShiftSchedule.Exists", "Shift schedule already exists.");
        }
        
        var shiftTypes = await EntityFrameworkQueryableExtensions.ToListAsync(context.ShiftTypes
                .Where(shift => request.ShiftTypeIds.Contains(shift.Id)));
        
        if (shiftTypes.Count != request.ShiftTypeIds.Count)
        {
            return Error.Validation("ShiftSchedule.InvalidShiftTypes", "One or more shift type IDs are invalid.");
        }
        
        
        var shiftScheduleEntity = mapper.Map<ShiftSchedule>(request);
        
        shiftScheduleEntity.ShiftTypes = shiftTypes;
        shiftScheduleEntity.ScheduleStatus = ScheduleStatus.New;
        
        shiftScheduleEntity.EndDate = ComputeEndDate(request.StartDate, shiftScheduleEntity.Frequency);
        
        await context.ShiftSchedules.AddAsync(shiftScheduleEntity);
        await context.SaveChangesAsync();
        
        return shiftScheduleEntity.Id;
    }

    private static DateTime ComputeEndDate(DateTime startDate, ScheduleFrequency frequency)
    {
        return frequency switch
        {
            ScheduleFrequency.Week => startDate.AddDays(6),
            ScheduleFrequency.Month => startDate.AddMonths(13),
            ScheduleFrequency.Biweek => startDate.AddWeeks(1).AddDays(-1),
            ScheduleFrequency.Quarter => startDate.AddMonths(3).AddDays(-1),
            ScheduleFrequency.Half => startDate.AddMonths(6).AddDays(-1),
            ScheduleFrequency.Year => startDate.AddYears(1).AddDays(-1),
            _ => startDate
        };
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
            query = query.WhereSearch(searchQuery, q => q.Department.Name);
        }

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            if (Enum.TryParse<ScheduleFrequency>(searchQuery, true, out var parsedFrequency))
            {
               query = query.Where(q => q.Frequency == parsedFrequency); 
            }
            
        }

        return await PaginationHelper.GetPaginatedResultAsync(
            query,
            page,
            pageSize,
            mapper.Map<ShiftScheduleDto>);
    }

    public async Task<Result<ShiftScheduleDto>> GetShiftSchedule(Guid id)
    {
        var shiftSchedule = await EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(context.ShiftSchedules
                .Include(schedule => schedule.Department)
                .Include(schedule => schedule.ShiftTypes), s => s.Id == id && s.LastDeletedById == null);
        
        return shiftSchedule is null ? 
            Error.NotFound("ShiftSchedule.NotFound", "Shift schedule is not found") : 
            Result.Success(mapper.Map<ShiftScheduleDto>(shiftSchedule));
    }

    public async Task<Result<IEnumerable<ShiftAssignmentDto>>> GetShiftScheduleRangeView(Guid shiftScheduleId, DateTime startDate, DateTime endDate)
    {
        var schedule = await EntityFrameworkQueryableExtensions.ToListAsync(context.ShiftAssignments
                .Where(s => s.ShiftScheduleId == shiftScheduleId
                            && s.ScheduleDate.Date >= startDate.Date && s.ScheduleDate.Date <= endDate.Date)
                .Include(s => s.Employee)
                .ThenInclude(e => e.Designation)
                .Include(s => s.Employee)
                .ThenInclude(e => e.Department)
                .Include(sa => sa.ShiftType)
                .Include(sa => sa.ShiftCategory)
                .Include(sa => sa.ShiftSchedules));

        var grouped = schedule
            .GroupBy(s => new
            {
                s.ScheduleDate.Date,
                s.ShiftCategoryId,
                s.ShiftTypeId,
                s.ShiftScheduleId
            })
            .Select(g => new ShiftAssignmentDto
            {
                ScheduleDate = g.Key.Date,
                ShiftCategory = g.Select(s => new ShiftCategoryDto
                {
                    Id = s.ShiftCategoryId,
                    Name = s.ShiftCategory?.Name
                }).FirstOrDefault(),

                ShiftType = g.Select(s => new MinimalShiftTypeDto
                {
                    ShiftTypeId = s.ShiftTypeId,
                    ShiftName = s.ShiftType?.ShiftName
                }).FirstOrDefault(),

                ShiftSchedule = g.Select(s => new MinimalShiftScheduleDto
                {
                    ScheduleId = s.ShiftScheduleId,
                    ScheduleName = s.ShiftSchedules?.ScheduleName
                }).FirstOrDefault(),

                Employees = g.Select(s => new MinimalEmployeeInfoDto
                {
                    EmployeeId = s.Employee.Id,
                    FirstName = s.Employee.FirstName,
                    LastName = s.Employee.LastName,
                    StaffNumber = s.Employee.StaffNumber,
                    Type = s.Employee.Type.ToString(),
                    Department = s.Employee.Department?.Name,
                    Designation = s.Employee.Designation?.Name
                }).Distinct().ToList()
            }).ToList();

        return Result.Success(grouped.AsEnumerable());
    }
    
      public async Task<Result<IEnumerable<ShiftAssignmentDto>>> GetShiftScheduleDayView(Guid shiftScheduleId, DateTime date)
    {
        var schedule = await EntityFrameworkQueryableExtensions.ToListAsync(context.ShiftAssignments
                .Where(s => s.ShiftScheduleId == shiftScheduleId
                            && s.ScheduleDate.Date == date.Date)
                .Include(s => s.Employee)
                .ThenInclude(e => e.Designation)
                .Include(s => s.Employee)
                .ThenInclude(e => e.Department)
                .Include(sa => sa.ShiftType)
                .Include(sa => sa.ShiftCategory)
                .Include(sa => sa.ShiftSchedules));

        var grouped = schedule
            .GroupBy(s => new 
            {
                s.ScheduleDate.Date,
                s.ShiftScheduleId,
                s.ShiftCategoryId,
                s.ShiftTypeId
            })
            .Select(g => new ShiftAssignmentDto
            {
                ScheduleDate = g.Key.Date,
                ShiftCategory = g.Select(s => new ShiftCategoryDto
                {
                    Id = s.ShiftCategoryId,
                    Name = s.ShiftCategory?.Name
                }).FirstOrDefault(),

                ShiftType = g.Select(s => new MinimalShiftTypeDto
                {
                    ShiftTypeId = s.ShiftTypeId,
                    ShiftName = s.ShiftType?.ShiftName
                }).FirstOrDefault(),

                ShiftSchedule = g.Select(s => new MinimalShiftScheduleDto
                {
                    ScheduleId = s.ShiftScheduleId,
                    ScheduleName = s.ShiftSchedules?.ScheduleName
                }).FirstOrDefault(),

                Employees = g.Select(s => new MinimalEmployeeInfoDto
                {
                    EmployeeId = s.Employee.Id,
                    FirstName = s.Employee.FirstName,
                    LastName = s.Employee.LastName,
                    StaffNumber = s.Employee.StaffNumber,
                    Type = s.Employee.Type.ToString(),
                    Department = s.Employee.Department?.Name,
                    Designation = s.Employee.Designation?.Name
                }).Distinct().ToList()
            }).ToList();

        return Result.Success(grouped.AsEnumerable());
    }

    public async Task<Result> AssignEmployeesToShift(AssignShiftRequest request)
{
    var shiftSchedule = await EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(context.ShiftSchedules
            .Include(s => s.ShiftTypes), s => s.Id == request.ShiftScheduleId);

    if (shiftSchedule is null)
        return Error.NotFound("Shift.NotFound", "Shift schedule not found.");
    
    var shiftType = await EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(context.ShiftTypes, st => st.Id == request.ShiftTypeId && st.LastDeletedById == null);

    if (shiftType == null)
    {
        return Error.NotFound("ShiftType.NotFound", "Shift type not found.");
    }
    
    var employeeIds = request.EmployeeIds.Distinct().ToList();

    // 🔹 Get employees on approved leave for this day of the week
    var employeesOnLeave = await EntityFrameworkQueryableExtensions.ToListAsync(context.LeaveRequests
            .Where(l =>
                employeeIds.Contains(l.EmployeeId) &&
                l.LeaveStatus == LeaveStatus.Approved &&
                l.StartDate <= request.ScheduleDate &&
                l.EndDate >= request.ScheduleDate)
            .Select(l => l.EmployeeId)
            .Distinct());

    // 🔹 Check for restricted holidays on this day
    var isRestrictedHoliday = await EntityFrameworkQueryableExtensions.AnyAsync(context.Holidays, h => h.Date == request.ScheduleDate);

    // 🔹 Load potential conflicting shift assignments
    var conflictingAssignments = await EntityFrameworkQueryableExtensions.ToListAsync(context.ShiftAssignments
            .Include(sa => sa.ShiftSchedules)
            .ThenInclude(ss => ss.ShiftTypes)
            .Where(sa =>
                employeeIds.Contains(sa.EmployeeId) &&
                sa.ScheduleDate == request.ScheduleDate));
    
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
            ShiftTypeId = request.ShiftTypeId,
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
        var shiftSchedule = await EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(context.ShiftSchedules, s => s.Id == id && s.LastDeletedById == null);
        if (shiftSchedule is null)
        {
            return Error.NotFound("ShiftSchedule.NotFound", "Shift schedule is not found");
        }

        var shiftTypes = await EntityFrameworkQueryableExtensions.ToListAsync(context.ShiftTypes
                .Where(shift => request.ShiftTypeIds.Contains(shift.Id)));
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
    var shiftSchedule = await EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(context.ShiftSchedules
            .Include(s => s.ShiftTypes), s => s.Id == id);

    if (shiftSchedule == null)
        return Error.NotFound("Shift.NotFound", "Shift schedule not found.");
    
    var shiftDay = request.ScheduleDate.Date;

    // 🔹 Remove employees from shift
    bool? isRemovable = request.RemoveEmployeeIds.Count != 0;

    if (isRemovable == true)
    {
        var assignmentsToRemove = await EntityFrameworkQueryableExtensions.ToListAsync(context.ShiftAssignments
                .Where(sa =>
                    request.RemoveEmployeeIds.Contains(sa.EmployeeId) &&
                    sa.ShiftScheduleId == id &&
                    sa.ScheduleDate.Date == shiftDay));

        if (assignmentsToRemove.Count != 0)
            context.ShiftAssignments.RemoveRange(assignmentsToRemove);
    }

    // 🔹 Add new employees to shift
    bool? isAdded = request.AddEmployeeIds.Count != 0;

    if (isAdded == true)
    {
        var employeeIds = request.AddEmployeeIds.Distinct().ToList();

        // Check leave, holidays, conflicts
        var employeesOnLeave = await EntityFrameworkQueryableExtensions.ToListAsync(context.LeaveRequests
                .Where(l =>
                    employeeIds.Contains(l.EmployeeId) &&
                    l.LeaveStatus == LeaveStatus.Approved &&
                    l.StartDate.Date <= shiftDay &&
                    l.EndDate.Date >= shiftDay)
                .Select(l => l.EmployeeId));

        var isHoliday = await EntityFrameworkQueryableExtensions.AnyAsync(context.Holidays, h => h.Date.Date == shiftDay);

        var conflictingAssignments = await EntityFrameworkQueryableExtensions.ToListAsync(context.ShiftAssignments
                .Include(sa => sa.ShiftSchedules)
                .ThenInclude(ss => ss.ShiftTypes)
                .Where(sa =>
                    employeeIds.Contains(sa.EmployeeId) &&
                    sa.ScheduleDate.Date == shiftDay));

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
        var shiftSchedule = await EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(context.ShiftSchedules, s => s.Id == id && s.LastDeletedById == null);
        
        if (shiftSchedule is null)
        {
            return Error.NotFound("ShiftSchedule.NotFound", "Shift schedule is not found");
        }
        shiftSchedule.LastDeletedById = userId;
        shiftSchedule.DeletedAt = DateTime.UtcNow;
        
        context.ShiftSchedules.Update(shiftSchedule);
        await context.SaveChangesAsync();
        return Result.Success();
    }
}