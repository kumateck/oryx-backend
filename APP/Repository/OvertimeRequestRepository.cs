using System.Globalization;
using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.OvertimeRequests;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public class OvertimeRequestRepository(ApplicationDbContext context, IConfigurationRepository configurationRepository, IMapper mapper) : IOvertimeRequestRepository
{
    public async Task<Result<Guid>> CreateOvertimeRequest(CreateOvertimeRequest request)
    {
        // Validate time format
        if (!IsValidStartTime(request.StartTime) || !IsValidStartTime(request.EndTime))
        {
            return Error.Validation("OvertimeRequest.InvalidTimeFormat", 
                "Start time and end time must be in 12-hour format.");
        }

        // Fetch all selected employees
        var selectedEmployees = await context.Employees
            .Where(e => request.EmployeeIds.Contains(e.Id))
            .Select(e => new { e.Id, e.FirstName, e.LastName, e.DepartmentId })
            .ToListAsync();

        // Check for duplicate overtime requests in one query
        var duplicateEmployeeIds = await context.OvertimeRequests
            .Where(ot => request.EmployeeIds.Any(id => ot.EmployeeIds.Contains(id)) 
                         && ot.OvertimeDate == request.OvertimeDate)
            .SelectMany(ot => ot.EmployeeIds)
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
        overtimeRequestEntity.EmployeeIds = request.EmployeeIds;

        await context.OvertimeRequests.AddAsync(overtimeRequestEntity);
        await context.SaveChangesAsync();

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
            .Include(o => o.Department)
            .Where(o => o.LastDeletedById == null)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            query = query.WhereSearch(searchQuery, ot => ot.StartTime);
        }
        
        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            query = query.WhereSearch(searchQuery, ot => ot.Status.ToString());;
        }

        
        return await PaginationHelper.GetPaginatedResultAsync(query, page, pageSize, mapper.Map<OvertimeRequestDto>);
    }

    public async Task<Result<OvertimeRequestDto>> GetOvertimeRequest(Guid id)
    {
        var overtimeRequest = await context.OvertimeRequests
            .Include(o => o.Department)
            .FirstOrDefaultAsync(ot => ot.Id == id && ot.LastDeletedById == null);
        
        return overtimeRequest is null ? 
            Error.NotFound("OvertimeRequest.NotFound", "Overtime request is not found") : 
            Result.Success(mapper.Map<OvertimeRequestDto>(overtimeRequest));

    }

    public async Task<Result> UpdateOvertimeRequest(Guid id, CreateOvertimeRequest request)
    {
        var overtimeRequest = await context.OvertimeRequests
            .FirstOrDefaultAsync(ot => ot.Id == id && ot.LastDeletedById == null);
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
            .FirstOrDefaultAsync(ot => ot.Id == id && ot.LastDeletedById == null);
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