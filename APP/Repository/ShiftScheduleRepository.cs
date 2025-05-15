using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.LeaveRequests;
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
    
    // public async Task<Result> AssignEmployeesToShiftAsync(Guid shiftId, List<Guid> employeeIds)
    // {
    //     var shift = await context.ShiftSchedules
    //         .FirstOrDefaultAsync(s => s.Id == shiftId);
    //
    //     if (shift == null)
    //         return Error.NotFound("Shift not found.");
    //
    //     var shiftStart = shift.StartTime;
    //     var shiftEnd = shift.EndTime;
    //
    //     var successfulAssignments = new List<Guid>();
    //     var violations = new List<string>();
    //
    //     foreach (var employeeId in employeeIds)
    //     {
    //         // Leave Check
    //         var onLeave = await context.LeaveRequests.AnyAsync(l =>
    //             l.EmployeeId == employeeId &&
    //             l.LeaveStatus == LeaveStatus.Approved &&
    //             l.StartDate <= shiftEnd &&
    //             l.EndDate >= shiftStart);
    //
    //         if (onLeave)
    //         {
    //             violations.Add($"Employee {employeeId} is on approved leave during the shift.");
    //             continue;
    //         }
    //
    //         // Conflict Check
    //         var hasConflict = await context.ShiftAssignments.AnyAsync(sa =>
    //             sa.EmployeeId == employeeId &&
    //             context.ShiftSchedules.Any(s =>
    //                 s.Id == sa.ShiftScheduleId &&
    //                 s.Id != shiftId &&
    //                 s.StartTime < shiftEnd &&
    //                 s.EndTime > shiftStart
    //             )
    //         );
    //
    //         if (hasConflict)
    //         {
    //             violations.Add($"Employee {employeeId} has a conflicting shift.");
    //             continue;
    //         }
    //
    //         // Assign
    //         var assignment = new ShiftAssignment
    //         {
    //             Id = Guid.NewGuid(),
    //             EmployeeId = employeeId,
    //             ShiftScheduleId = shiftId
    //         };
    //
    //         context.ShiftAssignments.Add(assignment);
    //         successfulAssignments.Add(employeeId);
    //     }
    //
    //     await context.SaveChangesAsync();
    //
    //     return Result.Success(new
    //     {
    //         Assigned = successfulAssignments,
    //         Skipped = violations
    //     });
    // }

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