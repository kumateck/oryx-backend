using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.LeaveEntitlements;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public class LeaveEntitlementRepository(ApplicationDbContext context, IMapper mapper) : ILeaveEntitlementRepository
{
    public async Task<Result<Guid>> CreateLeaveEntitlement(LeaveEntitlementDto leaveEntitlementRequest)
    {
        if (leaveEntitlementRequest.Year < DateTime.UtcNow.Year)
        {
            return Error.Validation("Invalid Year", "Year must be greater than or equal to the current year");
        }

        var employee = await context.Employees
            .FirstOrDefaultAsync(e => e.Id == leaveEntitlementRequest.EmployeeId
            && e.LastDeletedById == null);
        
        if (employee is null)
        {
            return Error.NotFound("Employee.NotFound", "Employee is not found");
        }

        var existingEntitlement =
            await context.LeaveEntitlements.FirstOrDefaultAsync(l =>
                l.EmployeeId == leaveEntitlementRequest.EmployeeId 
                && l.Year == leaveEntitlementRequest.Year
                && l.LastDeletedById == null);;
        
        if (existingEntitlement is not null)
        {
            return Error.Validation("LeaveEntitlement.AlreadyExists", "Leave entitlement already exists");
        }
        
        var leaveEntitlement = mapper.Map<LeaveEntitlement>(leaveEntitlementRequest);

        await context.LeaveEntitlements.AddAsync(leaveEntitlement);
        await context.SaveChangesAsync();
        
        return leaveEntitlement.Id;
    }

    public async Task<Result<LeaveEntitlementDto>> GetLeaveEntitlement(Guid leaveEntitlementId)
    {
        var leaveEntitlement = await context.LeaveEntitlements
            .FirstOrDefaultAsync(l => l.Id == leaveEntitlementId
            && l.LastDeletedById == null);
        
        return leaveEntitlement is null ?
            Error.NotFound("LeaveEntitlement.NotFound", "Leave entitlement is not found") :
            Result.Success(mapper.Map<LeaveEntitlementDto>(leaveEntitlement));
    }

    public async Task<Result<Paginateable<IEnumerable<LeaveEntitlementDto>>>> GetLeaveEntitlements(int page, int pageSize, string searchQuery)
    {
        var query = context.LeaveEntitlements
            .Where(l => l.LastDeletedById == null)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            query = query.WhereSearch(searchQuery);
        }
        
        return await PaginationHelper.GetPaginatedResultAsync(
            query,
            page,
            pageSize,
            mapper.Map<LeaveEntitlementDto>
        );
    }

    public async Task<Result> UpdateLeaveEntitlement(Guid id, LeaveEntitlementDto leaveEntitlementDto)
    {
        var query = context.LeaveEntitlements.AsQueryable();
        var leaveEntitlement = await query
            .FirstOrDefaultAsync(l => l.Id == id && l.LastDeletedById == null);
        if (leaveEntitlement is null)
        {
            return Error.NotFound("LeaveEntitlement.NotFound", "Leave entitlement is not found");
        }
        
        mapper.Map(leaveEntitlementDto, leaveEntitlement);

        context.LeaveEntitlements.Update(leaveEntitlement);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> DeleteLeaveEntitlement(Guid id, Guid userId)
    {
        var leaveEntitlement = await context.LeaveEntitlements
            .FirstOrDefaultAsync(l => l.Id == id && l.LastDeletedById == null);
        if (leaveEntitlement is null)
        {
            return Error.NotFound("LeaveEntitlement.NotFound", "Leave entitlement is not found");
        }
        
        leaveEntitlement.DeletedAt = DateTime.UtcNow;
        leaveEntitlement.LastDeletedById = userId;
        context.LeaveEntitlements.Update(leaveEntitlement);
        await context.SaveChangesAsync();
        return Result.Success();
    }
}