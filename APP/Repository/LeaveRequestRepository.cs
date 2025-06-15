using APP.Extensions;
using APP.IRepository;
using APP.Services.Background;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.LeaveRequests;
using DOMAIN.Entities.Notifications;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public class LeaveRequestRepository(ApplicationDbContext context, IMapper mapper, IApprovalRepository approvalRepository, IBackgroundWorkerService backgroundWorkerService) : ILeaveRequestRepository

{
    public async Task<Result<Guid>> CreateLeaveOrAbsenceRequest(CreateLeaveRequest request)
    {
        if (request.StartDate > request.EndDate)
            return Error.Validation("Request.InvalidDates", "Start date must be before end date.");

        var totalDays = (request.EndDate - request.StartDate).TotalDays + 1;
        
        var existingEmployee = await context.Employees
            .FirstOrDefaultAsync(e => e.Id == request.EmployeeId && e.LastDeletedById == null);
        
        if (existingEmployee is null)
            return Error.NotFound("Employee.NotFound", "Employee not found.");
        
        var leaveType = await context.LeaveTypes
            .FirstOrDefaultAsync(l => l.Id == request.LeaveTypeId && l.LastDeletedById == null);

        if (leaveType is null)
            return Error.NotFound("LeaveType.NotFound", "Leave type not found.");

        // Check if a request already exists for same period
        var existingRequest = await context.LeaveRequests
            .FirstOrDefaultAsync(l => l.EmployeeId == request.EmployeeId &&
                                      l.StartDate == request.StartDate &&
                                      l.EndDate == request.EndDate && l.LastDeletedById == null);
     
        if (existingRequest is not null)
            return Error.Validation("Request.Exists", "A request already exists for this period.");

        int paidDays = 0;
        int unpaidDays = 0;
        
        switch (request.RequestCategory)
        {
            case RequestCategory.AbsenceRequest or RequestCategory.LeaveRequest when string.IsNullOrWhiteSpace(request.ContactPerson) ||
                                                     string.IsNullOrWhiteSpace(request.ContactPersonNumber):
                return Error.Validation("AbsenceRequest.InvalidContactPerson", "Contact person and contact person number are required.");
            // Absence rules
            case RequestCategory.AbsenceRequest when totalDays > 2:
                return Error.Validation("AbsenceRequest.InvalidDuration", "Absence requests must be at most 2 days.");
            case RequestCategory.AbsenceRequest when leaveType.IsPaid:
            {
                if (leaveType.DeductFromBalance)
                {
                    var balanceDeducted = 0;
                    if (leaveType.DeductionLimit > 0)
                    {
                        paidDays = (int)Math.Min(totalDays, leaveType.DeductionLimit ?? 0);
                        var remaining = (int)totalDays - paidDays;

                        balanceDeducted = Math.Min(existingEmployee.AnnualLeaveDays, remaining);
                        unpaidDays = remaining - balanceDeducted;

                        existingEmployee.AnnualLeaveDays -= balanceDeducted;
                    }
                    else
                    {
                        balanceDeducted = Math.Min(existingEmployee.AnnualLeaveDays, (int)totalDays);
                        paidDays = balanceDeducted;
                        unpaidDays = (int)totalDays - balanceDeducted;

                        existingEmployee.AnnualLeaveDays -= balanceDeducted;
                    }
                }
                else
                {
                    paidDays = (int)totalDays;
                    unpaidDays = 0;
                }

                break;
            }
            case RequestCategory.AbsenceRequest:
                paidDays = 0;
                unpaidDays = (int)totalDays;
                break;
            case RequestCategory.ExitPassRequest:
            {
                if (request.StartDate != request.EndDate)
                {
                    return Error.Validation("LeaveRequest.InvalidDates", "Exit Pass request must be a single day.");
                }

                break;
            }
            case RequestCategory.LeaveRequest:
            default:
            {
                // Leave rules
                if (totalDays < 3)
                    return Error.Validation("LeaveRequest.InvalidDuration", "Leave request must be at least 3 days long.");

                if (leaveType.IsPaid)
                {
                    var deductionLimit = leaveType.DeductionLimit ?? 0;
                    var balance = existingEmployee.AnnualLeaveDays;

                    if (leaveType.DeductFromBalance)
                    {
                        if (balance >= totalDays - deductionLimit)
                        {
                            paidDays = (int)totalDays;
                            existingEmployee.AnnualLeaveDays -= (int)(totalDays - deductionLimit);
                        }
                        else
                        {
                            paidDays = balance + deductionLimit;
                            unpaidDays = (int)totalDays - paidDays;
                        }
                    }
                }
                else
                {
                    paidDays = 0;
                    unpaidDays = (int)totalDays;
                }

                break;
            }
        }

        var entity = mapper.Map<LeaveRequest>(request);

        entity.PaidDays = paidDays;
        entity.UnpaidDays = unpaidDays;

        await context.LeaveRequests.AddAsync(entity);
        await context.SaveChangesAsync();
        
        await approvalRepository.CreateInitialApprovalsAsync(nameof(LeaveRequest), entity.Id);
        
        backgroundWorkerService.EnqueueNotification("New leave request created", NotificationType.LeaveRequest);
        
        return entity.Id;
    }
    public async Task<Result<Paginateable<IEnumerable<LeaveRequestDto>>>> GetLeaveRequests(int page, int pageSize, string searchQuery)
    {
        var query = context.LeaveRequests
            .AsSplitQuery()
            .Include(l => l.LeaveType)
                .ThenInclude(l=> l.Designations)
            .Include(l => l.Employee)
                .ThenInclude(l => l.Designation)
                .ThenInclude(l => l.Departments)
            .Where(l => l.LastDeletedById == null)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            query = query.WhereSearch(searchQuery,
                q => q.Employee.FirstName,
                q => q.Employee.LastName,
                q => q.Employee.FirstName + " " + q.Employee.LastName,
                q => q.Employee.Department.Name,
                q => q.LeaveType.Name);
        }

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            if (Enum.TryParse<RequestCategory>(searchQuery, true, out var requestCategory))
            {
                query = query.Where(q => q.RequestCategory == requestCategory);
            }
        }

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            if (Enum.TryParse<LeaveStatus>(searchQuery, true, out var leaveStatus))
            {
               query = query.Where(q => q.LeaveStatus == leaveStatus); 
            }
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
           .AsSplitQuery()
           .Include(l => l.LeaveType)
           .Include(l => l.Employee)
           .FirstOrDefaultAsync(l => l.Id == leaveRequestId && l.LastDeletedById == null);
       
       return leaveRequest is null ? 
           Error.NotFound("LeaveRequest.NotFound", "Leave request not found") : 
           Result.Success(mapper.Map<LeaveRequestDto>(leaveRequest, opts => opts.Items[AppConstants.ModelType] = nameof(LeaveRequest)));
    }

    public async Task<Result> UpdateLeaveRequest(Guid leaveRequestId, CreateLeaveRequest leaveRequest)
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
        
        var leaveType = await context.LeaveTypes
            .FirstOrDefaultAsync(l => l.Id == leaveRequest.LeaveTypeId && l.LastDeletedById == null);

        if (leaveType is null)
        {
            return Error.NotFound("AbsenceType.NotFound", "Absence type not found");
        }

        
        mapper.Map(leaveRequest, existingLeaveRequest);

        context.LeaveRequests.Update(existingLeaveRequest);
        
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> ReapplyLeaveRequest(Guid leaveRequestId, ReapplyLeaveRequest reapplyLeaveRequest)
    {
        var existing = await context.LeaveRequests
            .Include(l => l.Approvals)
            .FirstOrDefaultAsync(l => l.Id == leaveRequestId
                                      && l.LeaveStatus == LeaveStatus.Rejected
                                      && l.LastDeletedById == null);

        if (existing == null)
            return Error.NotFound("Leave.NotFound", "Rejected leave request not found.");

        // Reset fields
        existing.StartDate = reapplyLeaveRequest.NewStartDate;
        existing.EndDate = reapplyLeaveRequest.NewEndDate;
        existing.Justification = reapplyLeaveRequest.Justification;
        existing.LeaveStatus = LeaveStatus.Reapplied;
        existing.Approved = false;
        existing.RecallDate = DateTime.UtcNow;

        // Reset approvals
        context.LeaveRequestApprovals.RemoveRange(existing.Approvals);
        existing.Approvals = [];

        await context.SaveChangesAsync();
        return Result.Success(existing.Id);
    }

    public async Task<Result> SubmitLeaveRecallRequest(CreateLeaveRecallRequest createLeaveRecallRequest)
    {
        var employee = await context.Employees.FirstOrDefaultAsync(e => e.Id == createLeaveRecallRequest.EmployeeId);

        if (employee == null)
        {
            return Error.NotFound("Employee.NotFound", "Employee not found");
        }
        
        var leaveRequest = await context.LeaveRequests
            .FirstOrDefaultAsync(l =>
                l.EmployeeId == createLeaveRecallRequest.EmployeeId &&
                l.LeaveStatus == LeaveStatus.Approved && l.DeletedAt == null);
        
        if (leaveRequest == null)
            return Error.Validation("LeaveRecall.Invalid", "No approved leave found for the specified date.");
        
        var today = DateTime.UtcNow.Date;

        if (today < leaveRequest.StartDate.Date || today > leaveRequest.EndDate.Date)
        {
            return Error.Validation("LeaveRecall.Invalid", "You can only recall a leave that is currently ongoing.");
        }
        
        if (createLeaveRecallRequest.RecallDate.Date < leaveRequest.StartDate.Date || 
            createLeaveRecallRequest.RecallDate.Date > leaveRequest.EndDate.Date)
        {
            return Error.Validation("LeaveRecall.Invalid", "Recall date must fall within the leave period.");
        }
        
        var daysRemaining = (leaveRequest.EndDate.Date - createLeaveRecallRequest.RecallDate.Date).Days;

        if (daysRemaining > 0)
        {
            employee.AnnualLeaveDays += daysRemaining;
        }

        leaveRequest.LeaveStatus = LeaveStatus.Recalled;
        leaveRequest.RecallDate = createLeaveRecallRequest.RecallDate;
        leaveRequest.RecallReason = createLeaveRecallRequest.RecallReason;

        context.LeaveRequests.Update(leaveRequest);
        await context.SaveChangesAsync();
        
        return Result.Success();
    }
    

    public async Task<Result> DeleteLeaveRequest(Guid leaveRequestId, Guid userId)
    {
        var leaveRequest = await context.LeaveRequests
            .Include(l => l.Employee)
            .FirstOrDefaultAsync(l => l.Id == leaveRequestId && l.LastDeletedById == null);
        
        if (leaveRequest is null)
        {
            return Error.NotFound("LeaveRequest.NotFound", "Leave request not found");
        }

        if (leaveRequest.LeaveStatus == LeaveStatus.Approved && leaveRequest.StartDate > DateTime.UtcNow.Date)
        {
            var daysRemaining = (leaveRequest.EndDate.Date - leaveRequest.StartDate.Date).Days;

            if (daysRemaining > 0)
            {
                leaveRequest.Employee.AnnualLeaveDays += daysRemaining;
            }
        }
        
        leaveRequest.DeletedAt = DateTime.UtcNow;
        leaveRequest.LastDeletedById = userId;
        context.LeaveRequests.Update(leaveRequest);
        
        await context.SaveChangesAsync();
        return Result.Success();
    }
}
