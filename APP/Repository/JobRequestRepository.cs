using APP.Extensions;
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

public class JobRequestRepository(ApplicationDbContext context, IMapper mapper, UserManager<User> userManager) : IJobRequestRepository
{
    public async Task<Result<Guid>> CreateJobRequest(CreateJobRequest request)
    {
        var department = await context.Departments.AnyAsync(d => d.Id == request.DepartmentId);
        if (!department) return Error.Validation("Department.Invalid", "Invalid department");

        var issuer = await userManager.FindByIdAsync(request.IssuedById.ToString());
        if (issuer is null) return Error.Validation("User.Invalid", "User Invalid");

        if (request.EquipmentId.HasValue)
        {
            var equipment = await context.Equipments.AnyAsync(e => e.Id == request.EquipmentId.Value);
            if (!equipment) return Error.Validation("Equipment.Invalid", "Invalid equipment");
        }

        var jobRequest = mapper.Map<JobRequest>(request);
        await context.JobRequests.AddAsync(jobRequest);
        await context.SaveChangesAsync();
        return jobRequest.Id;
    }

    public async Task<Result<Paginateable<IEnumerable<JobRequestDto>>>> GetJobRequests(int page, int pageSize, 
        string searchQuery = null, JobRequestStatus? status = null, JobHandlingType? handlingType = null, Guid? departmentId = null)
    {
        var query = context.JobRequests
            .AsSplitQuery()
            .Include(j => j.Department)
            .Include(j => j.Equipment)
            .Include(j => j.IssuedBy)
            .Include(j => j.AssignedToEmployee)
            .Include(j => j.AssignedBy)
            .Include(j => j.Service)
            .AsQueryable();

        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.WhereSearch(searchQuery, q => q.DescriptionOfWork, q => q.Location);
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
            entity => mapper.Map<JobRequestDto>(entity));
    }

    public async Task<Result<JobRequestDto>> GetJobRequest(Guid id)
    {
        var jobRequest = await context.JobRequests
            .AsSplitQuery()
            .Include(j => j.Department)
            .Include(j => j.Equipment)
            .Include(j => j.IssuedBy)
            .Include(j => j.AssignedToEmployee)
            .Include(j => j.AssignedBy)
            .Include(j => j.Service)
            .Include(j => j.Executions)
            .Include(j => j.JobOrders)
            .FirstOrDefaultAsync(j => j.Id == id);

        if (jobRequest is null)
            return Error.NotFound("JobRequest.NotFound", "Job request not found");

        return mapper.Map<JobRequestDto>(jobRequest);
    }

    public async Task<Result> UpdateJobRequest(Guid id, UpdateJobRequestRequest request)
    {
        var jobRequest = await context.JobRequests.FirstOrDefaultAsync(j => j.Id == id);
        if (jobRequest is null)
            return Error.NotFound("JobRequest.NotFound", "Job request not found");

        if (request.EquipmentId.HasValue)
        {
            var equipment = await context.Equipments.AnyAsync(e => e.Id == request.EquipmentId.Value);
            if (!equipment) return Error.Validation("Equipment.Invalid", "Invalid equipment");
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

        var employee = await context.Employees.AnyAsync(e => e.Id == request.AssignedToEmployeeId);
        if (!employee) return Error.Validation("Employee.Invalid", "Invalid employee");

        var assignedBy = await userManager.FindByIdAsync(request.AssignedById.ToString());
        if (assignedBy is null) return Error.Validation("User.Invalid", "User Invalid");

        // Update job request
        jobRequest.HandlingType = JobHandlingType.Internal;
        jobRequest.Status = JobRequestStatus.AssignedInternal;
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

        return jobExecution.Id;
    }

    public async Task<Result<Guid>> ReassignJob(ReassignJobRequest request)
    {
        var jobRequest = await context.JobRequests
            .Include(j => j.Executions.Where(e => e.Status != JobExecutionStatus.Cancelled))
            .FirstOrDefaultAsync(j => j.Id == request.JobRequestId);
            
        if (jobRequest is null)
            return Error.NotFound("JobRequest.NotFound", "Job request not found");

        if (jobRequest.HandlingType != JobHandlingType.Internal)
            return Error.Validation("JobRequest.NotInternal", "Only internally assigned jobs can be reassigned");

        // Check if already assigned to the same employee
        if (jobRequest.AssignedToEmployeeId == request.NewAssignedToEmployeeId)
            return Error.Validation("JobRequest.SameEmployee", "Job is already assigned to this employee");

        // Validate new employee exists
        var newEmployee = await context.Employees.AnyAsync(e => e.Id == request.NewAssignedToEmployeeId);
        if (!newEmployee) 
            return Error.Validation("Employee.Invalid", "Invalid employee");

        var reassignedBy = await userManager.FindByIdAsync(request.ReassignedById.ToString());
        if (reassignedBy is null) 
            return Error.Validation("User.Invalid", "User Invalid");

        // Cancel active job execution(s)
        var activeExecutions = jobRequest.Executions
            .Where(e => e.Status != JobExecutionStatus.Cancelled && 
                       e.Status != JobExecutionStatus.Completed && 
                       e.Status != JobExecutionStatus.Approved)
            .ToList();

        foreach (var execution in activeExecutions)
        {
            execution.Status = JobExecutionStatus.Cancelled;
            execution.Notes = $"Cancelled due to reassignment: {request.ReassignmentReason}";
            context.JobExecutions.Update(execution);
        }

        // Update job request
        jobRequest.AssignedToEmployeeId = request.NewAssignedToEmployeeId;
        jobRequest.AssignedAt = DateTime.UtcNow;
        jobRequest.AssignedById = request.ReassignedById;
        jobRequest.Status = JobRequestStatus.AssignedInternal; // Reset status to Assigned

        // Create new job execution
        var newJobExecution = new JobExecution
        {
            JobRequestId = request.JobRequestId,
            AssignedToEmployeeId = request.NewAssignedToEmployeeId,
            AssignedAt = DateTime.UtcNow,
            AssignedById = request.ReassignedById,
            Status = JobExecutionStatus.Assigned,
            Notes = $"Reassigned from previous employee. Reason: {request.ReassignmentReason}" + 
                    (string.IsNullOrEmpty(request.Notes) ? "" : $"\n{request.Notes}")
        };

        await context.JobExecutions.AddAsync(newJobExecution);
        context.JobRequests.Update(jobRequest);
        await context.SaveChangesAsync();

        return newJobExecution.Id;
    }

    public async Task<Result> UpdateJobRequestStatus(Guid id, JobRequestStatus status)
    {
        var jobRequest = await context.JobRequests.FirstOrDefaultAsync(j => j.Id == id);
        if (jobRequest is null)
            return Error.NotFound("JobRequest.NotFound", "Job request not found");

        jobRequest.Status = status;
        context.JobRequests.Update(jobRequest);
        await context.SaveChangesAsync();

        return Result.Success();
    }
}