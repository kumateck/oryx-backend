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
using DOMAIN.Entities.Requisitions.Request;

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

        var approvals = await context.Approvals.Include(approval => approval.ApprovalStages)
            .FirstOrDefaultAsync(a => a.RequisitionType == request.RequisitionType);

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
            .FirstOrDefaultAsync(r => r.Id == requisitionId);

        return requisition is null
            ? RequisitionErrors.NotFound(requisitionId)
            : mapper.Map<RequisitionDto>(requisition);
    }

    // Get paginated list of Stock Requisitions
    public async Task<Result<Paginateable<IEnumerable<RequisitionDto>>>> GetRequisitions(int page, int pageSize, string searchQuery, RequestStatus? status)
    {
        var query = context.Requisitions
            .Include(r => r.RequestedBy)
            .Include(r => r.Approvals).ThenInclude(r => r.User)
            .Include(r => r.Approvals).ThenInclude(r => r.Role)
            .Include(r => r.Items)
            .ThenInclude(i => i.Material)
            .AsQueryable();
        
        if (status.HasValue)
        {
            query = query.Where(r => r.Status == status);
        }

        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.WhereSearch(searchQuery, r => r.Comments);
        }

        return await PaginationHelper.GetPaginatedResultAsync(
            query,
            page,
            pageSize,
            mapper.Map<RequisitionDto>
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
    
        // ************* CRUD for SourceRequisition *************

    // Create Source Requisition
    public async Task<Result<Guid>> CreateSourceRequisition(CreateSourceRequisitionRequest request, Guid userId)
    {
        var requisition = await context.Requisitions.FirstOrDefaultAsync(r => r.Id == request.RequisitionId);
        if (requisition is null) return RequisitionErrors.NotFound(request.RequisitionId);
        var sourceRequisition = mapper.Map<SourceRequisition>(request);
        sourceRequisition.CreatedById = userId;
        await context.SourceRequisitions.AddAsync(sourceRequisition);
        requisition.Status = RequestStatus.Sourced;
        context.Requisitions.Update(requisition);
        await context.SaveChangesAsync();
        return sourceRequisition.Id;
    }

    // Get Source Requisition by ID
    public async Task<Result<SourceRequisitionDto>> GetSourceRequisition(Guid sourceRequisitionId)
    {
        var sourceRequisition = await context.SourceRequisitions
            .Include(sr => sr.Requisition)
            .Include(sr => sr.Items).ThenInclude(sr => sr.Suppliers)
            .Include(sr => sr.Items).ThenInclude(item => item.Material)
            .Include(sr => sr.Items).ThenInclude(item => item.UoM)
            .FirstOrDefaultAsync(sr => sr.Id == sourceRequisitionId);

        return sourceRequisition is null
            ? RequisitionErrors.NotFound(sourceRequisitionId)
            : mapper.Map<SourceRequisitionDto>(sourceRequisition, opt =>
            {
                opt.Items[AppConstants.ModelType] = nameof(SourceRequisition);
            });
    }

    // Get paginated list of Source Requisitions
    public async Task<Result<Paginateable<IEnumerable<SourceRequisitionDto>>>> GetSourceRequisitions(int page, int pageSize, string searchQuery)
    {
        var query = context.SourceRequisitions
            .Include(sr => sr.Requisition)
            .Include(sr => sr.Items).ThenInclude(sr => sr.Suppliers)
            .Include(sr => sr.Items).ThenInclude(item => item.Material)
            .Include(sr => sr.Items).ThenInclude(item => item.UoM)
            .AsQueryable();

        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.WhereSearch(searchQuery, sr => sr.Code);
        }

        return await PaginationHelper.GetPaginatedResultAsync(
            query,
            page,
            pageSize,
            mapper.Map<SourceRequisitionDto>
        );
    }
    
    public async Task<Result<Paginateable<IEnumerable<SourceRequisitionItemDto>>>> GetSourceRequisitionItems(int page, int pageSize,  ProcurementSource source)
    {
        var query = context.SourceRequisitionItems
            .Include(sr => sr.SourceRequisition)
            .Include(sr => sr.Material)
            .Include(sr => sr.UoM)
            .Include(sr => sr.Suppliers)
            .Where(sr => sr.Source == source)
            .AsQueryable();

      
        return await PaginationHelper.GetPaginatedResultAsync(
            query,
            page,
            pageSize,
            mapper.Map<SourceRequisitionItemDto>
        );
    }

    // Update Source Requisition
    public async Task<Result> UpdateSourceRequisition(CreateSourceRequisitionRequest request, Guid sourceRequisitionId)
    {
        var existingSourceRequisition = await context.SourceRequisitions.FirstOrDefaultAsync(sr => sr.Id == sourceRequisitionId);
        if (existingSourceRequisition is null)
        {
            return RequisitionErrors.NotFound(sourceRequisitionId);
        }

        mapper.Map(request, existingSourceRequisition);
        context.SourceRequisitions.Update(existingSourceRequisition);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    // Delete Source Requisition (soft delete)
    public async Task<Result> DeleteSourceRequisition(Guid sourceRequisitionId)
    {
        var sourceRequisition = await context.SourceRequisitions.FirstOrDefaultAsync(sr => sr.Id == sourceRequisitionId);
        if (sourceRequisition is null)
        {
            return RequisitionErrors.NotFound(sourceRequisitionId);
        }

        sourceRequisition.DeletedAt = DateTime.UtcNow;
        context.SourceRequisitions.Update(sourceRequisition);
        await context.SaveChangesAsync();
        return Result.Success();
    }
    
    public async Task<Result<Paginateable<IEnumerable<SupplierQuotationDto>>>> GetSuppliersWithSourceRequisitionItems(int page, int pageSize, ProcurementSource source, bool sent)
    {
        // Base query
        var query =  context.SourceRequisitionItemSuppliers
            .Include(s => s.Supplier)
            .Include(s => s.SourceRequisitionItem)
            .ThenInclude(item => item.Material)
            .Include(s => s.SourceRequisitionItem)
            .ThenInclude(item => item.UoM)
            .Include(s => s.SourceRequisitionItem)
            .ThenInclude(item => item.SourceRequisition)
            .Where(s => s.SourceRequisitionItem.Source == source)
            .AsQueryable();

        var sourceRequisitionItemSuppliers = sent ? await query.Where(s => s.SentQuotationRequestAt != null).ToListAsync() 
            : await query.Where(s => s.SentQuotationRequestAt == null).ToListAsync();

        // Group the query
        var groupedQuery = sourceRequisitionItemSuppliers
            .GroupBy(s => s.Supplier)
            .Select(item => new SupplierQuotationDto
            {
                Supplier = mapper.Map<CollectionItemDto>(item.Key),
                SentQuotationRequestAt = sent ? item.Min(s => s.SentQuotationRequestAt) : null, 
                Items = mapper.Map<List<SourceRequisitionItemDto>>(item.Select(i => i.SourceRequisitionItem))
            }).ToList();
        
        return PaginationHelper.Paginate(page, pageSize, groupedQuery);
    }
    
    public async Task<Result<SupplierQuotationDto>> GetSuppliersWithSourceRequisitionItems(Guid supplierId)
    {
        var query =  await context.SourceRequisitionItemSuppliers
            .Include(s => s.Supplier)
            .Include(s => s.SourceRequisitionItem)
            .ThenInclude(item => item.Material)
            .Include(s => s.SourceRequisitionItem)
            .ThenInclude(item => item.UoM)
            .Include(s => s.SourceRequisitionItem)
            .ThenInclude(item => item.SourceRequisition)
            .Where(s => s.SupplierId == supplierId && s.SentQuotationRequestAt == null)
            .ToListAsync();

        return new SupplierQuotationDto
        {
            Supplier = mapper.Map<CollectionItemDto>(query.FirstOrDefault()?.Supplier),
            Items = mapper.Map<List<SourceRequisitionItemDto>>(query.Select(i => i.SourceRequisitionItem))
        };
    }
    
    public async Task<Result> MarkQuotationAsSent(Guid supplierId)
    {
        var itemsToUpdate = await context.SourceRequisitionItemSuppliers
            .Where(s => s.SupplierId == supplierId && s.SentQuotationRequestAt == null)
            .ToListAsync();

        if (itemsToUpdate.Count == 0)
        {
            return Error.Validation("Supplier.Quotation", "No items found to mark as quotation sent for the specified supplier.");
        }

        foreach (var item in itemsToUpdate)
        {
            item.SentQuotationRequestAt = DateTime.UtcNow;
        }

        // Save changes to the database
        await context.SaveChangesAsync();
        return Result.Success();
    }
}
