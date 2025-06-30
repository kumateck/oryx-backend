using System.Globalization;
using APP.Extensions;
using APP.IRepository;
using APP.Services.Background;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.Notifications;
using DOMAIN.Entities.OvertimeRequests;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public class OvertimeRequestRepository(ApplicationDbContext context, IMapper mapper, IBackgroundWorkerService backgroundWorkerService) : IOvertimeRequestRepository
{
    public async Task<Result<Guid>> CreateOvertimeRequest(CreateOvertimeRequest request)
    {
        // Validate time format
        if (!IsValidStartTime(request.StartTime) || !IsValidStartTime(request.EndTime))
        {
            return Error.Validation("OvertimeRequest.InvalidTimeFormat", 
                "Start time and end time must be in 12-hour format.");
        }
        
        var department = await context.Departments.FirstOrDefaultAsync(d => d.Id == request.DepartmentId);
        if (department == null)
        {
            return Error.Validation("Department.Invalid", "Invalid department.");
        }

        // Fetch all selected employees
        var selectedEmployees = await context.Employees
            .Where(e => request.EmployeeIds.Contains(e.Id))
            .ToListAsync();

        // Check for duplicate overtime requests in one query
        var duplicateEmployeeIds = await context.OvertimeRequests
            .Where(ot => ot.OvertimeDate == request.OvertimeDate)
            .SelectMany(ot => ot.Employees)
            .Where(emp => request.EmployeeIds.Contains(emp.Id))
            .Select(emp => emp.Id) 
            .Distinct()
            .ToListAsync();

        if (duplicateEmployeeIds.Count != 0)
        {
            var duplicateNames = selectedEmployees
                .Where(e => duplicateEmployeeIds.Contains(e.Id))
                .Select(e => $"{e.FirstName} {e.LastName}")
                .ToList();

            return Error.Validation("OvertimeRequest.DuplicateEntries", 
                $"Overtime request already exists for the following employees on {request.OvertimeDate:yyyy-MM-dd}: {string.Join(", ", duplicateNames)}");
        }
        
        var overtimeRequestEntity = mapper.Map<OvertimeRequest>(request);
        overtimeRequestEntity.Employees = selectedEmployees;

        await context.OvertimeRequests.AddAsync(overtimeRequestEntity);
        await context.SaveChangesAsync();
        
        backgroundWorkerService.EnqueueNotification("New overtime request created", NotificationType.OvertimeRequest);

        return overtimeRequestEntity.Id;
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

    public async Task<Result<Paginateable<IEnumerable<OvertimeRequestDto>>>> GetOvertimeRequests(int page, int pageSize, string searchQuery)
    {
        var query =  context.OvertimeRequests
            .AsSplitQuery()
            .Include(o => o.Employees)
            .Include(o => o.Department)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            query = query.WhereSearch(searchQuery, ot => ot.StartTime);
        }
        
        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            if (Enum.TryParse<OvertimeStatus>(searchQuery, true, out var status))
            {
                  query = query.Where(ot => ot.Status == status);
            }
        }
        
        return await PaginationHelper.GetPaginatedResultAsync(query, page, pageSize, mapper.Map<OvertimeRequestDto>);
    }

    public async Task<Result<OvertimeRequestDto>> GetOvertimeRequest(Guid id)
    {
        var overtimeRequest = await context.OvertimeRequests
            .AsSplitQuery()
            .Include(o => o.Department)
            .Include(o => o.Employees)
            .Include(o => o.CreatedBy)
            .FirstOrDefaultAsync(ot => ot.Id == id);
        
        return overtimeRequest is null ? 
            Error.NotFound("OvertimeRequest.NotFound", "Overtime request is not found") : 
            Result.Success(mapper.Map<OvertimeRequestDto>(overtimeRequest));

    }

    public async Task<Result> UpdateOvertimeRequest(Guid id, CreateOvertimeRequest request)
    {
        var overtimeRequest = await context.OvertimeRequests
            .FirstOrDefaultAsync(ot => ot.Id == id);
        if (overtimeRequest is null)
        {
            return Error.NotFound("OvertimeRequest.NotFound", "Overtime request is not found");
        }
        
        mapper.Map(request, overtimeRequest);
        
        context.OvertimeRequests.Update(overtimeRequest);
        await context.SaveChangesAsync();
        return Result.Success();
        
    }

    public async Task<Result> DeleteOvertimeRequest(Guid id, Guid userId)
    {
        var overtimeRequest = await context.OvertimeRequests
            .FirstOrDefaultAsync(ot => ot.Id == id);
        if (overtimeRequest is null)
        {
            return Error.NotFound("OvertimeRequest.NotFound", "Overtime request is not found");
        }

        if (overtimeRequest.Status is OvertimeStatus.Approved or OvertimeStatus.Rejected or OvertimeStatus.Expired)
        {
            return Error.Validation("OvertimeRequest.InvalidStatus",
                "Cannot delete an overtime request that has already been approved or rejected.");
        }
        
        overtimeRequest.DeletedAt = DateTime.UtcNow;
        overtimeRequest.LastDeletedById = userId;
        
        context.OvertimeRequests.Update(overtimeRequest);
        await context.SaveChangesAsync();
        return Result.Success();
    }
}