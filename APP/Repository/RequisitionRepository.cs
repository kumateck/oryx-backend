using APP.Extensions;
using APP.IRepository;
using APP.Services.Email;
using APP.Services.Pdf;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Materials;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;
using DOMAIN.Entities.Requisitions;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.Procurement.Suppliers;
using DOMAIN.Entities.PurchaseOrders;
using DOMAIN.Entities.PurchaseOrders.Request;
using DOMAIN.Entities.Requisitions.Request;
using DOMAIN.Entities.Warehouses;

namespace APP.Repository;

public class RequisitionRepository(ApplicationDbContext context, IMapper mapper, IProcurementRepository procurementRepository, 
    IEmailService emailService, IPdfService pdfService, IConfigurationRepository configurationRepository, IMaterialRepository materialRepository) : IRequisitionRepository
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

        if (request.ProductionActivityStepId.HasValue)
        {
            var activityStep =
                await context.ProductionActivitySteps.FirstOrDefaultAsync(p => p.Id == request.ProductionActivityStepId);

            if (activityStep is not null)
            {
                activityStep.Status = ProductionStatus.InProgress;
                context.ProductionActivitySteps.Update(activityStep);
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
    public async Task<Result<Paginateable<IEnumerable<RequisitionDto>>>> GetRequisitions(int page, int pageSize, string searchQuery, RequestStatus? status, RequisitionType? requisitionType)
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

        if (requisitionType.HasValue)
        {
            query = query.Where(r => r.RequisitionType == requisitionType);
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
            .Include(r => r.Approvals).Include(requisition => requisition.ProductionActivityStep)
            .Include(requisition => requisition.RequestedBy).ThenInclude(r => r.Department)
            .ThenInclude(d => d.Warehouses).ThenInclude(w => w.Warehouse).Include(requisition => requisition.Items)
            .AsSplitQuery()
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
            requisition.ProductionActivityStep.Status = ProductionStatus.Completed;
            context.ProductionActivitySteps.Update(requisition.ProductionActivityStep);

            var warehouse =
                requisition.RequestedBy.Department?.Warehouses.FirstOrDefault(w =>
                    w.Warehouse.Type == WarehouseType.Production)?.Warehouse;

            if (warehouse is not null)
            {
                foreach (var item in requisition.Items)
                {
                    var frozenMaterialsResult = await materialRepository.GetFrozenMaterialBatchesInWarehouse(item.MaterialId, warehouse.Id);
                    if (frozenMaterialsResult.IsFailure) continue;

                    var frozenMaterial = frozenMaterialsResult.Value;
                    foreach (var materialBatch in frozenMaterial)
                    {
                        await materialRepository.ConsumeMaterialAtLocation(materialBatch.Id, warehouse.Id, item.Quantity, userId);
                    }
                }
            }
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
                decimal consumedFromBatch;

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
    public async Task<Result> CreateSourceRequisition(CreateSourceRequisitionRequest request, Guid userId)
    {
        var requisition = await context.Requisitions.FirstOrDefaultAsync(r => r.Id == request.RequisitionId);
        if (requisition is null) return RequisitionErrors.NotFound(request.RequisitionId);
        
        var supplierGroupedItems = request.Items
            .SelectMany(item => item.Suppliers.Select(supplier => new { item, supplier }))
            .GroupBy(x => x.supplier.SupplierId);

        // Process each supplier group
        foreach (var supplierGroup in supplierGroupedItems)
        {
            var supplierId = supplierGroup.Key;
            
            var existingSourceRequisition = await context.SourceRequisitions
                .Include(sourceRequisition => sourceRequisition.Items).FirstOrDefaultAsync(sr => sr.SupplierId == supplierId && !sr.SentQuotationRequestAt.HasValue);
            if (existingSourceRequisition is not null)
            {
                foreach (var groupItem in supplierGroup)
                {
                    var existingItem = existingSourceRequisition.Items
                        .FirstOrDefault(i => i.MaterialId == groupItem.item.MaterialId && i.UoMId == groupItem.item.UoMId);

                    if (existingItem != null)
                    {
                        existingItem.Quantity += groupItem.item.Quantity;
                    }
                    else
                    {
                        existingSourceRequisition.Items.Add(new SourceRequisitionItem
                        {
                            MaterialId = groupItem.item.MaterialId,
                            UoMId = groupItem.item.UoMId,
                            Quantity = groupItem.item.Quantity,
                            Source = groupItem.item.Source
                        });
                    }
                    
                }
                existingSourceRequisition.RequisitionIds.Add(request.RequisitionId);
                context.SourceRequisitions.Update(existingSourceRequisition);
            }
            else
            {
                // Create a SourceRequisition instance per supplier
                var requisitionForSupplier = new SourceRequisition
                {
                    Code = request.Code,
                    RequisitionId = request.RequisitionId,
                    SupplierId = supplierId,
                    SentQuotationRequestAt = null, 
                    Items = supplierGroup.Select(x => new SourceRequisitionItem
                    {
                        MaterialId = x.item.MaterialId,
                        UoMId = x.item.UoMId,
                        Quantity = x.item.Quantity,
                        Source = x.item.Source
                    }).ToList(),
                    RequisitionIds = [request.RequisitionId]
                };
                // Add to the main requisition's items
                await context.SourceRequisitions.AddAsync(requisitionForSupplier);
            }
        }
        
        requisition.Status = RequestStatus.Sourced;
        context.Requisitions.Update(requisition);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    // Get Source Requisition by ID
    public async Task<Result<SourceRequisitionDto>> GetSourceRequisition(Guid sourceRequisitionId)
    {
        var sourceRequisition = await context.SourceRequisitions
            .Include(sr => sr.Requisition)
            .Include(sr => sr.Supplier)
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
            .Include(sr => sr.Supplier)
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
    
    public async Task<Result<Paginateable<IEnumerable<SupplierQuotationRequest>>>> GetSuppliersWithSourceRequisitionItems(int page, int pageSize, ProcurementSource source, bool sent)
    {
        // Base query
        var query = context.SourceRequisitions
            .Include(sr => sr.Requisition)
            .Include(sr => sr.Supplier)
            .Include(sr => sr.Items).ThenInclude(item => item.Material)
            .Include(sr => sr.Items).ThenInclude(item => item.UoM)
            .AsQueryable();

        query = sent 
            ?  query.Where(s => s.SentQuotationRequestAt != null) 
            : query.Where(s => s.SentQuotationRequestAt == null);

        return await PaginationHelper.GetPaginatedResultAsync(
            query,
            page,
            pageSize,
            mapper.Map<SupplierQuotationRequest>
        );
    }
    
    public async Task<Result<SupplierQuotationRequest>> GetSuppliersWithSourceRequisitionItems(Guid supplierId)
    {
        // Base query
        var query = await context.SourceRequisitions
            .Include(sr => sr.Supplier)
            .Include(sr => sr.Items).ThenInclude(item => item.Material)
            .Include(sr => sr.Items).ThenInclude(item => item.UoM)
            .FirstOrDefaultAsync(sr => sr.SupplierId == supplierId && !sr.SentQuotationRequestAt.HasValue);
        
        return mapper.Map<SupplierQuotationRequest>(query);
    }

    
    public async Task<Result> SendQuotationToSupplier(Guid supplierId)
    {
        var sourceRequisition = await context.SourceRequisitions
            .Include(sr => sr.Requisition)
            .Include(sr => sr.Supplier)
            .Include(sr => sr.Items).ThenInclude(item => item.Material)
            .Include(sr => sr.Items).ThenInclude(item => item.UoM)
            .FirstOrDefaultAsync(s => s.SupplierId == supplierId && !s.SentQuotationRequestAt.HasValue);
        
        if (sourceRequisition is null)
        {
            return Error.Validation("Supplier.Quotation", "No items found to mark as quotation sent for the specified supplier.");
        }
        
        var supplierQuotationDto = mapper.Map<SupplierQuotationRequest>(sourceRequisition);
        
        if (supplierQuotationDto.Items.Count == 0)
        {
            return Error.Validation("Supplier.Quotation", "No items found to mark as quotation sent for the specified supplier.");
        }
        
        var mailAttachments = new List<(byte[] fileContent, string fileName, string fileType)>();
        var fileContent = pdfService.GeneratePdfFromHtml(PdfTemplate.QuotationRequestTemplate(supplierQuotationDto));
        mailAttachments.Add((fileContent, $"Quotation Request from Entrance",  "application/pdf"));

        try
        {
            emailService.SendMail(supplierQuotationDto.Supplier.Email, "Sales Quote From Entrance", "Please find attached to this email a sales quote from us.", mailAttachments);
        }
        catch (Exception e)
        {
            return Error.Validation("Supplier.Quotation", e.Message);
        }
        
        sourceRequisition.SentQuotationRequestAt = DateTime.UtcNow;

        var supplierQuotation = new SupplierQuotation
        {
            SupplierId = sourceRequisition.SupplierId,
            SourceRequisitionId = sourceRequisition.Id,
            Items = sourceRequisition.Items.Select(i => new SupplierQuotationItem
            {
                MaterialId = i.MaterialId,
                UoMId = i.UoMId,
                Quantity = i.Quantity
            }).ToList()
        };

        context.SourceRequisitions.UpdateRange(sourceRequisition);
        await context.SupplierQuotations.AddAsync(supplierQuotation);
        // Save changes to the database
        await context.SaveChangesAsync();
        return Result.Success();
    }
    
    public async Task<Result<Paginateable<IEnumerable<SupplierQuotationDto>>>> GetSupplierQuotations(int page, int pageSize, SupplierType supplierType, bool received)
    {

        var query =  context.SupplierQuotations
            .Include(s => s.Items).ThenInclude(s => s.Material)
            .Include(s => s.Items).ThenInclude(s => s.UoM)
            .Include(s => s.Supplier)
            .Where(s => s.Supplier.Type == supplierType)
            .AsQueryable();

        var supplierQuotations = received
            ?  query.Where(s => s.ReceivedQuotation)
            :  query.Where(s => !s.ReceivedQuotation);

        return await PaginationHelper.GetPaginatedResultAsync(
            supplierQuotations,
            page,
            pageSize,
            mapper.Map<SupplierQuotationDto>);
    }
    
    public async Task<Result<SupplierQuotationDto>> GetSupplierQuotation(Guid supplierQuotationId)
    {
        return mapper.Map<SupplierQuotationDto>(await  context.SupplierQuotations
            .Include(s => s.Items).ThenInclude(s => s.Material)
            .Include(s => s.Items).ThenInclude(s => s.UoM)
            .Include(s => s.Supplier)
            .FirstOrDefaultAsync(s => s.Id == supplierQuotationId));
    }
    
    public async Task<Result> ReceiveQuotationFromSupplier(List<SupplierQuotationResponseDto> supplierQuotationResponse, Guid supplierQuotationId)
    {
        var supplierQuotation = await context.SupplierQuotations
            .Include(s => s.Items).ThenInclude(s => s.Material)
            .Include(s => s.Items).ThenInclude(s => s.UoM)
            .Include(s => s.Supplier)
            .FirstOrDefaultAsync(s => s.Id == supplierQuotationId);

        if (supplierQuotation.Items.Count == 0)
        {
            return Error.Validation("Supplier.Quotation", "No items found to mark as quotation sent for the specified supplier.");
        }

        foreach (var item in supplierQuotation.Items)
        {
            item.QuotedPrice = supplierQuotationResponse.FirstOrDefault(s => s.Id == item.Id)?.Price;
        }
        
        supplierQuotation.ReceivedQuotation = true;
        context.SupplierQuotations.Update(supplierQuotation);
        context.SupplierQuotationItems.UpdateRange(supplierQuotation.Items);
        // Save changes to the database
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result<List<SupplierPriceComparison>>> GetPriceComparisonOfMaterial(SupplierType supplierType)
    {
        var sourceRequisitionItemSuppliers = await context.SupplierQuotationItems
            .Include(s => s.Material)
            .Include(s => s.UoM)
            .Include(s => s.SupplierQuotation).ThenInclude(s => s.Supplier)
            .Include(s => s.SupplierQuotation).ThenInclude(s => s.SourceRequisition)
            .Where(s => s.QuotedPrice != null && !s.SupplierQuotation.Processed && s.SupplierQuotation.Supplier.Type == supplierType)
            .ToListAsync();

        return sourceRequisitionItemSuppliers
            .GroupBy(s => new { s.Material, s.UoM })
            .Select(item => new SupplierPriceComparison
            {
                Material = mapper.Map<CollectionItemDto>(item.Key.Material),
                UoM = mapper.Map<UnitOfMeasureDto>(item.Key.UoM),
                Quantity = item.Select(s => s.Quantity).First(),
                SupplierQuotation = item
                    .GroupBy(s => s.SupplierQuotation.SupplierId)
                    .Select(sg => sg.OrderByDescending(s => s.SupplierQuotation.CreatedAt).First())
                    .Select(s => new SupplierPrice
                    {
                        Supplier = mapper.Map<CollectionItemDto>(s.SupplierQuotation.Supplier),
                        SourceRequisition = mapper.Map<CollectionItemDto>(s.SupplierQuotation.SourceRequisition),
                        Price = s.QuotedPrice
                    }).ToList()
            }).ToList();
    }

    public async Task<Result> ProcessQuotationAndCreatePurchaseOrder(List<ProcessQuotation> processQuotations, Guid userId)
    {
        var supplierQuotations =  await context.SupplierQuotations
            .Include(s => s.Items).ThenInclude(i => i.Material)
            .Include(s => s.Items).ThenInclude(i => i.UoM)
            .Include(s => s.Supplier)
            .Where(s => s.ReceivedQuotation && !s.Processed).ToListAsync();

      
        foreach (var quotation in processQuotations)
        {
            await procurementRepository.CreatePurchaseOrder(new CreatePurchaseOrderRequest
            {
                Code = await GeneratePurchaseOrderCode(),
                SupplierId = quotation.SupplierId,
                SourceRequisitionId = quotation.SourceRequisitionId,
                RequestDate = DateTime.UtcNow,
                Items = quotation.Items
            }, userId);
        }

        foreach (var supplierQuotation in supplierQuotations)
        {
            supplierQuotation.Processed = true;
        }
        
        context.SupplierQuotations.UpdateRange(supplierQuotations);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    private async Task<string> GeneratePurchaseOrderCode()
    {
        // Fetch the configuration for PurchaseOrder
        var config = await context.Configurations
            .FirstOrDefaultAsync(c => c.ModelType == nameof(PurchaseOrder));
        if(config is null) return $"PO-{Guid.NewGuid()}";

        var seriesCount =
            await configurationRepository.GetCountForCodeConfiguration(nameof(PurchaseOrder), config.Prefix);
        return seriesCount.IsFailure ? $"PO-{Guid.NewGuid()}" : CodeGenerator.GenerateCode(config, seriesCount.Value);
    }
}
