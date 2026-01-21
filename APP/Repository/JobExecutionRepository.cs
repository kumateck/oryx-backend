using APP.IRepository;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.JobRequests;
using DOMAIN.Entities.Users;
using INFRASTRUCTURE.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public class JobExecutionRepository(ApplicationDbContext context, IMapper mapper, UserManager<User> userManager,
    IJobRequestRepository jobRequestRepository) : IJobExecutionRepository
{
    public async Task<Result<Paginateable<IEnumerable<JobExecutionDto>>>> GetJobExecutions(int page, int pageSize,
        JobExecutionStatus? status = null, Guid? employeeId = null, Guid? jobRequestId = null)
    {
        var query = context.JobExecutions
            .Include(j => j.JobRequest)
            .Include(j => j.AssignedToEmployee)
            .Include(j => j.AssignedBy)
            .Include(j => j.VerifiedBy)
            .Include(j => j.ApprovedBy)
            .AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(j => j.Status == status.Value);
        }

        if (employeeId.HasValue)
        {
            query = query.Where(j => j.AssignedToEmployeeId == employeeId.Value);
        }

        if (jobRequestId.HasValue)
        {
            query = query.Where(j => j.JobRequestId == jobRequestId.Value);
        }

        return await PaginationHelper.GetPaginatedResultAsync(query, page, pageSize,
            entity => mapper.Map<JobExecutionDto>(entity));
    }

    public async Task<Result<JobExecutionDto>> GetJobExecution(Guid id)
    {
        var jobExecution = await context.JobExecutions
            .Include(j => j.JobRequest)
            .Include(j => j.AssignedToEmployee)
            .Include(j => j.AssignedBy)
            .Include(j => j.VerifiedBy)
            .Include(j => j.ApprovedBy)
            .Include(j => j.Activities).ThenInclude(a => a.PerformedBy)
            .Include(j => j.ConsumedItems).ThenInclude(c => c.Item)
            .Include(j => j.ConsumedItems).ThenInclude(c => c.UnitOfMeasure)
            .FirstOrDefaultAsync(j => j.Id == id);

        if (jobExecution is null)
            return Error.NotFound("JobExecution.NotFound", "Job execution not found");

        return mapper.Map<JobExecutionDto>(jobExecution);
    }

    public async Task<Result> AcknowledgeJobExecution(AcknowledgeJobExecutionRequest request)
    {
        var jobExecution = await context.JobExecutions.FirstOrDefaultAsync(j => j.Id == request.JobExecutionId);
        if (jobExecution is null)
            return Error.NotFound("JobExecution.NotFound", "Job execution not found");

        if (jobExecution.Status != JobExecutionStatus.Assigned)
            return Error.Validation("JobExecution.InvalidStatus", "Job execution cannot be acknowledged in current status");

        jobExecution.Status = JobExecutionStatus.Acknowledged;
        jobExecution.AcknowledgedAt = DateTime.UtcNow;

        context.JobExecutions.Update(jobExecution);

        // Update job request status
        await jobRequestRepository.UpdateJobRequestStatus(jobExecution.JobRequestId, JobRequestStatus.Acknowledged);

        await context.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result> StartJobExecution(StartJobExecutionRequest request, Guid userId)
    {
        var jobExecution = await context.JobExecutions.FirstOrDefaultAsync(j => j.Id == request.JobExecutionId);
        if (jobExecution is null)
            return Error.NotFound("JobExecution.NotFound", "Job execution not found");

        if (jobExecution.Status != JobExecutionStatus.Acknowledged && jobExecution.Status != JobExecutionStatus.Assigned)
            return Error.Validation("JobExecution.InvalidStatus", "Job execution must be assigned or acknowledged before starting");

        var performedBy = await userManager.FindByIdAsync(userId.ToString());
        if (performedBy is null) return Error.Validation("User.Invalid", "User Invalid");

        jobExecution.Status = JobExecutionStatus.InProgress;
        jobExecution.StartedAt = DateTime.UtcNow;
        if (!string.IsNullOrEmpty(request.Notes))
        {
            jobExecution.Notes = request.Notes;
        }

        // Automatically log the start activity
        var startActivity = new JobActivity
        {
            JobExecutionId = request.JobExecutionId,
            ActivityDescription = request.ActivityDescription,
            PerformedAt = DateTime.UtcNow,
            PerformedById = userId,
            Notes = request.Notes
        };
        await context.JobActivities.AddAsync(startActivity);

        context.JobExecutions.Update(jobExecution);

        // Update job request status
        await jobRequestRepository.UpdateJobRequestStatus(jobExecution.JobRequestId, JobRequestStatus.JobStarted);

        await context.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result<Guid>> RecordJobActivity(Guid jobExecutionId, RecordJobActivityRequest request)
    {
        var jobExecution = await context.JobExecutions.FirstOrDefaultAsync(j => j.Id == jobExecutionId);
        if (jobExecution is null)
            return Error.NotFound("JobExecution.NotFound", "Job execution not found");

        var performedBy = await userManager.FindByIdAsync(request.PerformedById.ToString());
        if (performedBy is null) return Error.Validation("User.Invalid", "User Invalid");

        var activity = new JobActivity
        {
            JobExecutionId = jobExecutionId,
            ActivityDescription = request.ActivityDescription,
            PerformedAt = request.PerformedAt,
            PerformedById = request.PerformedById,
            Notes = request.Notes
        };

        await context.JobActivities.AddAsync(activity);
        await context.SaveChangesAsync();

        return activity.Id;
    }

    public async Task<Result<Guid>> RecordConsumedItem(Guid jobExecutionId, RecordConsumedItemRequest request)
    {
        var jobExecution = await context.JobExecutions.FirstOrDefaultAsync(j => j.Id == jobExecutionId);
        if (jobExecution is null)
            return Error.NotFound("JobExecution.NotFound", "Job execution not found");

        var item = await context.Items.FirstOrDefaultAsync(i => i.Id == request.ItemId);
        if (item is null) return Error.Validation("Item.Invalid", "Invalid item");

        var uom = await context.UnitOfMeasures.AnyAsync(u => u.Id == request.UnitOfMeasureId);
        if (!uom) return Error.Validation("UnitOfMeasure.Invalid", "Invalid unit of measure");

        var consumedItem = new ConsumedItem
        {
            JobExecutionId = jobExecutionId,
            ItemId = request.ItemId,
            QuantityConsumed = request.QuantityConsumed,
            UnitOfMeasureId = request.UnitOfMeasureId,
            Notes = request.Notes,
            Source = request.Source
        };

        await context.ConsumedItems.AddAsync(consumedItem);
        await context.SaveChangesAsync();

        return consumedItem.Id;
    }

    public async Task<Result> CompleteJobExecution(CompleteJobExecutionRequest request, Guid userId)
    {
        var jobExecution = await context.JobExecutions.FirstOrDefaultAsync(j => j.Id == request.JobExecutionId);
        if (jobExecution is null)
            return Error.NotFound("JobExecution.NotFound", "Job execution not found");

        if (jobExecution.Status != JobExecutionStatus.InProgress)
            return Error.Validation("JobExecution.InvalidStatus", "Job execution must be in progress to complete");

        var performedBy = await userManager.FindByIdAsync(userId.ToString());
        if (performedBy is null) return Error.Validation("User.Invalid", "User Invalid");

        jobExecution.Status = JobExecutionStatus.Completed;
        jobExecution.CompletedAt = DateTime.UtcNow;
        if (!string.IsNullOrEmpty(request.Notes))
        {
            jobExecution.Notes = request.Notes;
        }

        // Automatically log the completion activity
        var completionActivity = new JobActivity
        {
            JobExecutionId = request.JobExecutionId,
            ActivityDescription = request.ActivityDescription,
            PerformedAt = DateTime.UtcNow,
            PerformedById = userId,
            Notes = request.Notes
        };
        await context.JobActivities.AddAsync(completionActivity);

        // Record additional activities if provided
        foreach (var activity in request.AdditionalActivities)
        {
            var jobActivity = new JobActivity
            {
                JobExecutionId = request.JobExecutionId,
                ActivityDescription = activity.ActivityDescription,
                PerformedAt = activity.PerformedAt,
                PerformedById = activity.PerformedById,
                Notes = activity.Notes
            };
            await context.JobActivities.AddAsync(jobActivity);
        }

        // Record consumed items
        foreach (var item in request.ConsumedItems)
        {
            var consumedItem = new ConsumedItem
            {
                JobExecutionId = request.JobExecutionId,
                ItemId = item.ItemId,
                QuantityConsumed = item.QuantityConsumed,
                UnitOfMeasureId = item.UnitOfMeasureId,
                Notes = item.Notes,
                Source = item.Source
            };
            await context.ConsumedItems.AddAsync(consumedItem);
        }

        context.JobExecutions.Update(jobExecution);

        // Update job request status
        await jobRequestRepository.UpdateJobRequestStatus(jobExecution.JobRequestId, JobRequestStatus.Completed);

        await context.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result> ReassignJobExecution(ReassignJobExecutionRequest request, Guid reassignedById)
    {
        var jobExecution = await context.JobExecutions
            .Include(j => j.JobRequest)
            .FirstOrDefaultAsync(j => j.Id == request.JobExecutionId);
        if (jobExecution is null)
            return Error.NotFound("JobExecution.NotFound", "Job execution not found");

        // Cannot reassign if already completed, verified, approved, or cancelled
        if (jobExecution.Status == JobExecutionStatus.Completed ||
            jobExecution.Status == JobExecutionStatus.VerifiedBySupervisor ||
            jobExecution.Status == JobExecutionStatus.Approved ||
            jobExecution.Status == JobExecutionStatus.Cancelled)
        {
            return Error.Validation("JobExecution.InvalidStatus", "Cannot reassign a job that is completed, verified, approved, or cancelled");
        }

        // Validate new employee exists
        var newEmployee = await context.Employees.AnyAsync(e => e.Id == request.NewEmployeeId);
        if (!newEmployee) return Error.Validation("Employee.Invalid", "Invalid employee");

        // Validate reassigned by user exists
        var reassignedBy = await userManager.FindByIdAsync(reassignedById.ToString());
        if (reassignedBy is null) return Error.Validation("User.Invalid", "User Invalid");

        // Store old employee for activity log
        var oldEmployeeId = jobExecution.AssignedToEmployeeId;

        // Update job execution
        jobExecution.AssignedToEmployeeId = request.NewEmployeeId;
        jobExecution.AssignedAt = DateTime.UtcNow;
        jobExecution.AssignedById = reassignedById;
        jobExecution.Status = JobExecutionStatus.Assigned; // Reset to assigned
        jobExecution.AcknowledgedAt = null; // Reset acknowledgment
        jobExecution.StartedAt = null; // Reset start time
        if (!string.IsNullOrEmpty(request.Notes))
        {
            jobExecution.Notes = request.Notes;
        }

        // Update job request
        var jobRequest = jobExecution.JobRequest;
        jobRequest.AssignedToEmployeeId = request.NewEmployeeId;
        jobRequest.AssignedAt = DateTime.UtcNow;
        jobRequest.AssignedById = reassignedById;
        jobRequest.Status = JobRequestStatus.Assigned; // Reset to assigned

        // Log reassignment activity
        var reassignmentActivity = new JobActivity
        {
            JobExecutionId = request.JobExecutionId,
            ActivityDescription = $"Job reassigned from employee {oldEmployeeId} to employee {request.NewEmployeeId}. Reason: {request.Reason}",
            PerformedAt = DateTime.UtcNow,
            PerformedById = reassignedById,
            Notes = request.Notes
        };
        await context.JobActivities.AddAsync(reassignmentActivity);

        context.JobExecutions.Update(jobExecution);
        context.JobRequests.Update(jobRequest);
        await context.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result> VerifyJobExecution(VerifyJobExecutionRequest request)
    {
        var jobExecution = await context.JobExecutions.FirstOrDefaultAsync(j => j.Id == request.JobExecutionId);
        if (jobExecution is null)
            return Error.NotFound("JobExecution.NotFound", "Job execution not found");

        if (jobExecution.Status != JobExecutionStatus.Completed)
            return Error.Validation("JobExecution.InvalidStatus", "Job execution must be completed before verification");

        var verifiedBy = await userManager.FindByIdAsync(request.VerifiedById.ToString());
        if (verifiedBy is null) return Error.Validation("User.Invalid", "User Invalid");

        jobExecution.Status = JobExecutionStatus.VerifiedBySupervisor;
        jobExecution.VerifiedAt = DateTime.UtcNow;
        jobExecution.VerifiedById = request.VerifiedById;
        jobExecution.VerificationComments = request.VerificationComments;

        context.JobExecutions.Update(jobExecution);
        await context.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result> ApproveJobExecution(ApproveJobExecutionRequest request)
    {
        var jobExecution = await context.JobExecutions.FirstOrDefaultAsync(j => j.Id == request.JobExecutionId);
        if (jobExecution is null)
            return Error.NotFound("JobExecution.NotFound", "Job execution not found");

        if (jobExecution.Status != JobExecutionStatus.VerifiedBySupervisor)
            return Error.Validation("JobExecution.InvalidStatus", "Job execution must be verified before approval");

        var approvedBy = await userManager.FindByIdAsync(request.ApprovedById.ToString());
        if (approvedBy is null) return Error.Validation("User.Invalid", "User Invalid");

        jobExecution.Status = JobExecutionStatus.Approved;
        jobExecution.ApprovedAt = DateTime.UtcNow;
        jobExecution.ApprovedById = request.ApprovedById;
        jobExecution.ApprovalComments = request.ApprovalComments;

        context.JobExecutions.Update(jobExecution);

        // Update job request status
        await jobRequestRepository.UpdateJobRequestStatus(jobExecution.JobRequestId, JobRequestStatus.Approved);

        await context.SaveChangesAsync();

        return Result.Success();
    }
}

