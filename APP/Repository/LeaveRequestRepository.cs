using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.LeaveRequests;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public class LeaveRequestRepository(ApplicationDbContext context, IMapper mapper) : ILeaveRequestRepository

{
    public async Task<Result<Guid>> CreateLeaveRequest(CreateLeaveRequest leaveRequest, Guid userId)
    {
        if ((leaveRequest.EndDate - leaveRequest.StartDate).TotalDays + 1 < 3)
        {
            return Error.Validation("LeaveRequest.InvalidDates", "Leave request must be at least 3 days long.");
        }
        var existingLeaveRequest = await context.LeaveRequests
            .FirstOrDefaultAsync(l => l.StartDate == leaveRequest.StartDate
            && l.EndDate == leaveRequest.EndDate && l.EmployeeId == leaveRequest.EmployeeId);
        
        if (existingLeaveRequest != null)
        {
            return Error.Validation("LeaveRequest.Exists", "Leave request already exists.");
        }

        if (leaveRequest.StartDate > leaveRequest.EndDate)
        {
            return Error.Validation("LeaveRequest.InvalidDates", "Start date must be before end date.");
        }
        
        var existingEmployee = await context.Employees
            .FirstOrDefaultAsync(e => e.Id == leaveRequest.EmployeeId && e.LastDeletedById == null);;
        
        var leaveType = await context.LeaveTypes
            .FirstOrDefaultAsync(l => l.Id == leaveRequest.LeaveTypeId && l.LastDeletedById == null);
        
        if (existingEmployee is null)
        {
            return Error.NotFound("Employee.NotFound", "Employee not found");
        }
        
        if (leaveType is null)
        {
            return Error.NotFound("LeaveType.NotFound", "Leave type not found");
        }
        
        var totalDays = (int) (leaveRequest.EndDate - leaveRequest.StartDate).TotalDays + 1;
        var unpaidDays = 0;

        if (leaveType.IsPaid)
        {
            var paidDays = 0;
            if (leaveType.DeductFromBalance)
            {
                var deductionLimit = leaveType.DeductionLimit ?? 0;
                var balance = existingEmployee.AnnualLeaveDays;

                if (balance >= totalDays - deductionLimit)
                {
                    paidDays = totalDays;
                    existingEmployee.AnnualLeaveDays -= (totalDays - deductionLimit);
                }
                else
                {
                    paidDays = balance + deductionLimit;
                    unpaidDays = totalDays - paidDays;
                    existingEmployee.AnnualLeaveDays = 0;
                }
            }
            else
            {
                paidDays = totalDays;
            }
        }
        else
        {
            unpaidDays = totalDays;
        }
        
        var leaveRequestEntity = mapper.Map<LeaveRequest>(leaveRequest);
        leaveRequestEntity.CreatedById = userId;
        leaveRequestEntity.CreatedAt = DateTime.UtcNow;
        
        await context.LeaveRequests.AddAsync(leaveRequestEntity);
        await context.SaveChangesAsync();
        return leaveRequestEntity.Id;
    }

    public async Task<Result<Paginateable<IEnumerable<LeaveRequestDto>>>> GetLeaveRequests(int page, int pageSize, string searchQuery)
    {
        var query = context.LeaveRequests
            .Include(l => l.LeaveType)
            .Include(l => l.Employee)
            .Where(l => l.LastDeletedById == null)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            query = query.WhereSearch(searchQuery, q => q.Employee.FullName);
        }

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            query = query.WhereSearch(searchQuery, q => q.LeaveType.Name);
        }
        
        return await PaginationHelper.GetPaginatedResultAsync(
            query,
            page,
            pageSize,
            mapper.Map<LeaveRequestDto>
        );
    }

    public async Task<Result<LeaveRequestDto>> GetLeaveRequest(Guid leaveRequestId)
    {
       var leaveRequest = await context.LeaveRequests
           .FirstOrDefaultAsync(l => l.Id == leaveRequestId && l.LastDeletedById == null);
       
       return leaveRequest is null ? 
           Error.NotFound("LeaveRequest.NotFound", "Leave request not found") : 
           Result.Success(mapper.Map<LeaveRequestDto>(leaveRequest));
    }

    public async Task<Result> UpdateLeaveRequest(Guid leaveRequestId, CreateLeaveRequest leaveRequest, Guid userId)
    {
        var existingLeaveRequest = await context.LeaveRequests
            .FirstOrDefaultAsync(l => l.Id == leaveRequestId && l.LastDeletedById == null);
        
        if (existingLeaveRequest is null)
        {
            return Error.NotFound("LeaveRequest.NotFound", "Leave request not found");
        }

        if (existingLeaveRequest.StartDate > existingLeaveRequest.EndDate)
        {
            return Error.Validation("LeaveRequest.InvalidDates", "Start date must be before end date.");
        }
        
        var existingEmployee = await context.Employees
            .FirstOrDefaultAsync(l => l.Id == leaveRequest.EmployeeId && l.LastDeletedById == null);;
        
        if (existingEmployee is null)
        {
            return Error.NotFound("Employee.NotFound", "Employee not found");
        }
        
        mapper.Map(leaveRequest, existingLeaveRequest);
        existingLeaveRequest.LastUpdatedById = userId;
        existingLeaveRequest.UpdatedAt = DateTime.UtcNow;
        context.LeaveRequests.Update(existingLeaveRequest);
        
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> DeleteLeaveRequest(Guid leaveRequestId, Guid userId)
    {
        var leaveRequest = await context.LeaveRequests
            .FirstOrDefaultAsync(l => l.Id == leaveRequestId && l.LastDeletedById == null);;
        
        if (leaveRequest is null)
        {
            return Error.NotFound("LeaveRequest.NotFound", "Leave request not found");
        }
        
        leaveRequest.DeletedAt = DateTime.UtcNow;
        leaveRequest.LastDeletedById = userId;
        context.LeaveRequests.Update(leaveRequest);
        
        await context.SaveChangesAsync();
        return Result.Success();
    }
}