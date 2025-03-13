using APP.Extensions;
using APP.IRepository;
using APP.Services.Email;
using APP.Services.Pdf;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.BinCards;
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
using DOMAIN.Entities.Users;
using DOMAIN.Entities.Warehouses;

namespace APP.Repository;

public class RequisitionRepository(ApplicationDbContext context, IMapper mapper, IProcurementRepository procurementRepository, 
    IEmailService emailService, IPdfService pdfService, IConfigurationRepository configurationRepository, IMaterialRepository materialRepository) : IRequisitionRepository
{
    // ************* CRUD for Requisitions *************

    // Create Stock Requisition
    public async Task<Result> CreateRequisition(CreateRequisitionRequest request, Guid userId)
    {

        var existingRequisition = await context.Requisitions.Include(requisition => requisition.Items).FirstOrDefaultAsync(r =>
            r.ProductionScheduleId == request.ProductionScheduleId && r.ProductId == request.ProductId &&
            r.RequisitionType == request.RequisitionType);

        if (existingRequisition is { RequisitionType: RequisitionType.Stock })
            return Error.Validation("Requisition.Validation",
                $"A {request.RequisitionType.ToString()} requisition for this production schedule and product has already been created");

        if (existingRequisition != null &&
            existingRequisition.Items.Any(r => request.Items.Select(i => i.MaterialId).Contains(r.MaterialId)))
        {
            return Error.Validation("Requisition.Validation",
                $"A {request.RequisitionType.ToString()} requisition for this production schedule and product with at least one of the materials has already been created");
        }

        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null)
            return UserErrors.NotFound(userId);

        if (!user.DepartmentId.HasValue)
            return UserErrors.DepartmentNotFound;
        
        if (request.RequisitionType == RequisitionType.Stock)
        {
            // Fetch materials to determine their kind (Raw or Package)
            var materialIds = request.Items.Select(i => i.MaterialId).ToList();
            var materials = await context.Materials
                .Where(m => materialIds.Contains(m.Id))
                .Select(m => new { m.Id, m.Kind })
                .ToListAsync();

            // Separate items into Raw and Package
            var rawItems = request.Items.Where(i => materials.Any(m => m.Id == i.MaterialId && m.Kind == MaterialKind.Raw)).ToList();
            var packageItems = request.Items.Where(i => materials.Any(m => m.Id == i.MaterialId && m.Kind == MaterialKind.Package)).ToList();

            // Create Raw Material Requisition
            await CreateStockRequisition("raw", rawItems);

            // Create Package Material Requisition
             await CreateStockRequisition("package", packageItems);

            async Task CreateStockRequisition(string suffix, List<CreateRequisitionItemRequest> items)
            {
                if (items.Count == 0) return; // Skip if no items

                var requisition = mapper.Map<Requisition>(request);
                requisition.Code = $"{request.Code}-{suffix}";
                requisition.RequestedById = userId;
                requisition.DepartmentId = user.DepartmentId.Value;
                requisition.Items = mapper.Map<List<RequisitionItem>>(items);

                await context.Requisitions.AddAsync(requisition);
            }

            var productionActivityStep =
                await context.ProductionActivitySteps.FirstOrDefaultAsync(p =>
                    p.Id == request.ProductionActivityStepId);

            if (productionActivityStep is not null)
            {
                productionActivityStep.StartedAt = DateTime.UtcNow;
                productionActivityStep.Status = ProductionStatus.InProgress;
                context.ProductionActivitySteps.Update(productionActivityStep);
            }
        }
        else
        {
            var requisition = mapper.Map<Requisition>(request);
            requisition.RequestedById = userId;
            requisition.DepartmentId = user.DepartmentId.Value; 
            await context.Requisitions.AddAsync(requisition);

            if (request.ProductionActivityStepId.HasValue)
            {
                var activityStep =
                    await context.ProductionActivitySteps.FirstOrDefaultAsync(p => p.Id == request.ProductionActivityStepId);

                if (activityStep is not null)
                {
                    activityStep.Status = ProductionStatus.InProgress;
                    activityStep.StartedAt = DateTime.UtcNow;
                    context.ProductionActivitySteps.Update(activityStep);
                }
            }
        }
        
        await context.SaveChangesAsync();
        return Result.Success();
    }

    // Get Stock Requisition by ID
    public async Task<Result<RequisitionDto>> GetRequisition(Guid requisitionId, Guid userId)
    {
        var requisition = await context.Requisitions
            .AsSplitQuery()
            .Include(r => r.ProductionSchedule)
            .Include(r => r.Product)
            .Include(r => r.RequestedBy)
            .Include(r => r.Approvals).ThenInclude(r => r.User)
            .Include(r => r.Approvals).ThenInclude(r => r.Role)
            .Include(r => r.Items).ThenInclude(i => i.Material)
            .FirstOrDefaultAsync(r => r.Id == requisitionId);

        if (requisition is null)
        {
            return RequisitionErrors.NotFound(requisitionId);
        }

        var user = await context.Users
            .Include(u => u.Department).ThenInclude(u => u.Warehouses)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user?.Department is null)
        {
            return UserErrors.NotFound(userId);
        }

        // Find user's raw material & packing warehouses
        var rawWarehouse = user.Department.Warehouses.FirstOrDefault(i => i.Type == WarehouseType.RawMaterialStorage);
        if (rawWarehouse is null)
            return Error.NotFound("User.Warehouse", "No raw material warehouse is associated with current user");

        var packingWarehouse = user.Department.Warehouses.FirstOrDefault(i => i.Type == WarehouseType.PackagedStorage);
        if (packingWarehouse is null)
            return Error.NotFound("User.Warehouse", "No packing material warehouse is associated with current user");

        var result = mapper.Map<RequisitionDto>(requisition);

        // If the requisition type is a purchase, return early
        if (requisition.RequisitionType == RequisitionType.Purchase) return result;

        foreach (var item in result.Items)
        {
            // Determine appropriate warehouse based on material type
            var appropriateWarehouse = item.Material.Kind == MaterialKind.Raw ? rawWarehouse : packingWarehouse;

            // Fetch frozen batches that will fulfill the request
            var batchResult = await materialRepository.BatchesToSupplyForGivenQuantity(item.Material.Id, appropriateWarehouse.Id, item.Quantity);
            
            if (batchResult.IsSuccess)
            {
                item.Batches = batchResult.Value; // Assign batches with quantity to take
            }
        }

        return result;
    }


    public async Task<Result> ApproveStockRequisition(Guid stockRequisitionId)
    {
        var stockRequisition = await context.Requisitions.FirstOrDefaultAsync(r => r.Id == stockRequisitionId);

        if (stockRequisition is null)
            return RequisitionErrors.NotFound(stockRequisitionId);

        return Result.Success();
    }
    
    public async Task<Result> IssueStockRequisition(Guid stockRequisitionId, Guid userId)
    {
        var stockRequisition = await context.Requisitions
            .AsSplitQuery()
            .Include(r => r.Items).ThenInclude(requisitionItem => requisitionItem.Material)
            .FirstOrDefaultAsync(r => r.Id == stockRequisitionId);
        
        if (stockRequisition is null)
            return RequisitionErrors.NotFound(stockRequisitionId);
        
        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null)
            return UserErrors.NotFound(userId);
        
        if (user.Department == null)
            return Error.NotFound("User.Department", "User has no association to any department");
        
        if (user.Department.Warehouses.Count == 0)
            return Error.NotFound("User.Warehouse", "No raw material warehouse is associated with current user");
        
        var rawWarehouse = user.Department.Warehouses.FirstOrDefault(i => i.Type == WarehouseType.RawMaterialStorage);
        if (rawWarehouse is null)
            return Error.NotFound("User.Warehouse", "No raw material warehouse is associated with current user");
        
        var packingWarehouse = user.Department.Warehouses.FirstOrDefault(i => i.Type == WarehouseType.PackagedStorage);
        if (packingWarehouse is null)
            return Error.NotFound("User.Warehouse", "No packing material warehouse is associated with current user");

        var productionWarehouse = await context.Warehouses.IgnoreQueryFilters().FirstOrDefaultAsync(w =>
            w.DepartmentId == stockRequisition.DepartmentId && w.Type == WarehouseType.Production);
        
        if (productionWarehouse is null)
            return Error.NotFound("User.Warehouse", "No production warehouse is associated with department who made stock requisition");


        foreach (var item in stockRequisition.Items)
        {
            var appropriateWarehouse = item.Material.Kind == MaterialKind.Raw ? rawWarehouse : packingWarehouse;

            var batchesToConsume =
                await materialRepository.GetReservedBatchesAndQuantityForProductionWarehouse(item.MaterialId,
                    productionWarehouse.Id, stockRequisition.ProductionScheduleId, stockRequisition.ProductId);
            
            foreach (var batch in batchesToConsume)
            {
                var materialBatch = await context.MaterialBatches.FirstOrDefaultAsync(m => m.Id == batch.MaterialBatchId);
                if (materialBatch is null) continue;
                
                materialBatch.QuantityAssigned = 0;
                context.MaterialBatches.Update(materialBatch);

                var shelfMaterialBatches = await context.ShelfMaterialBatches
                    .Where(sb => sb.MaterialBatchId == batch.MaterialBatchId)
                    .ToListAsync();
                context.ShelfMaterialBatches.RemoveRange(shelfMaterialBatches);

                var movement = new MassMaterialBatchMovement
                {
                    BatchId = batch.MaterialBatchId,
                    FromWarehouseId = appropriateWarehouse.Id,
                    ToWarehouseId = productionWarehouse.Id,
                    Quantity = batch.Quantity,
                    MovedAt = DateTime.UtcNow,
                    MovedById = userId
                };

                await context.MassMaterialBatchMovements.AddAsync(movement);

                var batchEvent = new MaterialBatchEvent
                {
                    BatchId =  batch.MaterialBatchId,
                    Type = EventType.Moved,
                    Quantity =batch.Quantity,
                    UserId = userId
                };

                await context.MaterialBatchEvents.AddAsync(batchEvent);
            }
        }

        // ✅ Mark the current stock requisition as completed
        stockRequisition.Status = RequestStatus.Completed;

        await context.SaveChangesAsync();

        // ✅ Check if all requisitions for the same `ProductionActivityStepId` are completed
        if (stockRequisition.ProductionActivityStepId.HasValue)
        {
            var relatedRequisitions = await context.Requisitions
                .Where(r => r.ProductionActivityStepId == stockRequisition.ProductionActivityStepId)
                .ToListAsync();

            bool allCompleted = relatedRequisitions.All(r => r.Status == RequestStatus.Completed);

            if (allCompleted)
            {
                var productionActivityStep = await context.ProductionActivitySteps
                    .FirstOrDefaultAsync(p => p.Id == stockRequisition.ProductionActivityStepId);

                if (productionActivityStep is not null)
                {
                    productionActivityStep.Status = ProductionStatus.Completed;
                    productionActivityStep.CompletedAt = DateTime.UtcNow;
                    context.ProductionActivitySteps.Update(productionActivityStep);
                    await context.SaveChangesAsync();
                }
            }
        }

        return Result.Success();
    }

   private async Task<List<ShelfMaterialBatchDto>> GetShelvesOfBatch(Guid batchId, Guid warehouseId)
   {
       var shelves = await context.ShelfMaterialBatches
           .Include(smb => smb.WarehouseLocationShelf)
           .Where(smb =>
               smb.MaterialBatchId == batchId &&
               smb.WarehouseLocationShelf.WarehouseLocationRack.WarehouseLocation.Warehouse.Id == warehouseId)
           .ToListAsync();

        return mapper.Map<List<ShelfMaterialBatchDto>>(shelves);
    }
   
    public async Task<Result> IssueStockRequisitionVoucher(List<BatchQuantityDto> batchQuantities, Guid productId, Guid userId)
    {
        decimal quantityIssued = 0;
        var product = await context.Products.FirstOrDefaultAsync(p => p.Id == productId);
        foreach (var batch in batchQuantities)
        {
            var shelfMaterialBatch = await context.ShelfMaterialBatches
                .Include(smb => smb.WarehouseLocationShelf)
                .ThenInclude(wls => wls.WarehouseLocationRack)
                .ThenInclude(wlr => wlr.WarehouseLocation)
                .ThenInclude(wl => wl.Warehouse)
                .Include(shelfMaterialBatch => shelfMaterialBatch.MaterialBatch)
                .FirstOrDefaultAsync(smb => smb.Id == batch.ShelfMaterialBatchId);

            if (shelfMaterialBatch == null)
            {
                return Error.Validation("ShelfMaterialBatch.NotFound", $"ShelfMaterialBatch with ID {batch.ShelfMaterialBatchId} not found.");
            }

            if (shelfMaterialBatch.Quantity < batch.Quantity)
            {
                return Error.Validation("ShelfMaterialBatch.InsufficientQuantity", $"Insufficient quantity in ShelfMaterialBatch with ID {batch.ShelfMaterialBatchId}.");
            }

            var productionWarehouse = await context.Warehouses
                .Where(dw => dw.Id == shelfMaterialBatch.WarehouseLocationShelf.WarehouseLocationRack
                    .WarehouseLocation.Warehouse.Id)
                .Include(warehouse => warehouse.ArrivalLocation)
                .FirstOrDefaultAsync(w => w.Type == WarehouseType.Production);

            if (productionWarehouse == null)
            {
                return Error.Validation("ProductionWarehouse.NotFound", "Production warehouse not found for the department.");
            }

            // Update the quantity in the shelf
            shelfMaterialBatch.Quantity -= batch.Quantity;

            if (shelfMaterialBatch.Quantity == 0)
            {
                context.ShelfMaterialBatches.Remove(shelfMaterialBatch);
            }
            else
            {
                context.ShelfMaterialBatches.Update(shelfMaterialBatch);
            }

            // Log the transfer event
            var materialBatchEvent = new MaterialBatchEvent
            {
                BatchId = shelfMaterialBatch.MaterialBatchId,
                Quantity = batch.Quantity,
                Type = EventType.Moved,
                UserId = userId
            };

            await context.MaterialBatchEvents.AddAsync(materialBatchEvent);

            var batchMovement = new MassMaterialBatchMovement
            {
                BatchId = shelfMaterialBatch.MaterialBatchId,
                FromWarehouseId = shelfMaterialBatch.WarehouseLocationShelf.WarehouseLocationRack.WarehouseLocation.Warehouse.Id,
                ToWarehouseId = productionWarehouse.Id,
                Quantity = batch.Quantity,
                CreatedById = userId
            };

            await context.MassMaterialBatchMovements.AddAsync(batchMovement);

            quantityIssued += batch.Quantity;

            var toBinCardEvent = new BinCardInformation
            {
                MaterialBatchId = shelfMaterialBatch.MaterialBatch.Id,
                Description = shelfMaterialBatch.WarehouseLocationShelf.WarehouseLocationRack.WarehouseLocation.Warehouse.Name,
                WayBill = "N/A",
                ArNumber = "N/A",
                QuantityReceived = 0,
                QuantityIssued = batch.Quantity,
                BalanceQuantity = (await materialRepository.GetMaterialStockInWarehouse(shelfMaterialBatch.MaterialBatch.MaterialId, shelfMaterialBatch.WarehouseLocationShelf.WarehouseLocationRack.WarehouseLocation.Warehouse.Id)).Value - quantityIssued,
                UoMId = shelfMaterialBatch.MaterialBatch.UoMId,
                ProductId = product.Id,
                CreatedAt = DateTime.UtcNow,
                CreatedById = userId
            };

            await context.BinCardInformation.AddAsync(toBinCardEvent);

            var fromBinCardEvent = new BinCardInformation
            {
                MaterialBatchId = shelfMaterialBatch.MaterialBatch.Id,
                Description = productionWarehouse.Name,
                WayBill = "N/A",
                ArNumber = "N/A",
                QuantityReceived = batch.Quantity,
                QuantityIssued = 0,
                BalanceQuantity = (await materialRepository.GetMaterialStockInWarehouse(shelfMaterialBatch.MaterialBatch.MaterialId, productionWarehouse.Id)).Value + batch.Quantity,
                UoMId = shelfMaterialBatch.MaterialBatch.UoMId,
                ProductId = product.Id,
                CreatedAt = DateTime.UtcNow,
                CreatedById = userId
            };

            await context.BinCardInformation.AddAsync(fromBinCardEvent);
        }

        await context.SaveChangesAsync();
        return Result.Success();
    }

    // Get paginated list of Stock Requisitions
    public async Task<Result<Paginateable<IEnumerable<RequisitionDto>>>> GetRequisitions(int page, int pageSize, string searchQuery, RequestStatus? status, RequisitionType? requisitionType, Guid? departmentId)
    {
        var query = context.Requisitions
            .AsSplitQuery()
            .Include(r => r.RequestedBy)
            .Include(r => r.Approvals).ThenInclude(r => r.User)
            .Include(r => r.Approvals).ThenInclude(r => r.Role)
            .Include(r => r.Items)
            .ThenInclude(i => i.Material)
            .AsQueryable();

        if (departmentId.HasValue)
        {
            query = query.Where(q => q.DepartmentId == departmentId);
        }
        
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
            .ThenInclude(d => d.Warehouses).Include(requisition => requisition.Items)
            .AsSplitQuery()
            .FirstOrDefaultAsync(r => r.Id == requisitionId);

        if (requisition == null)
        {
            return RequisitionErrors.NotFound(requisitionId);
        }

        if (!requisition.ProductionActivityStepId.HasValue || requisition.RequisitionType == RequisitionType.Purchase)
        {
            return Error.Validation("Requisition.Approve", "You cant approve a purchase requisition");
        }
        
        requisition.Approved = true;
        requisition.ProductionActivityStep.Status = ProductionStatus.Completed;
        requisition.ProductionActivityStep.CompletedAt = DateTime.UtcNow;
        context.ProductionActivitySteps.Update(requisition.ProductionActivityStep);

        /*var warehouse =
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
        }*/
        
        await context.SaveChangesAsync();
        return Result.Success();
    }

    // ************* Process Stock Requisition *************

    // Consume stock once the requisition is fully approved

    /*public async Task<Result> ProcessRequisition(CreateRequisitionRequest request, Guid requisitionId, Guid userId)
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
    }*/
    
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
                    // var existingItem = existingSourceRequisition.Items
                    //     .FirstOrDefault(i => i.MaterialId == groupItem.item.MaterialId && i.UoMId == groupItem.item.UoMId);

                    existingSourceRequisition.Items.Add(new SourceRequisitionItem
                    {
                        MaterialId = groupItem.item.MaterialId,
                        UoMId = groupItem.item.UoMId,
                        Quantity = groupItem.item.Quantity,
                        Source = groupItem.item.Source,
                        RequisitionId = request.RequisitionId
                    });
                    
                }
                context.SourceRequisitions.Update(existingSourceRequisition);
            }
            else
            {
                // Create a SourceRequisition instance per supplier
                var requisitionForSupplier = new SourceRequisition
                {
                    Code = request.Code,
                    SupplierId = supplierId,
                    SentQuotationRequestAt = null, 
                    Items = supplierGroup.Select(x => new SourceRequisitionItem
                    {
                        MaterialId = x.item.MaterialId,
                        UoMId = x.item.UoMId,
                        Quantity = x.item.Quantity,
                        Source = x.item.Source,
                        RequisitionId = request.RequisitionId
                    }).ToList(),
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
    
    public async Task<Result<Paginateable<IEnumerable<SupplierQuotationRequest>>>> GetSuppliersWithSourceRequisitionItems(int page, int pageSize, SupplierType source, bool sent)
    {
        // Base query
        var query = context.SourceRequisitions
            .Include(sr => sr.Supplier)
            .Include(sr => sr.Items).ThenInclude(item => item.Material)
            .Include(sr => sr.Items).ThenInclude(item => item.UoM)
            .Where(sr => sr.Supplier.Type == source)
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
            .Include(sr => sr.Supplier)
            .Include(sr => sr.Items).ThenInclude(item => item.Material)
            .Include(sr => sr.Items).ThenInclude(item => item.UoM)
            .FirstOrDefaultAsync(s => s.SupplierId == supplierId && !s.SentQuotationRequestAt.HasValue);
        
        if (sourceRequisition is null)
        {
            return Error.Validation("Supplier.Quotation", "No items found to mark as quotation sent for the specified supplier.");
        }
        
        var supplierQuotationDto = mapper.Map<SupplierQuotationRequest>(sourceRequisition);

        var sourceRequisitionDto = mapper.Map<SourceRequisitionDto>(sourceRequisition);
        
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
            Items = sourceRequisitionDto.Items.Select(i => new SupplierQuotationItem
            {
                MaterialId = i.Material.Id,
                UoMId = i.UoM.Id,
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
            .Where(s => s.ReceivedQuotation && !s.Processed).ToListAsync();

      
        foreach (var quotation in processQuotations)
        {
            await procurementRepository.CreatePurchaseOrder(new CreatePurchaseOrderRequest
            {
                Code = await GeneratePurchaseOrderCode(),
                SupplierId = quotation.SupplierId,
                SourceRequisitionId = quotation.SourceRequisitionId ?? supplierQuotations.First(s => s.SupplierId == quotation.SupplierId).SourceRequisitionId,
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
        if(config is null) throw new Exception("No configuration exists");

        var seriesCount =
            await configurationRepository.GetCountForCodeConfiguration(nameof(PurchaseOrder), config.Prefix);
        return seriesCount.IsFailure ? throw new Exception("No configuration exists") : CodeGenerator.GenerateCode(config, seriesCount.Value);
    }
}
