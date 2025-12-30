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

        // Validate services if provided
        if (request.ServiceIds != null && request.ServiceIds.Any())
        {
            foreach (var serviceId in request.ServiceIds)
            {
                var serviceExists = await context.Services.AnyAsync(s => s.Id == serviceId);
                if (!serviceExists) return Error.Validation("Service.Invalid", $"Invalid service: {serviceId}");
            }
        }

        // Validate activities if provided
        if (request.Activities != null && request.Activities.Any())
        {
            foreach (var activity in request.Activities)
            {
                var performedByExists = await userManager.FindByIdAsync(activity.PerformedById.ToString());
                if (performedByExists is null) return Error.Validation("User.Invalid", $"Invalid user for activity: {activity.PerformedById}");
            }
        }

        var jobRequest = mapper.Map<JobRequest>(request);
        jobRequest.DepartmentId = departmentId;
        jobRequest.IssuedById = issuedById;
        
        // Set first service if provided
        if (request.ServiceIds != null && request.ServiceIds.Any())
        {
            jobRequest.ServiceId = request.ServiceIds.First();
        }

        await context.JobRequests.AddAsync(jobRequest);
        await context.SaveChangesAsync();

        // Create activities if provided
        if (request.Activities != null && request.Activities.Any())
        {
            var activities = request.Activities.Select(a => new JobActivity
            {
                JobExecutionId = null, // Activities at creation don't belong to an execution yet
                ActivityDescription = a.ActivityDescription,
                PerformedAt = a.PerformedAt,
                PerformedById = a.PerformedById,
                Notes = a.Notes
            }).ToList();

            await context.JobActivities.AddRangeAsync(activities);
            await context.SaveChangesAsync();
        }

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
            query = query.WhereSearch(searchQuery, q => q.DescriptionOfWork,
                q => q.Location);
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

        return jobExecution.Id;
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