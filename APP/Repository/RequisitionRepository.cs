using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.Materials;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;
using DOMAIN.Entities.Requisitions;
using DOMAIN.Entities.Materials.Batch;

namespace APP.Repository;

public class RequisitionRepository(ApplicationDbContext context, IMapper mapper) : IRequisitionRepository
{
    // ************* CRUD for Requisitions *************

    // Create Stock Requisition
    public async Task<Result<Guid>> CreateRequisition(CreateRequisitionRequest request, Guid userId)
    {
        var requisition = mapper.Map<Requisition>(request);
        requisition.RequestedById = userId;
        await context.Requisitions.AddAsync(requisition);

        var approvals = await context.Approvals.Include(approval => approval.ApprovalStages).FirstOrDefaultAsync(a => a.ItemType == nameof(StockRequisition));

        if (approvals is not null)
        {
            foreach (var approval in approvals.ApprovalStages.Select(approvalStage => new RequisitionApproval
                     {
                         RequisitionId = requisition.Id,
                         UserId = approvalStage.UserId,
                         RoleId = approvalStage.RoleId,
                         Required = approvalStage.Required,
                         Order = approvalStage.Order
                     }))
            {
                await context.RequisitionApprovals.AddAsync(approval);
            }
        }
        await context.SaveChangesAsync();
        return Result.Success(requisition.Id);
    }

    // Get Stock Requisition by ID
    public async Task<Result<RequisitionDto>> GetRequisition(Guid requisitionId)
    {
        var requisition = await context.Requisitions
            .Include(r => r.RequestedBy)
            .Include(r => r.Approvals).ThenInclude(r => r.User)
            .Include(r => r.Approvals).ThenInclude(r => r.Role)
            .Include(r => r.Items)
            .ThenInclude(i => i.Material)
            .Include(r => r.Approvals)
            .FirstOrDefaultAsync(r => r.Id == requisitionId);

        return requisition is null
            ? RequisitionErrors.NotFound(requisitionId)
            : mapper.Map<RequisitionDto>(requisition);
    }

    // Get paginated list of Stock Requisitions
    public async Task<Result<Paginateable<IEnumerable<RequisitionListDto>>>> GetRequisitions(int page, int pageSize, string searchQuery)
    {
        var query = context.Requisitions
            .Include(r => r.Items)
            .ThenInclude(i => i.Material)            .Include(r => r.Approvals)
            .AsQueryable();

        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.WhereSearch(searchQuery, r => r.Comments);
        }

        return await PaginationHelper.GetPaginatedResultAsync(
            query,
            page,
            pageSize,
            mapper.Map<RequisitionListDto>
        );
    }

    // Update Stock Requisition
    public async Task<Result> UpdateRequisition(CreateRequisitionRequest request, Guid requisitionId, Guid userId)
    {
        var existingRequisition = await context.Requisitions.FirstOrDefaultAsync(r => r.Id == requisitionId);
        if (existingRequisition is null)
        {
            return RequisitionErrors.NotFound(requisitionId);
        }

        mapper.Map(request, existingRequisition);
        existingRequisition.LastUpdatedById = userId;

        context.Requisitions.Update(existingRequisition);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    // Delete Stock Requisition (soft delete)
    public async Task<Result> DeleteRequisition(Guid requisitionId, Guid userId)
    {
        var requisition = await context.Requisitions.FirstOrDefaultAsync(r => r.Id == requisitionId);
        if (requisition is null)
        {
            return RequisitionErrors.NotFound(requisitionId);
        }

        requisition.DeletedAt = DateTime.UtcNow;
        requisition.LastDeletedById = userId;

        context.Requisitions.Update(requisition);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    // ************* Manage Stock Requisition Approvals *************

    // Approve Stock Requisition
    public async Task<Result> ApproveRequisition(ApproveRequisitionRequest request, Guid requisitionId, Guid userId, List<Guid> roleIds)
    {
        // Get the requisition and its approvals
        var requisition = await context.Requisitions
            .Include(r => r.Approvals)
            .FirstOrDefaultAsync(r => r.Id == requisitionId);

        if (requisition == null)
        {
            return RequisitionErrors.NotFound(requisitionId);
        }

        // Find the next required approval
        var pendingApproval = requisition.Approvals
            .FirstOrDefault(a => !a.Approved &&
                (a.UserId == userId || (a.RoleId.HasValue && roleIds.Contains(a.RoleId.Value))));

        if (pendingApproval == null)
        {
            return RequisitionErrors.NoPendingApprovals;
        }

        // Mark approval as complete
        pendingApproval.Approved = true;
        pendingApproval.ApprovalTime = DateTime.UtcNow;
        pendingApproval.Comments = request.Comments;

        await context.SaveChangesAsync();

        // Check if all required approvals are complete
        var allApproved = requisition.Approvals.All(a => a.Approved);
        if (allApproved)
        {
            requisition.Approved = true;
        }
        
        await context.SaveChangesAsync();
        return Result.Success();
    }

    // ************* Process Stock Requisition *************

    // Consume stock once the requisition is fully approved

    public async Task<Result> ProcessRequisition(CreateRequisitionRequest request, Guid requisitionId, Guid userId)
     {
         var requisition = await context.Requisitions.FirstOrDefaultAsync(r => r.Id == requisitionId);
         if (requisition is null)
         {
             return RequisitionErrors.NotFound(requisitionId);
         }

         if (!requisition.Approved)
         {
             return RequisitionErrors.PendingApprovals;
         }

         var completedRequisition = mapper.Map<CompletedRequisition>(request);
         completedRequisition.CreatedById = userId;
         completedRequisition.RequisitionId = requisitionId;
         await context.CompletedRequisitions.AddAsync(completedRequisition);
         await context.SaveChangesAsync();

         foreach (var requisitionItem in completedRequisition.Items)
         { 
             var materialId = requisitionItem.MaterialId;
             var requestedQuantity = requisitionItem.Quantity;
             
            // Get the material to check the minimum stock level
            var material = await context.Materials.FirstOrDefaultAsync(m => m.Id == materialId);
            if (material == null)
            {
                return MaterialErrors.NotFound(materialId);
            }

            // Fetch available batches for the material in the specified warehouse
            var availableBatches = await context.MaterialBatches
                .Where(b => b.MaterialId == materialId && b.Status == BatchStatus.Available)
                .OrderBy(b => b.DateReceived)
                .ToListAsync();

            // Sum up the total available quantity from the batches
            var totalAvailable = availableBatches.Sum(b => b.RemainingQuantity);

            // Check if the requested quantity can be fulfilled
            if (totalAvailable < requestedQuantity)
            {
                return MaterialErrors.InsufficientStock;
            }

            // Check if processing the requisition would drop stock below the minimum level
            var totalRemainingAfterRequisition = totalAvailable - requestedQuantity;
            if (totalRemainingAfterRequisition < material.MinimumStockLevel)
            {
                return MaterialErrors.BelowMinimumStock(materialId);
            }

            // Process the requisition: consume stock from batches
            var remainingToConsume = requestedQuantity;

            foreach (var batch in availableBatches)
            {
                int consumedFromBatch;

                if (batch.RemainingQuantity >= remainingToConsume)
                {
                    consumedFromBatch = remainingToConsume;
                    batch.ConsumedQuantity += remainingToConsume;
                    remainingToConsume = 0;
                }
                else
                {
                    consumedFromBatch = batch.RemainingQuantity;
                    batch.ConsumedQuantity = batch.TotalQuantity;  // Fully consume the batch
                    remainingToConsume -= consumedFromBatch;
                }

                // Log the consumption event
                var materialBatchEvent = new MaterialBatchEvent
                {
                    BatchId = batch.Id,
                    Quantity = consumedFromBatch,
                    Type = EventType.Supplied,
                    UserId = requisition.RequestedById,
                };

                await context.MaterialBatchEvents.AddAsync(materialBatchEvent);

                if (remainingToConsume == 0) break;
            }
         }
         completedRequisition.Status = RequestStatus.Completed;
         context.CompletedRequisitions.Update(completedRequisition);
         // Save changes to the database
         await context.SaveChangesAsync();
         return Result.Success();
    }
}
