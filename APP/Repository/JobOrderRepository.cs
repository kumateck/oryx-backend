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

public class JobOrderRepository(ApplicationDbContext context, IMapper mapper, UserManager<User> userManager,
    IJobRequestRepository jobRequestRepository) : IJobOrderRepository
{
    public async Task<Result<Guid>> CreateJobOrder(CreateJobOrderRequest request)
    {
        var jobRequest = await context.JobRequests.FirstOrDefaultAsync(j => j.Id == request.JobRequestId);
        if (jobRequest is null)
            return Error.NotFound("JobRequest.NotFound", "Job request not found");

        if (request.ServiceId.HasValue)
        {
            var service = await context.Services.AnyAsync(s => s.Id == request.ServiceId.Value);
            if (!service) return Error.Validation("Service.Invalid", "Invalid service");
        }

        var issuer = await userManager.FindByIdAsync(request.IssuedById.ToString());
        if (issuer is null) return Error.Validation("User.Invalid", "User Invalid");

        // Get user signature if not provided
        var signature = request.IssuedBySignature;
        if (string.IsNullOrEmpty(signature) && !string.IsNullOrEmpty(issuer.Signature))
        {
            signature = issuer.Signature;
        }

        // Validate service providers
        foreach (var providerId in request.ServiceProviderIds)
        {
            var exists = await context.ServiceProviders.AnyAsync(sp => sp.Id == providerId);
            if (!exists) return Error.Validation("ServiceProvider.Invalid", $"Invalid service provider: {providerId}");
        }

        // Generate code
        var code = await GenerateJobOrderCode();

        var jobOrder = mapper.Map<JobOrder>(request);
        jobOrder.Code = code;
        jobOrder.IssuedBySignature = signature;

        // Add service providers
        jobOrder.ServiceProviders = request.ServiceProviderIds.Select(id => new JobOrderServiceProvider
        {
            ServiceProviderId = id,
            SentAt = DateTime.UtcNow,
            ResponseReceived = false
        }).ToList();

        // Update job request
        jobRequest.HandlingType = JobHandlingType.External;
        jobRequest.Status = JobRequestStatus.SentToExternal;
        if (request.ServiceId.HasValue)
        {
            jobRequest.ServiceId = request.ServiceId.Value;
        }

        await context.JobOrders.AddAsync(jobOrder);
        context.JobRequests.Update(jobRequest);
        await context.SaveChangesAsync();
        
        await SendJobOrderToProviders(new SendJobOrderToProvidersRequest
        {
            JobOrderId = jobOrder.Id,
            ServiceProviderIds = request.ServiceProviderIds
        });

        return jobOrder.Id;
    }

    public async Task<Result<Paginateable<IEnumerable<JobOrderDto>>>> GetJobOrders(int page, int pageSize,
        JobOrderStatus? status = null, Guid? jobRequestId = null, Guid? serviceId = null)
    {
        var query = context.JobOrders
            .AsSplitQuery()
            .Include(j => j.JobRequest)
            .Include(j => j.Service)
            .Include(j => j.IssuedBy)
            .Include(j => j.ServiceProviders).ThenInclude(sp => sp.ServiceProvider)
            .Include(j => j.Quotations).ThenInclude(q => q.ServiceProvider)
            .Include(j => j.SelectedQuotation)
            .Include(j => j.ServiceMemo)
            .AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(j => j.Status == status.Value);
        }

        if (jobRequestId.HasValue)
        {
            query = query.Where(j => j.JobRequestId == jobRequestId.Value);
        }

        if (serviceId.HasValue)
        {
            query = query.Where(j => j.ServiceId == serviceId.Value);
        }

        return await PaginationHelper.GetPaginatedResultAsync(query, page, pageSize,
            mapper.Map<JobOrderDto>);
    }

    public async Task<Result<JobOrderDto>> GetJobOrder(Guid id)
    {
        var jobOrder = await context.JobOrders
            .AsSplitQuery()
            .Include(j => j.JobRequest).ThenInclude(jr => jr.Department)
            .Include(j => j.Service)
            .Include(j => j.IssuedBy)
            .Include(j => j.ServiceProviders).ThenInclude(sp => sp.ServiceProvider)
            .Include(j => j.Quotations).ThenInclude(q => q.ServiceProvider)
            .Include(j => j.Quotations).ThenInclude(q => q.Items)
            .Include(j => j.SelectedQuotation)
            .Include(j => j.ServiceMemo)
            .Include(j => j.Execution).ThenInclude(e => e.Activities)
            .Include(j => j.Execution).ThenInclude(e => e.ConsumedItems)
            .FirstOrDefaultAsync(j => j.Id == id);

        if (jobOrder is null)
            return Error.NotFound("JobOrder.NotFound", "Job order not found");

        return mapper.Map<JobOrderDto>(jobOrder);
    }

    public async Task<Result> SendJobOrderToProviders(SendJobOrderToProvidersRequest request)
    {
        var jobOrder = await context.JobOrders
            .AsSplitQuery()
            .Include(j => j.ServiceProviders)
            .FirstOrDefaultAsync(j => j.Id == request.JobOrderId);

        if (jobOrder is null)
            return Error.NotFound("JobOrder.NotFound", "Job order not found");

        // Add new service providers
        foreach (var providerId in request.ServiceProviderIds)
        {
            var exists = await context.ServiceProviders.AnyAsync(sp => sp.Id == providerId);
            if (!exists) return Error.Validation("ServiceProvider.Invalid", $"Invalid service provider: {providerId}");

            // Check if already sent to this provider
            if (jobOrder.ServiceProviders.All(sp => sp.ServiceProviderId != providerId))
            {
                jobOrder.ServiceProviders.Add(new JobOrderServiceProvider
                {
                    ServiceProviderId = providerId,
                    SentAt = DateTime.UtcNow,
                    ResponseReceived = false
                });
            }
        }

        jobOrder.Status = JobOrderStatus.SentToProviders;
        context.JobOrders.Update(jobOrder);
        await context.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result> SelectQuotation(SelectQuotationRequest request)
    {
        var jobOrder = await context.JobOrders
            .AsSplitQuery()
            .Include(j => j.Quotations)
            .FirstOrDefaultAsync(j => j.Id == request.JobOrderId);

        if (jobOrder is null)
            return Error.NotFound("JobOrder.NotFound", "Job order not found");

        var quotation = await context.ServiceQuotations
            .FirstOrDefaultAsync(q => q.Id == request.QuotationId && q.JobOrderId == request.JobOrderId);

        if (quotation is null)
            return Error.NotFound("Quotation.NotFound", "Quotation not found");

        // Update all quotations - mark selected one
        foreach (var quot in jobOrder.Quotations)
        {
            quot.IsSelected = quot.Id == request.QuotationId;
            quot.Status = quot.Id == request.QuotationId ? QuotationStatus.Selected : QuotationStatus.Rejected;
        }

        jobOrder.SelectedQuotationId = request.QuotationId;
        jobOrder.Status = JobOrderStatus.QuotationSelected;

        context.JobOrders.Update(jobOrder);

        // Update job request status
        await jobRequestRepository.UpdateJobRequestStatus(jobOrder.JobRequestId, JobRequestStatus.ContractorSelected);

        await context.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result<Guid>> StartJobOrderExecution(StartJobOrderExecutionRequest request)
    {
        var jobOrder = await context.JobOrders.FirstOrDefaultAsync(j => j.Id == request.JobOrderId);
        if (jobOrder is null)
            return Error.NotFound("JobOrder.NotFound", "Job order not found");

        if (jobOrder.Status != JobOrderStatus.MemoCreated)
            return Error.Validation("JobOrder.InvalidStatus", "Service memo must be created before starting execution");

        var serviceProvider = await context.ServiceProviders.AnyAsync(sp => sp.Id == request.ServiceProviderId);
        if (!serviceProvider) return Error.Validation("ServiceProvider.Invalid", "Invalid service provider");

        var execution = new JobOrderExecution
        {
            JobOrderId = request.JobOrderId,
            ServiceProviderId = request.ServiceProviderId,
            StartedAt = DateTime.UtcNow,
            Status = JobOrderExecutionStatus.InProgress,
            Notes = request.Notes
        };

        jobOrder.Status = JobOrderStatus.InProgress;

        await context.JobOrderExecutions.AddAsync(execution);
        context.JobOrders.Update(jobOrder);

        // Update job request status
        await jobRequestRepository.UpdateJobRequestStatus(jobOrder.JobRequestId, JobRequestStatus.JobStarted);

        await context.SaveChangesAsync();

        return execution.Id;
    }

    public async Task<Result<Guid>> RecordJobOrderActivity(Guid jobOrderExecutionId, RecordJobActivityRequest request)
    {
        var execution = await context.JobOrderExecutions.FirstOrDefaultAsync(e => e.Id == jobOrderExecutionId);
        if (execution is null)
            return Error.NotFound("JobOrderExecution.NotFound", "Job order execution not found");

        var performedBy = await userManager.FindByIdAsync(request.PerformedById.ToString());
        if (performedBy is null) return Error.Validation("User.Invalid", "User Invalid");

        var activity = new JobActivity
        {
            JobOrderExecutionId = jobOrderExecutionId,
            ActivityDescription = request.ActivityDescription,
            PerformedAt = request.PerformedAt,
            PerformedById = request.PerformedById,
            Notes = request.Notes
        };

        await context.JobActivities.AddAsync(activity);
        await context.SaveChangesAsync();

        return activity.Id;
    }

    public async Task<Result<Guid>> RecordJobOrderConsumedItem(Guid jobOrderExecutionId, RecordConsumedItemRequest request)
    {
        var execution = await context.JobOrderExecutions.FirstOrDefaultAsync(e => e.Id == jobOrderExecutionId);
        if (execution is null)
            return Error.NotFound("JobOrderExecution.NotFound", "Job order execution not found");

        var item = await context.Items.FirstOrDefaultAsync(i => i.Id == request.ItemId);
        if (item is null) return Error.Validation("Item.Invalid", "Invalid item");

        var uom = await context.UnitOfMeasures.AnyAsync(u => u.Id == request.UnitOfMeasureId);
        if (!uom) return Error.Validation("UnitOfMeasure.Invalid", "Invalid unit of measure");

        var consumedItem = new ConsumedItem
        {
            JobOrderExecutionId = jobOrderExecutionId,
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

    public async Task<Result> CompleteJobOrderExecution(CompleteJobOrderExecutionRequest request)
    {
        var execution = await context.JobOrderExecutions
            .AsSplitQuery()
            .Include(e => e.JobOrder)
            .FirstOrDefaultAsync(e => e.Id == request.JobOrderExecutionId);

        if (execution is null)
            return Error.NotFound("JobOrderExecution.NotFound", "Job order execution not found");

        if (execution.Status != JobOrderExecutionStatus.InProgress)
            return Error.Validation("JobOrderExecution.InvalidStatus", "Job order execution must be in progress to complete");

        execution.Status = JobOrderExecutionStatus.Completed;
        execution.CompletedAt = DateTime.UtcNow;
        if (!string.IsNullOrEmpty(request.Notes))
        {
            execution.Notes = request.Notes;
        }

        // Record activities
        foreach (var activity in request.Activities)
        {
            var jobActivity = new JobActivity
            {
                JobOrderExecutionId = request.JobOrderExecutionId,
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
                JobOrderExecutionId = request.JobOrderExecutionId,
                ItemId = item.ItemId,
                QuantityConsumed = item.QuantityConsumed,
                UnitOfMeasureId = item.UnitOfMeasureId,
                Notes = item.Notes,
                Source = item.Source
            };
            await context.ConsumedItems.AddAsync(consumedItem);
        }

        execution.JobOrder.Status = JobOrderStatus.Completed;

        context.JobOrderExecutions.Update(execution);

        // Update job request status
        await jobRequestRepository.UpdateJobRequestStatus(execution.JobOrder.JobRequestId, JobRequestStatus.Completed);

        await context.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result> VerifyJobOrderExecution(VerifyJobOrderExecutionRequest request)
    {
        var execution = await context.JobOrderExecutions.FirstOrDefaultAsync(e => e.Id == request.JobOrderExecutionId);
        if (execution is null)
            return Error.NotFound("JobOrderExecution.NotFound", "Job order execution not found");

        if (execution.Status != JobOrderExecutionStatus.Completed)
            return Error.Validation("JobOrderExecution.InvalidStatus", "Job order execution must be completed before verification");

        var verifiedBy = await userManager.FindByIdAsync(request.VerifiedById.ToString());
        if (verifiedBy is null) return Error.Validation("User.Invalid", "User Invalid");

        execution.Status = JobOrderExecutionStatus.VerifiedBySupervisor;
        execution.VerifiedAt = DateTime.UtcNow;
        execution.VerifiedById = request.VerifiedById;
        execution.VerificationComments = request.VerificationComments;

        context.JobOrderExecutions.Update(execution);
        await context.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result> ApproveJobOrderExecution(ApproveJobOrderExecutionRequest request)
    {
        var execution = await context.JobOrderExecutions
            .AsSplitQuery()
            .Include(e => e.JobOrder)
            .FirstOrDefaultAsync(e => e.Id == request.JobOrderExecutionId);

        if (execution is null)
            return Error.NotFound("JobOrderExecution.NotFound", "Job order execution not found");

        if (execution.Status != JobOrderExecutionStatus.VerifiedBySupervisor)
            return Error.Validation("JobOrderExecution.InvalidStatus", "Job order execution must be verified before approval");

        var approvedBy = await userManager.FindByIdAsync(request.ApprovedById.ToString());
        if (approvedBy is null) return Error.Validation("User.Invalid", "User Invalid");

        execution.Status = JobOrderExecutionStatus.ApprovedByRequester;
        execution.ApprovedAt = DateTime.UtcNow;
        execution.ApprovedById = request.ApprovedById;
        execution.ApprovalComments = request.ApprovalComments;
        execution.RequesterSatisfied = request.RequesterSatisfied;
        execution.RequesterComments = request.RequesterComments;

        execution.JobOrder.Status = JobOrderStatus.Approved;

        context.JobOrderExecutions.Update(execution);

        // Update job request status
        await jobRequestRepository.UpdateJobRequestStatus(execution.JobOrder.JobRequestId, JobRequestStatus.Approved);

        await context.SaveChangesAsync();

        return Result.Success();
    }

    private async Task<string> GenerateJobOrderCode()
    {
        var count = await context.JobOrders.CountAsync();
        return $"JO-{DateTime.UtcNow:yyyyMM}-{count + 1:D4}";
    }
}

