using APP.Extensions;
using APP.IRepository;
using APP.Services.JobRequests;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.JobRequests;
using DOMAIN.Entities.Users;
using INFRASTRUCTURE.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public class JobRequestRepository(ApplicationDbContext context,
    IMapper mapper, 
    UserManager<User> userManager,
    IApprovalRepository approvalRepository,
    IJobRequestAssignmentNotifier assignmentNotifier)
    : IJobRequestRepository
{
    public async Task<Result<Guid>> CreateJobRequest(CreateJobRequest request, Guid departmentId, Guid issuedById)
    {
        var department = await context.Departments.AnyAsync(d => d.Id == departmentId);
        if (!department) return Error.Validation("Department.Invalid", "Invalid department");

        var issuer = await userManager.FindByIdAsync(issuedById.ToString());
        if (issuer is null) return Error.Validation("User.Invalid", "User Invalid");

        if (request.EquipmentId.HasValue)
        {
            var equipment = await context.Equipments.AnyAsync(e => e.Id == request.EquipmentId.Value);
            if (!equipment) return Error.Validation("Equipment.Invalid", "Invalid equipment");
        }

        var jobRequest = mapper.Map<JobRequest>(request);
        jobRequest.DepartmentId = departmentId;
        jobRequest.IssuedById = issuedById;
        
        await context.JobRequests.AddAsync(jobRequest);
        await context.SaveChangesAsync();
        
        await approvalRepository.CreateInitialApprovalsAsync(nameof(JobRequest), jobRequest.Id);

        return jobRequest.Id;
    }

    public async Task<Result<Paginateable<IEnumerable<JobRequestDto>>>> GetJobRequests(int page, 
        int pageSize, 
        string searchQuery = null, 
        JobRequestStatus? status = null, 
        JobHandlingType? handlingType = null, 
        Guid? departmentId = null)
    {
        var query = context.JobRequests
            .AsSplitQuery()
            .IgnoreQueryFilters()
            .Include(j => j.Department)
            .Include(j => j.Equipment)
            .Include(j => j.IssuedBy)
            .Include(j => j.AssignedToEmployee)
            .Include(j => j.AssignedBy)
            .Include(j => j.Service)
            .Include(j => j.Site)
            .Where(j => !j.DeletedAt.HasValue)
            .AsQueryable();

        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.WhereSearch(searchQuery, q => q.DescriptionOfWork,
                q => q.Site.Name);
        }

        if (status.HasValue)
        {
            query = query.Where(j => j.Status == status.Value);
        }

        if (handlingType.HasValue)
        {
            query = query.Where(j => j.HandlingType == handlingType.Value);
        }

        if (departmentId.HasValue)
        {
            query = query.Where(j => j.DepartmentId == departmentId.Value);
        }

        return await PaginationHelper.GetPaginatedResultAsync(query, page, pageSize,
            mapper.Map<JobRequestDto>);
    }

    public async Task<Result<IEnumerable<JobRequestDto>>> GetJobRequestsInJobOrders()
    {
        var jobRequests = await context.JobRequests
            .AsSplitQuery()
            .IgnoreQueryFilters()
            .IgnoreAutoIncludes()
            .Where(j => context.JobOrders.Any(jo => jo.JobRequestId == j.Id))
            .Include(j => j.Department)
            .Include(j => j.Equipment)
            .Include(j => j.IssuedBy)
            .Include(j => j.AssignedToEmployee)
            .Include(j => j.AssignedBy)
            .Include(j => j.Service)
            .Include(j => j.Site)
            .ToListAsync();
        
        return mapper.Map<List<JobRequestDto>>(jobRequests);
    }

    public async Task<Result<JobRequestDto>> GetJobRequest(Guid id)
    {
        try
        {
            var jobRequest = await context.JobRequests
                .AsSplitQuery()
                .Include(j => j.Site)
                .Include(j => j.Department)
                .Include(j => j.Equipment)
                .Include(j => j.IssuedBy)
                .Include(j => j.AssignedToEmployee)
                .Include(j => j.AssignedBy)
                .Include(j => j.Service)
                .Include(j => j.Executions)
                    .ThenInclude(e => e.AssignedToEmployee)
                .Include(j => j.Executions)
                    .ThenInclude(e => e.AssignedBy)
                .Include(j => j.Executions)
                    .ThenInclude(e => e.VerifiedBy)
                .Include(j => j.Executions)
                    .ThenInclude(e => e.ApprovedBy)
                .Include(j => j.Executions)
                    .ThenInclude(e => e.Activities)
                        .ThenInclude(a => a.PerformedBy)
                .Include(j => j.Executions)
                    .ThenInclude(e => e.ConsumedItems)
                        .ThenInclude(c => c.Item)
                .Include(j => j.Executions)
                    .ThenInclude(e => e.ConsumedItems)
                        .ThenInclude(c => c.UnitOfMeasure)
                .Include(j => j.JobOrders)
                    .ThenInclude(jo => jo.Service)
                .Include(j => j.JobOrders)
                    .ThenInclude(jo => jo.IssuedBy)
                .Include(j => j.JobOrders)
                    .ThenInclude(jo => jo.ServiceProviders)
                        .ThenInclude(sp => sp.ServiceProvider)
                .Include(j => j.JobOrders)
                    .ThenInclude(jo => jo.Quotations)
                        .ThenInclude(q => q.ServiceProvider)
                .Include(j => j.JobOrders)
                    .ThenInclude(jo => jo.Quotations)
                        .ThenInclude(q => q.Items)
                .Include(j => j.JobOrders)
                    .ThenInclude(jo => jo.SelectedQuotation)
                .Include(j => j.JobOrders)
                    .ThenInclude(jo => jo.ServiceProformaInvoice)
                .Include(j => j.JobOrders)
                    .ThenInclude(jo => jo.ServiceMemo)
                .Include(j => j.JobOrders)
                    .ThenInclude(jo => jo.Execution)
                        .ThenInclude(e => e.ServiceProvider)
                .Include(j => j.JobOrders)
                    .ThenInclude(jo => jo.Execution)
                        .ThenInclude(e => e.VerifiedBy)
                .Include(j => j.JobOrders)
                    .ThenInclude(jo => jo.Execution)
                        .ThenInclude(e => e.ApprovedBy)
                .Include(j => j.JobOrders)
                    .ThenInclude(jo => jo.Execution)
                        .ThenInclude(e => e.Activities)
                            .ThenInclude(a => a.PerformedBy)
                .Include(j => j.JobOrders)
                    .ThenInclude(jo => jo.Execution)
                        .ThenInclude(e => e.ConsumedItems)
                            .ThenInclude(c => c.Item)
                .Include(j => j.JobOrders)
                    .ThenInclude(jo => jo.Execution)
                        .ThenInclude(e => e.ConsumedItems)
                        .ThenInclude(c => c.UnitOfMeasure)
                .AsNoTracking()
                .FirstOrDefaultAsync(j => j.Id == id);

            if (jobRequest is null)
                return Error.NotFound("JobRequest.NotFound", $"Job request with ID '{id}' not found");

            var dto = mapper.Map<JobRequestDto>(jobRequest);
            return dto;
        }
        catch (DbUpdateException dbEx)
        {
            return Error.Failure("JobRequest.DatabaseError", 
                $"Database error while retrieving job request: {dbEx.Message}");
        }
        catch (Exception ex)
        {
            return Error.Failure("JobRequest.RetrievalError", 
                $"An error occurred while retrieving job request: {ex.Message}. Stack trace: {ex.StackTrace}");
        }
    }

    public async Task<Result> UpdateJobRequest(Guid id, UpdateJobRequestRequest request)
    {
        var jobRequest = await context.JobRequests.FirstOrDefaultAsync(j => j.Id == id);
        if (jobRequest is null)
            return Error.NotFound("JobRequest.NotFound", "Job request not found");
        if (jobRequest.Status is not (JobRequestStatus.Pending or JobRequestStatus.Acknowledged))
            return Error.Validation("JobRequest.InProgress", "Work on this job request has begun, so it can no longer be changed.");

        if (request.EquipmentId.HasValue)
        {
            var equipment = await context.Equipments.AnyAsync(e => e.Id == request.EquipmentId.Value);
            if (!equipment) return Error.Validation("Equipment.Invalid", "Invalid equipment");
        }

        // Validate ServiceId if provided (takes precedence over ServiceIds)
        if (request.ServiceId.HasValue)
        {
            var serviceExists = await context.Services.AnyAsync(s => s.Id == request.ServiceId.Value);
            if (!serviceExists) return Error.Validation("Service.Invalid", $"Invalid service: {request.ServiceId.Value}");
        }
        
        mapper.Map(request, jobRequest);
        
        context.JobRequests.Update(jobRequest);
        await context.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result> DeleteJobRequest(Guid id, Guid userId)
    {
        var jobRequest = await context.JobRequests.FirstOrDefaultAsync(j => j.Id == id);
        if (jobRequest is null)
            return Error.NotFound("JobRequest.NotFound", "Job request not found");
        if (jobRequest.Status is not (JobRequestStatus.Pending or JobRequestStatus.Acknowledged or JobRequestStatus.Cancelled)
            || await context.JobExecutions.AnyAsync(e => e.JobRequestId == id) || await context.JobOrders.AnyAsync(o => o.JobRequestId == id))
            return Error.Validation("JobRequest.InProgress", "Work on this job request has begun, so it cannot be deleted.");

        jobRequest.DeletedAt = DateTime.UtcNow;
        jobRequest.LastDeletedById = userId;

        context.JobRequests.Update(jobRequest);
        await context.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result<Guid>> AssignInternalJob(AssignInternalJobRequest request)
    {
        var jobRequest = await context.JobRequests.FirstOrDefaultAsync(j => j.Id == request.JobRequestId);
        if (jobRequest is null)
            return Error.NotFound("JobRequest.NotFound", "Job request not found");

        var approvalGate = jobRequest.EnsureApprovedForProgression("Job request");
        if (approvalGate.IsFailure)
            return approvalGate.Error;
        if (jobRequest.Status is not (JobRequestStatus.Pending or JobRequestStatus.Acknowledged) || jobRequest.HandlingType == JobHandlingType.External)
            return Error.Validation("JobRequest.AlreadyHandled", "This job request has already been assigned or sent out. Reassign its execution instead.");

        var employee = await context.Employees
            .FirstOrDefaultAsync(e => e.Id == request.AssignedToEmployeeId);
        if (employee is null) return Error.Validation("Employee.Invalid", "Invalid employee");

        if (string.IsNullOrWhiteSpace(employee.Email))
            return Error.Validation(
                "Employee.UserAccountUnavailable",
                "The selected employee has no active user account and cannot receive this assignment");

        var assignee = await userManager.FindByEmailAsync(employee.Email);
        if (assignee is null || assignee.IsDisabled)
            return Error.Validation(
                "Employee.UserAccountUnavailable",
                "The selected employee has no active user account and cannot receive this assignment");

        var assignedBy = await userManager.FindByIdAsync(request.AssignedById.ToString());
        if (assignedBy is null) return Error.Validation("User.Invalid", "User Invalid");

        // Update job request
        jobRequest.HandlingType = JobHandlingType.Internal;
        jobRequest.Status = JobRequestStatus.Assigned;
        jobRequest.AssignedToEmployeeId = request.AssignedToEmployeeId;
        jobRequest.AssignedAt = DateTime.UtcNow;
        jobRequest.AssignedById = request.AssignedById;

        // Create job execution
        var jobExecution = new JobExecution
        {
            JobRequestId = request.JobRequestId,
            AssignedToEmployeeId = request.AssignedToEmployeeId,
            AssignedAt = DateTime.UtcNow,
            AssignedById = request.AssignedById,
            Status = JobExecutionStatus.Assigned,
            Notes = request.Notes
        };

        await context.JobExecutions.AddAsync(jobExecution);
        context.JobRequests.Update(jobRequest);
        await context.SaveChangesAsync();

        await assignmentNotifier.NotifyAssigned(assignee, jobRequest);

        return jobExecution.Id;
    }


    public async Task<Result> UpdateJobRequestStatus(Guid id, JobRequestStatus status)
    {
        var jobRequest = await context.JobRequests.FirstOrDefaultAsync(j => j.Id == id);
        if (jobRequest is null)
            return Error.NotFound("JobRequest.NotFound", $"Job request with ID '{id}' not found");

        var approvalGate = jobRequest.EnsureApprovedForProgression("Job request");
        if (approvalGate.IsFailure)
            return approvalGate;

        // Validate status value is within enum range
        if (!Enum.IsDefined(typeof(JobRequestStatus), status))
        {
            return Error.Validation("JobRequest.InvalidStatus", 
                $"Invalid status value '{status}'. Valid values are: 0 (Pending), 1 (Acknowledged), 2 (Assigned), 3 (JobStarted), 4 (Completed), 5 (SentToExternal), 6 (QuotationReceived), 7 (ContractorSelected), 8 (Approved), 9 (Cancelled)");
        }

        jobRequest.Status = status;
        context.JobRequests.Update(jobRequest);
        await context.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result<Paginateable<IEnumerable<JobRequestDto>>>> GetCompletedJobRequestsForInternalEmployees(int page, int pageSize,
        string searchQuery = null, Guid? employeeId = null)
    {
        var query = context.JobRequests
            .AsSplitQuery()
            .Include(j => j.Department)
            .Include(j => j.Equipment)
            .Include(j => j.IssuedBy)
            .Include(j => j.AssignedToEmployee)
            .Include(j => j.AssignedBy)
            .Include(j => j.Service)
            .Include(j => j.Site)
            .Where(j => j.HandlingType == JobHandlingType.Internal && 
                        j.Status == JobRequestStatus.Completed)
            .AsQueryable();

        // Filter by employee if provided
        if (employeeId.HasValue)
        {
            query = query.Where(j => j.AssignedToEmployeeId == employeeId.Value);
        }

        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.WhereSearch(searchQuery, q => q.DescriptionOfWork,
                q => q.Site.Name);
        }

        return await PaginationHelper.GetPaginatedResultAsync(query, page, pageSize,
            mapper.Map<JobRequestDto>);
    }

    public async Task<Result> CompleteJobRequest(CompleteJobRequestRequest request, Guid userId)
    {
        var jobRequest = await context.JobRequests
            .Include(j => j.AssignedToEmployee)
            .Include(j => j.Executions)
            .FirstOrDefaultAsync(j => j.Id == request.JobRequestId);

        if (jobRequest is null)
            return Error.NotFound("JobRequest.NotFound", $"Job request with ID '{request.JobRequestId}' not found");

        var approvalGate = jobRequest.EnsureApprovedForProgression("Job request");
        if (approvalGate.IsFailure)
            return approvalGate;

        if (jobRequest.Status is not (JobRequestStatus.Assigned or JobRequestStatus.JobStarted))
            return Error.Validation("JobRequest.NotInProgress", "Only an assigned or started job request can be completed.");

        // Verify job request is assigned internally
        if (jobRequest.HandlingType != JobHandlingType.Internal)
            return Error.Validation("JobRequest.InvalidHandlingType", "Job request must be assigned internally to be completed");

        if (!jobRequest.AssignedToEmployeeId.HasValue)
            return Error.Validation("JobRequest.NotAssigned", "Job request must be assigned to an employee");

        // Get the user to verify they match the assigned employee
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return Error.Validation("User.Invalid", "Invalid user");

        // Verify the user's email matches the assigned employee's email
        if (jobRequest.AssignedToEmployee?.Email != user.Email)
            return Error.Validation("JobRequest.Unauthorized", "You are not authorized to complete this job request. Only the assigned employee can complete it.");

        // Get the job execution if it exists
        var jobExecution = jobRequest.Executions.FirstOrDefault();

        // Create activity record
        var activity = new JobActivity
        {
            JobExecutionId = jobExecution?.Id,
            ActivityDescription = request.ActivityPerformedNote,
            PerformedAt = DateTime.UtcNow,
            PerformedById = userId,
            Notes = request.Notes
        };

        await context.JobActivities.AddAsync(activity);

        // Update job execution status if it exists
        if (jobExecution != null)
        {
            jobExecution.Status = JobExecutionStatus.Completed;
            jobExecution.CompletedAt = DateTime.UtcNow;
            if (!string.IsNullOrEmpty(request.Notes))
            {
                jobExecution.Notes = request.Notes;
            }
            context.JobExecutions.Update(jobExecution);
        }

        // Update job request status to Completed
        jobRequest.Status = JobRequestStatus.Completed;
        context.JobRequests.Update(jobRequest);

        await context.SaveChangesAsync();

        return Result.Success();
    }

    /// <summary>
    /// The steps set directly on a job request: acknowledging a pending request, the assignee starting assigned work, and
    /// cancelling one that has not begun. Every other stage follows its own action.
    /// </summary>
    public async Task<Result> ChangeJobRequestStatus(Guid id, JobRequestStatus target, Guid actor)
    {
        var jobRequest = await context.JobRequests.Include(j => j.AssignedToEmployee).FirstOrDefaultAsync(j => j.Id == id);
        if (jobRequest is null) return Error.NotFound("JobRequest.NotFound", $"Job request with ID '{id}' not found");
        switch (target)
        {
            case JobRequestStatus.Acknowledged:
                if (jobRequest.Status != JobRequestStatus.Pending) return Error.Validation("JobRequest.Status", "Only a pending job request can be acknowledged.");
                break;
            case JobRequestStatus.JobStarted:
                if (jobRequest.Status != JobRequestStatus.Assigned || jobRequest.HandlingType != JobHandlingType.Internal)
                    return Error.Validation("JobRequest.Status", "Only an internally assigned job request can be started.");
                var user = await userManager.FindByIdAsync(actor.ToString());
                if (user is null || string.IsNullOrWhiteSpace(user.Email) || !string.Equals(jobRequest.AssignedToEmployee?.Email, user.Email, StringComparison.OrdinalIgnoreCase))
                    return Error.Validation("JobRequest.Unauthorized", "Only the assigned employee can start this job.");
                break;
            case JobRequestStatus.Cancelled:
                if (jobRequest.Status is not (JobRequestStatus.Pending or JobRequestStatus.Acknowledged))
                    return Error.Validation("JobRequest.Status", "Work on this job request has begun, so it cannot be cancelled.");
                break;
            default:
                return Error.Validation("JobRequest.Status", "This stage follows its own step.");
        }
        if (target != JobRequestStatus.Cancelled)
        {
            var approvalGate = jobRequest.EnsureApprovedForProgression("Job request");
            if (approvalGate.IsFailure) return approvalGate;
        }
        jobRequest.Status = target;
        jobRequest.LastUpdatedById = actor;
        await context.SaveChangesAsync();
        return Result.Success();
    }
}
