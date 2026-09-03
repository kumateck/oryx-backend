using APP.Extensions;
using APP.IRepository;
using APP.Services.Background;
using APP.Services.Email;
using APP.Services.Pdf;
using APP.Services.ProductionActivityStepEventPublisher;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.BinCards;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.Notifications;
using DOMAIN.Entities.Procurement.Manufacturers;
using DOMAIN.Entities.Procurement.Suppliers;
using DOMAIN.Entities.Products;
using DOMAIN.Entities.Products.Production;
using DOMAIN.Entities.PurchaseOrders;
using DOMAIN.Entities.PurchaseOrders.Request;
using DOMAIN.Entities.Requisitions;
using DOMAIN.Entities.Requisitions.Request;
using DOMAIN.Entities.Users;
using DOMAIN.Entities.Warehouses;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public class RequisitionRepository(
    ApplicationDbContext context,
    IMapper mapper,
    IProcurementRepository procurementRepository,
    IEmailService emailService,
    IPdfService pdfService,
    IConfigurationRepository configurationRepository,
    IMaterialRepository materialRepository,
    IApprovalRepository approvalRepository,
    IBackgroundWorkerService backgroundWorkerService,
    IProductionActivityStepEventPublisher stepEventPublisher
) : IRequisitionRepository
{
    // ************* CRUD for Requisitions *************

    // Create Stock Requisition
    public async Task<Result> CreateRequisition(CreateRequisitionRequest request, Guid userId)
    {
        if (request.ProductionScheduleProductId.HasValue)
        {
            var existingRequisition = await context
                .Requisitions.AsSplitQuery()
                .Include(requisition => requisition.Items)
                .FirstOrDefaultAsync(r =>
                    r.ProductionScheduleProductId == request.ProductionScheduleProductId
                    && r.RequisitionType == request.RequisitionType
                );

            if (existingRequisition is { RequisitionType: RequisitionType.Stock })
                return Error.Validation(
                    "Requisition.Validation",
                    $"A {request.RequisitionType.ToString()} requisition for "
                        + $"this production schedule and product has already been created"
                );

            if (
                existingRequisition != null
                && existingRequisition.Items.Any(r =>
                    request.Items.Select(i => i.MaterialId).Contains(r.MaterialId)
                )
            )
            {
                return Error.Validation(
                    "Requisition.Validation",
                    $"A {request.RequisitionType.ToString()} requisition for this"
                        + $" production schedule and product with at least one of the materials has already been created"
                );
            }
        }

        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null)
            return UserErrors.NotFound(userId);

        if (!user.DepartmentId.HasValue)
            return UserErrors.DepartmentNotFound;

        var department = await context.Departments.FirstOrDefaultAsync(d =>
            d.Id == user.DepartmentId.Value
        );
        if (department is null)
            return UserErrors.DepartmentNotFound;

        if (request.RequisitionType == RequisitionType.Stock)
        {
            if (!request.ProductionScheduleProductId.HasValue)
                return Error.Validation(
                    "Stock.Requisition",
                    "Production schedule product cannot be null when creating stock requisitions"
                );

            if (!request.ProductionActivityStepId.HasValue)
                return Error.Validation(
                    "Stock.Requisition",
                    "Production activity step cannot be null when creating stock requisitions"
                );

            // Fetch materials to determine their kind (Raw or Package)
            var materialIds = request.Items.Select(i => i.MaterialId).ToList();
            var materials = await context
                .Materials.Where(m => materialIds.Contains(m.Id))
                .Select(m => new { m.Id, m.Kind })
                .ToListAsync();

            // Separate items into Raw and Package
            var rawItems = request
                .Items.Where(i =>
                    materials.Any(m => m.Id == i.MaterialId && m.Kind == MaterialKind.Raw)
                )
                .ToList();
            var packageItems = request
                .Items.Where(i =>
                    materials.Any(m => m.Id == i.MaterialId && m.Kind == MaterialKind.Package)
                )
                .ToList();

            // Create Raw Material Requisition
            var rawStockRequisitionId = await CreateStockRequisition("RM", rawItems);
            if (rawStockRequisitionId.HasValue)
            {
                await approvalRepository.CreateInitialApprovalsAsync(
                    "RawStockRequisition",
                    rawStockRequisitionId.Value
                );
            }

            // Create Package Material Requisition
            var packageStockRequisitionId = await CreateStockRequisition("PM", packageItems);
            if (packageStockRequisitionId.HasValue)
            {
                await approvalRepository.CreateInitialApprovalsAsync(
                    "PackageStockRequisition",
                    packageStockRequisitionId.Value
                );
            }

            async Task<Guid?> CreateStockRequisition(
                string prefix,
                List<CreateRequisitionItemRequest> items
            )
            {
                if (items == null || items.Count == 0)
                    return null;

                // Use a transaction to prevent two users from generating the same number simultaneously
                await using var transaction = await context.Database.BeginTransactionAsync();
                try
                {
                    var beta = department.Division == Division.BetaLactam ? "B" : "N";
                    var year = DateTime.UtcNow.ToString("yy");
                    var searchPattern = $"{prefix}/{beta}/{year}/";

                    // 1. Find the highest existing sequence number for this specific prefix/beta/year
                    var lastCode = await context
                        .Requisitions.IgnoreQueryFilters()
                        .Where(c => c.Code.StartsWith(searchPattern))
                        .OrderByDescending(c => c.Code)
                        .Select(c => c.Code)
                        .FirstOrDefaultAsync();

                    int nextCount = 1;
                    if (lastCode != null)
                    {
                        // Extract the digits after the last slash
                        var lastPart = lastCode.Split('/').Last();
                        if (int.TryParse(lastPart, out int lastNumber))
                        {
                            nextCount = lastNumber + 1;
                        }
                    }

                    var requisition = mapper.Map<Requisition>(request);

                    // 2. Assign the unique code
                    requisition.Code = $"{prefix}/{beta}/{year}/{nextCount:D3}";
                    requisition.RequestedById = userId;
                    requisition.DepartmentId = department.Id;
                    requisition.Items = mapper.Map<List<RequisitionItem>>(items);

                    await context.Requisitions.AddAsync(requisition);
                    await context.SaveChangesAsync();

                    // 3. Commit everything at once
                    await transaction.CommitAsync();

                    return requisition.Id;
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }

            var productionActivityStep = await context.ProductionActivitySteps.FirstOrDefaultAsync(
                p => p.Id == request.ProductionActivityStepId
            );

            if (productionActivityStep is not null)
            {
                productionActivityStep.StartedAt = DateTime.UtcNow;
                productionActivityStep.Status = ProductionStatus.InProgress;
                context.ProductionActivitySteps.Update(productionActivityStep);
            }

            backgroundWorkerService.EnqueueNotification(
                "Stock requisition created",
                NotificationType.StockRequisitionCreated,
                user.DepartmentId,
                []
            );
        }
        else
        {
            var requisition = mapper.Map<Requisition>(request);
            requisition.RequestedById = userId;
            requisition.DepartmentId = user.DepartmentId.Value;
            await context.Requisitions.AddAsync(requisition);

            if (request.ProductionActivityStepId.HasValue)
            {
                var activityStep = await context.ProductionActivitySteps.FirstOrDefaultAsync(p =>
                    p.Id == request.ProductionActivityStepId
                );

                if (activityStep is not null)
                {
                    activityStep.Status = ProductionStatus.InProgress;
                    activityStep.StartedAt = DateTime.UtcNow;
                    context.ProductionActivitySteps.Update(activityStep);
                }
            }
            await context.SaveChangesAsync();
            await approvalRepository.CreateInitialApprovalsAsync(
                "PurchaseRequisition",
                requisition.Id
            );
        }
        await context.SaveChangesAsync();
        return Result.Success();
    }

    // Get Stock Requisition by ID
    public async Task<Result<RequisitionDto>> GetRequisition(Guid requisitionId, Guid userId)
    {
        var requisition = await context
            .Requisitions.IgnoreQueryFilters()
            .AsSplitQuery()
            .Include(r => r.ProductionScheduleProduct)
                .ThenInclude(r => r.ProductionSchedule)
            .Include(r => r.ProductionScheduleProduct)
                .ThenInclude(r => r.Product)
            .Include(r => r.RequestedBy)
            .Include(r => r.Items)
                .ThenInclude(i => i.Material)
            .FirstOrDefaultAsync(r => r.Id == requisitionId);

        if (requisition is null)
        {
            return RequisitionErrors.NotFound(requisitionId);
        }

        var result = mapper.Map<RequisitionDto>(requisition);

        // If the requisition type is a purchase, return early
        if (requisition.RequisitionType == RequisitionType.Purchase)
            return result;

        var user = await context
            .Users.AsSplitQuery()
            .Include(u => u.Department)
                .ThenInclude(u => u.Warehouses)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user?.Department is null)
        {
            return Error.Validation(
                "User.Department",
                "This user is not assigned to any department."
            );
        }

        // Find user's raw material & packing warehouses
        var rawWarehouse = user.Department.Warehouses.FirstOrDefault(i =>
            i.Type == WarehouseType.RawMaterialStorage
        );
        if (rawWarehouse is null)
            return Error.NotFound(
                "User.Warehouse",
                "No raw material warehouse is associated with current user"
            );

        var packingWarehouse = user.Department.Warehouses.FirstOrDefault(i =>
            i.Type == WarehouseType.PackagedStorage
        );
        if (packingWarehouse is null)
            return Error.NotFound(
                "User.Warehouse",
                "No packing material warehouse is associated with current user"
            );

        var productionWarehouse = user.Department.Warehouses.FirstOrDefault(i =>
            i.Type == WarehouseType.Production
        );

        foreach (var item in result.Items)
        {
            if (requisition.ProductionScheduleProductId.HasValue && productionWarehouse != null)
            {
                var reservedBatches =
                    await materialRepository.GetReservedBatchesAndQuantityForProductionWarehouse(
                        item.Material.Id,
                        productionWarehouse.Id,
                        requisition.ProductionScheduleProductId.Value
                    );

                if (reservedBatches.Count != 0)
                {
                    item.Batches = reservedBatches
                        .Select(rb => new BatchToSupply
                        {
                            Batch = rb.MaterialBatch,
                            QuantityToTake = rb.Quantity,
                            WarehouseLocationShelfId = rb.WarehouseLocationShelf?.Id,
                        })
                        .ToList();
                    continue;
                }
            }

            // Determine appropriate warehouse based on material type
            var appropriateWarehouse =
                item.Material.Kind == MaterialKind.Raw ? rawWarehouse : packingWarehouse;

            // Fetch frozen batches that will fulfill the request
            var batchResult = await materialRepository.BatchesToSupplyForGivenQuantity(
                item.Material.Id,
                appropriateWarehouse.Id,
                item.Quantity
            );

            if (batchResult.IsSuccess)
            {
                item.Batches = batchResult.Value; // Assign batches with quantity to take
            }
        }

        return result;
    }

    public async Task<
        Result<List<MaterialAlternativeBatchesDto>>
    > GetAlternativeBatchesForStockRequisition(Guid requisitionId, Guid userId)
    {
        var requisition = await context
            .Requisitions.AsSplitQuery()
            .Include(r => r.Items)
                .ThenInclude(i => i.Material)
            .Include(r => r.Items)
                .ThenInclude(i => i.UoM)
            .Include(r => r.ProductionScheduleProduct)
            .FirstOrDefaultAsync(r => r.Id == requisitionId);

        if (requisition is null)
            return RequisitionErrors.NotFound(requisitionId);

        if (requisition.RequisitionType != RequisitionType.Stock)
            return Error.Validation(
                "Requisition.Type",
                "Only stock requisitions support alternative batch lookup."
            );

        var user = await context
            .Users.AsSplitQuery()
            .Include(u => u.Department)
                .ThenInclude(u => u.Warehouses)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user?.Department is null)
            return Error.Validation(
                "User.Department",
                "This user is not assigned to any department."
            );

        var departmentWarehouseIds = user.Department.Warehouses.Select(w => w.Id).ToList();
        var productionWarehouse = user.Department.Warehouses.FirstOrDefault(i =>
            i.Type == WarehouseType.Production
        );

        var result = new List<MaterialAlternativeBatchesDto>();

        // Fetch alternative shelf batches for every requested material in a single
        // round trip instead of once per item - the per-item query joins 4 levels
        // deep (shelf -> rack -> location -> warehouse) and was the dominant cost
        // of this endpoint. minExpiryDate varies per item, so it's applied in-memory
        // below rather than in this shared query.
        var requestedMaterialIds = requisition.Items.Select(i => i.MaterialId).Distinct().ToList();

        // A shelf batch already committed to a pending swap shouldn't be offered
        // again as an alternative for another swap - it's not actually free until
        // that swap is approved or rejected.
        var pendingSwapShelfBatchIds = (
            await context
                .SwapRequests.IgnoreQueryFilters()
                .Where(s => s.Status == SwapRequestStatus.Pending)
                .SelectMany(s =>
                    s.FirstSwapShelfMaterialBatches.Select(b => b.ShelfMaterialBatchId)
                        .Concat(s.SecondSwapShelfMaterialBatches.Select(b => b.ShelfMaterialBatchId))
                )
                .ToListAsync()
        ).ToHashSet();

        var candidateShelfBatchesByMaterial = (
            await context
                .ShelfMaterialBatches.IgnoreQueryFilters()
                .AsSplitQuery()
                .Include(smb => smb.MaterialBatch)
                    .ThenInclude(b => b.UoM)
                .Include(smb => smb.WarehouseLocationShelf)
                    .ThenInclude(s => s.WarehouseLocationRack)
                        .ThenInclude(r => r.WarehouseLocation)
                            .ThenInclude(l => l.Warehouse)
                .Where(smb =>
                    requestedMaterialIds.Contains(smb.MaterialBatch.MaterialId)
                    && smb.Quantity > 0
                    && !smb.DeletedAt.HasValue
                    && smb.MaterialBatch.Status == BatchStatus.Available
                    && (
                        smb.MaterialBatch.ExpiryDate == null
                        || smb.MaterialBatch.ExpiryDate == DateTime.MinValue
                        || smb.MaterialBatch.ExpiryDate >= DateTime.UtcNow
                    )
                    && !departmentWarehouseIds.Contains(
                        smb.WarehouseLocationShelf
                            .WarehouseLocationRack
                            .WarehouseLocation
                            .WarehouseId
                    )
                    && !pendingSwapShelfBatchIds.Contains(smb.Id)
                )
                .ToListAsync()
        )
            .GroupBy(smb => smb.MaterialBatch.MaterialId)
            .ToDictionary(g => g.Key, g => g.ToList());

        foreach (var item in requisition.Items)
        {
            var materialAlternative = new MaterialAlternativeBatchesDto
            {
                Material = mapper.Map<MaterialDto>(item.Material),
                RequestedQuantity = item.Quantity,
            };

            // 1. Get current reserved batches (if any)
            if (requisition.ProductionScheduleProductId.HasValue && productionWarehouse != null)
            {
                var reservedBatches =
                    await materialRepository.GetReservedBatchesAndQuantityForProductionWarehouse(
                        item.MaterialId,
                        productionWarehouse.Id,
                        requisition.ProductionScheduleProductId.Value
                    );

                if (reservedBatches.Count != 0)
                {
                    materialAlternative.CurrentReservedBatches = reservedBatches
                        .Select(rb => new BatchToSupply
                        {
                            Batch = rb.MaterialBatch,
                            QuantityToTake = rb.Quantity,
                            WarehouseLocationShelfId = rb.WarehouseLocationShelf?.Id,
                        })
                        .ToList();
                }
            }

            // 2. Find minimum expiry date of current batches. A batch whose
            // ExpiryDate is the DateTime.MinValue sentinel (no real expiry was
            // ever entered -- see the department-stock/forecast fixes) must be
            // excluded here too: treating it as a real date would make it the
            // "earliest" expiry, which then makes the alternative-batch filter
            // below impossible to satisfy (nothing expires before year 1) and
            // silently returns zero alternatives.
            DateTime? minExpiryDate = null;
            if (materialAlternative.CurrentReservedBatches.Count != 0)
            {
                minExpiryDate = materialAlternative
                    .CurrentReservedBatches.Where(b =>
                        b.Batch.ExpiryDate.HasValue && b.Batch.ExpiryDate.Value != DateTime.MinValue
                    )
                    .Min(b => b.Batch.ExpiryDate);
            }

            // 3. Find alternative batches across ALL warehouses EXCEPT our own department.
            // Only ever suggest usable stock: never already-expired batches --
            // the whole point is to surface batches that are *about to* expire
            // (prioritized soonest-first via the OrderBy below) so they get
            // consumed before they're wasted, not batches that already can't
            // be used.
            var candidateShelfBatches = candidateShelfBatchesByMaterial.GetValueOrDefault(
                item.MaterialId,
                []
            );

            var alternativeShelfBatches = candidateShelfBatches
                .Where(smb =>
                    !minExpiryDate.HasValue || smb.MaterialBatch.ExpiryDate < minExpiryDate.Value
                )
                .OrderBy(smb => smb.MaterialBatch.ExpiryDate)
                .ToList();

            materialAlternative.AlternativeBatches = alternativeShelfBatches
                .Select(smb => new AlternativeBatchDto
                {
                    Batch = mapper.Map<MaterialBatchListDto>(smb.MaterialBatch),
                    QuantityAvailable = smb.Quantity,
                    Warehouse = mapper.Map<CollectionItemDto>(
                        smb.WarehouseLocationShelf.WarehouseLocationRack.WarehouseLocation.Warehouse
                    ),
                    WarehouseLocationShelfId = smb.WarehouseLocationShelfId,
                })
                .ToList();

            result.Add(materialAlternative);
        }

        return result;
    }

    public async Task<Result> ApproveStockRequisition(Guid stockRequisitionId)
    {
        var stockRequisition = await context.Requisitions.FirstOrDefaultAsync(r =>
            r.Id == stockRequisitionId
        );

        return stockRequisition is null
            ? RequisitionErrors.NotFound(stockRequisitionId)
            : Result.Success();
    }

    public async Task<Result> IssueStockRequisition(Guid stockRequisitionId, Guid userId)
    {
        var stockRequisition = await context
            .Requisitions.Include(r => r.Items)
                .ThenInclude(requisitionItem => requisitionItem.Material)
            .Include(requisition => requisition.ProductionActivityStep)
            .AsSplitQuery()
            .FirstOrDefaultAsync(r => r.Id == stockRequisitionId);

        if (stockRequisition is null)
            return RequisitionErrors.NotFound(stockRequisitionId);

        if (stockRequisition.ProductionScheduleProductId is null)
            return Error.Validation(
                "Stock.Requisition",
                "Stock requisition has no production schedule product associated, contact admin."
            );

        var user = await context
            .Users.Include(u => u.Department)
                .ThenInclude(d => d.Warehouses)
            .AsSplitQuery()
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user is null)
            return UserErrors.NotFound(userId);

        if (user.Department == null)
            return Error.NotFound("User.Department", "User has no association to any department");

        if (user.Department.Warehouses.Count == 0)
            return Error.NotFound(
                "User.Warehouse",
                "No raw material warehouse is associated with current user"
            );

        var rawWarehouse = user.Department.Warehouses.FirstOrDefault(i =>
            i.Type == WarehouseType.RawMaterialStorage
        );
        if (rawWarehouse is null)
            return Error.NotFound(
                "User.Warehouse",
                "No raw material warehouse is associated with current user"
            );

        var packingWarehouse = user.Department.Warehouses.FirstOrDefault(i =>
            i.Type == WarehouseType.PackagedStorage
        );
        if (packingWarehouse is null)
            return Error.NotFound(
                "User.Warehouse",
                "No packing material warehouse is associated with current user"
            );

        var productionWarehouse = await context
            .Warehouses.IgnoreQueryFilters()
            .FirstOrDefaultAsync(w =>
                w.DepartmentId == stockRequisition.DepartmentId
                && w.Type == WarehouseType.Production
            );

        if (productionWarehouse is null)
            return Error.NotFound(
                "User.Warehouse",
                "No production warehouse is associated with department who made stock requisition"
            );

        // Pre-fetch all reserved quantities for all requisition items in a single query
        var itemMaterialIds = stockRequisition.Items.Select(i => i.MaterialId).Distinct().ToList();
        var reservedBatches = await context
            .MaterialBatchReservedQuantities.IgnoreQueryFilters()
            .Include(r => r.MaterialBatch)
                .ThenInclude(b => b.Material)
            .Where(r =>
                itemMaterialIds.Contains(r.MaterialBatch.MaterialId)
                && r.WarehouseId == productionWarehouse.Id
                && r.ProductionScheduleProductId
                    == stockRequisition.ProductionScheduleProductId.Value
                && r.DeletedAt == null
            )
            .AsSplitQuery()
            .ToListAsync();

        var reservedBatchesByMaterial = reservedBatches
            .GroupBy(r => r.MaterialBatch.MaterialId)
            .ToDictionary(g => g.Key, g => g.ToList());

        // Validate that all requisition items have reserved quantities before starting mutations
        foreach (var item in stockRequisition.Items)
        {
            if (
                !reservedBatchesByMaterial.TryGetValue(item.MaterialId, out var batches)
                || batches.Count == 0
            )
            {
                return Error.Validation(
                    "Stock.Requisition",
                    $"No reserved quantities to issue for {item.Material?.Name ?? "item"}"
                );
            }
        }

        var allBatchIds = reservedBatches.Select(b => b.MaterialBatchId).Distinct().ToList();

        // Batch pre-fetch MaterialBatches, ShelfMaterialBatches, and latest MaterialSamplings
        var materialBatchesMap = await context
            .MaterialBatches.Include(m => m.Checklist)
                .ThenInclude(c => c.Supplier)
            .Include(m => m.Checklist)
                .ThenInclude(c => c.Manufacturer)
            .Where(m => allBatchIds.Contains(m.Id))
            .AsSplitQuery()
            .ToDictionaryAsync(m => m.Id);

        var shelfMaterialBatches = await context
            .ShelfMaterialBatches.IgnoreQueryFilters()
            .Include(sb => sb.WarehouseLocationShelf)
                .ThenInclude(wls => wls.WarehouseLocationRack)
                    .ThenInclude(wlr => wlr.WarehouseLocation)
            .Where(sb =>
                allBatchIds.Contains(sb.MaterialBatchId)
                && !sb.DeletedAt.HasValue
                && (
                    sb.WarehouseLocationShelf.WarehouseLocationRack.WarehouseLocation.WarehouseId
                        == rawWarehouse.Id
                    || sb.WarehouseLocationShelf.WarehouseLocationRack.WarehouseLocation.WarehouseId
                        == packingWarehouse.Id
                )
            )
            .AsSplitQuery()
            .OrderBy(s => s.Quantity)
            .ToListAsync();

        var shelfBatchesByBatchId = shelfMaterialBatches
            .GroupBy(sb => sb.MaterialBatchId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var samplings = await context
            .MaterialSamplings.Where(s => allBatchIds.Contains(s.MaterialBatchId))
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => new { s.MaterialBatchId, s.ArNumber })
            .ToListAsync();

        var latestArNumberByBatchId = samplings
            .GroupBy(s => s.MaterialBatchId)
            .ToDictionary(g => g.Key, g => g.First().ArNumber);

        // Pre-compute initial bin card balances via targeted queries per Material & Warehouse
        var materialWarehousePairs = stockRequisition
            .Items.Select(i =>
                (
                    MaterialId: i.MaterialId,
                    WarehouseId: i.Material.Kind == MaterialKind.Raw
                        ? rawWarehouse.Id
                        : packingWarehouse.Id
                )
            )
            .Distinct()
            .ToList();

        var runningBalances = new Dictionary<(Guid MaterialId, Guid WarehouseId), decimal>();
        foreach (var pair in materialWarehousePairs)
        {
            var history = await context
                .BinCardInformation.IgnoreQueryFilters()
                .Where(b =>
                    b.WarehouseId == pair.WarehouseId
                    && b.MaterialBatch.MaterialId == pair.MaterialId
                )
                .Select(b => new { b.QuantityReceived, b.QuantityIssued })
                .AsSplitQuery()
                .ToListAsync();

            var initialBalance =
                history.Sum(x => x.QuantityReceived) - history.Sum(x => x.QuantityIssued);
            runningBalances[(pair.MaterialId, pair.WarehouseId)] = initialBalance;
        }

        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            var movementsToAdd = new List<MassMaterialBatchMovement>();
            var batchEventsToAdd = new List<MaterialBatchEvent>();
            var binCardsToAdd = new List<BinCardInformation>();
            var shelvesToRemove = new List<ShelfMaterialBatch>();
            var now = DateTime.UtcNow;

            foreach (var item in stockRequisition.Items)
            {
                var appropriateWarehouse =
                    item.Material.Kind == MaterialKind.Raw ? rawWarehouse : packingWarehouse;
                var batchesToConsume = reservedBatchesByMaterial[item.MaterialId];

                foreach (var batch in batchesToConsume)
                {
                    if (
                        !materialBatchesMap.TryGetValue(
                            batch.MaterialBatchId,
                            out var materialBatch
                        )
                    )
                        continue;

                    materialBatch.QuantityAssigned = 0;

                    if (shelfBatchesByBatchId.TryGetValue(batch.MaterialBatchId, out var shelfList))
                    {
                        var quantityToDeduct = batch.Quantity;
                        foreach (
                            var shelfMaterialBatch in shelfList.Where(sb =>
                                sb.WarehouseLocationShelf
                                    ?.WarehouseLocationRack
                                    ?.WarehouseLocation
                                    ?.WarehouseId == appropriateWarehouse.Id
                            )
                        )
                        {
                            if (quantityToDeduct <= 0)
                                break;

                            var deductAmount = Math.Min(
                                shelfMaterialBatch.Quantity,
                                quantityToDeduct
                            );
                            shelfMaterialBatch.Quantity -= deductAmount;
                            quantityToDeduct -= deductAmount;

                            if (shelfMaterialBatch.Quantity <= 0)
                            {
                                shelvesToRemove.Add(shelfMaterialBatch);
                            }
                        }
                    }

                    movementsToAdd.Add(
                        new MassMaterialBatchMovement
                        {
                            BatchId = batch.MaterialBatchId,
                            FromWarehouseId = appropriateWarehouse.Id,
                            ToWarehouseId = productionWarehouse.Id,
                            Quantity = batch.Quantity,
                            MovedAt = now,
                            MovedById = userId,
                        }
                    );

                    batchEventsToAdd.Add(
                        new MaterialBatchEvent
                        {
                            BatchId = batch.MaterialBatchId,
                            Type = EventType.Moved,
                            Quantity = batch.Quantity,
                            UserId = userId,
                        }
                    );

                    var balanceKey = (item.MaterialId, appropriateWarehouse.Id);
                    var previousBalance = runningBalances[balanceKey];
                    var currentBalance = previousBalance - batch.Quantity;
                    runningBalances[balanceKey] = currentBalance;

                    latestArNumberByBatchId.TryGetValue(materialBatch.Id, out var arNumber);
                    var supplier = materialBatch.Checklist?.Supplier?.Name;
                    var manufacturer = materialBatch.Checklist?.Manufacturer?.Name;

                    binCardsToAdd.Add(
                        new BinCardInformation
                        {
                            MaterialBatchId = materialBatch.Id,
                            MaterialBatch = materialBatch,
                            Description = appropriateWarehouse.Name,
                            WayBill = "N/A",
                            ArNumber = arNumber ?? "N/A",
                            Supplier = supplier,
                            Manufacturer = manufacturer,
                            QuantityReceived = 0,
                            QuantityIssued = batch.Quantity,
                            BalanceQuantity = currentBalance,
                            UoMId = materialBatch.UoMId,
                            CreatedAt = now,
                            WarehouseId = appropriateWarehouse.Id,
                        }
                    );

                    item.Status = RequestStatus.Completed;
                }
            }

            if (shelvesToRemove.Count > 0)
            {
                context.ShelfMaterialBatches.RemoveRange(shelvesToRemove);
            }

            if (movementsToAdd.Count > 0)
            {
                context.MassMaterialBatchMovements.AddRange(movementsToAdd);
            }

            if (batchEventsToAdd.Count > 0)
            {
                context.MaterialBatchEvents.AddRange(batchEventsToAdd);
            }

            if (binCardsToAdd.Count > 0)
            {
                context.BinCardInformation.AddRange(binCardsToAdd);
            }

            var allItemsCompleted = stockRequisition.Items.All(i =>
                i.Status == RequestStatus.Completed
            );

            if (allItemsCompleted)
            {
                stockRequisition.Status = RequestStatus.Completed;
            }

            Guid? completedStepId = null;
            if (stockRequisition.ProductionActivityStepId.HasValue)
            {
                var anyOtherPendingItems = await context.RequisitionItems.AnyAsync(ri =>
                    ri.Requisition.ProductionActivityStepId
                        == stockRequisition.ProductionActivityStepId
                    && ri.RequisitionId != stockRequisition.Id
                    && ri.Status != RequestStatus.Completed
                );

                if (!anyOtherPendingItems && allItemsCompleted)
                {
                    var productionActivityStep =
                        await context.ProductionActivitySteps.FirstOrDefaultAsync(p =>
                            p.Id == stockRequisition.ProductionActivityStepId
                        );

                    if (productionActivityStep is not null)
                    {
                        productionActivityStep.Status = ProductionStatus.Completed;
                        productionActivityStep.CompletedAt = now;
                        completedStepId = productionActivityStep.Id;
                        if (stockRequisition.ProductionActivityStep is not null)
                        {
                            context.ProductionActivityLogs.Add(
                                new ProductionActivityLog
                                {
                                    ProductionActivityId = stockRequisition
                                        .ProductionActivityStep
                                        .ProductionActivityId,
                                    UserId = userId,
                                    Message = "Issued stock requisition.",
                                    Timestamp = now,
                                }
                            );
                        }
                    }
                }
            }

            await context.SaveChangesAsync();
            await transaction.CommitAsync();

            if (completedStepId.HasValue)
            {
                await stepEventPublisher.PublishStatusChanged(
                    completedStepId.Value,
                    ProductionStatus.Completed,
                    userId
                );
            }

            return Result.Success();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private async Task<List<ShelfMaterialBatchDto>> GetShelvesOfBatch(
        Guid batchId,
        Guid warehouseId
    )
    {
        var shelves = await context
            .ShelfMaterialBatches.AsSplitQuery()
            .Include(smb => smb.WarehouseLocationShelf)
            .Where(smb =>
                smb.MaterialBatchId == batchId
                && smb.WarehouseLocationShelf.WarehouseLocationRack.WarehouseLocation.Warehouse.Id
                    == warehouseId
            )
            .ToListAsync();

        return mapper.Map<List<ShelfMaterialBatchDto>>(shelves);
    }

    public async Task<Result> IssueStockRequisitionVoucher(
        List<BatchQuantityDto> batchQuantities,
        Guid productId,
        Guid userId
    )
    {
        var product = await context.Products.FirstOrDefaultAsync(p => p.Id == productId);
        foreach (var batch in batchQuantities)
        {
            var shelfMaterialBatch = await context
                .ShelfMaterialBatches.AsSplitQuery()
                .Include(smb => smb.WarehouseLocationShelf)
                    .ThenInclude(wls => wls.WarehouseLocationRack)
                        .ThenInclude(wlr => wlr.WarehouseLocation)
                            .ThenInclude(wl => wl.Warehouse)
                .Include(shelfMaterialBatch => shelfMaterialBatch.MaterialBatch)
                    .ThenInclude(mb => mb.Checklist)
                        .ThenInclude(c => c.Supplier)
                .Include(shelfMaterialBatch => shelfMaterialBatch.MaterialBatch)
                    .ThenInclude(mb => mb.Checklist)
                        .ThenInclude(c => c.Manufacturer)
                .FirstOrDefaultAsync(smb => smb.Id == batch.ShelfMaterialBatchId);

            if (shelfMaterialBatch == null)
            {
                return Error.Validation(
                    "ShelfMaterialBatch.NotFound",
                    $"ShelfMaterialBatch with ID {batch.ShelfMaterialBatchId} not found."
                );
            }

            if (shelfMaterialBatch.Quantity < batch.Quantity)
            {
                return Error.Validation(
                    "ShelfMaterialBatch.InsufficientQuantity",
                    $"Insufficient quantity in ShelfMaterialBatch with ID {batch.ShelfMaterialBatchId}."
                );
            }

            var productionWarehouse = await context
                .Warehouses.AsSplitQuery()
                .Where(dw =>
                    dw.Id
                    == shelfMaterialBatch
                        .WarehouseLocationShelf
                        .WarehouseLocationRack
                        .WarehouseLocation
                        .Warehouse
                        .Id
                )
                .Include(warehouse => warehouse.ArrivalLocation)
                .FirstOrDefaultAsync(w => w.Type == WarehouseType.Production);

            if (productionWarehouse == null)
            {
                return Error.Validation(
                    "ProductionWarehouse.NotFound",
                    "Production warehouse not found for the department."
                );
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
                UserId = userId,
            };

            await context.MaterialBatchEvents.AddAsync(materialBatchEvent);

            var batchMovement = new MassMaterialBatchMovement
            {
                BatchId = shelfMaterialBatch.MaterialBatchId,
                FromWarehouseId = shelfMaterialBatch
                    .WarehouseLocationShelf
                    .WarehouseLocationRack
                    .WarehouseLocation
                    .Warehouse
                    .Id,
                ToWarehouseId = productionWarehouse.Id,
                Quantity = batch.Quantity,
                CreatedById = userId,
            };

            await context.MassMaterialBatchMovements.AddAsync(batchMovement);

            var fromWarehouse = shelfMaterialBatch
                .WarehouseLocationShelf
                .WarehouseLocationRack
                .WarehouseLocation
                .Warehouse;

            var toWarehouse = productionWarehouse;

            var warehouseIds = new List<Guid> { fromWarehouse.Id, toWarehouse.Id };

            var history = await context
                .BinCardInformation.AsSplitQuery()
                .IgnoreQueryFilters()
                .Where(b =>
                    b.MaterialBatch.MaterialId == shelfMaterialBatch.MaterialBatch.MaterialId
                    && warehouseIds.Contains(b.WarehouseId.Value)
                )
                .Select(b => new
                {
                    b.WarehouseId,
                    b.QuantityReceived,
                    b.QuantityIssued,
                })
                .ToListAsync();

            var fromBalance = history
                .Where(h => h.WarehouseId == fromWarehouse.Id)
                .Sum(x => x.QuantityReceived - x.QuantityIssued);

            var toBalance = history
                .Where(h => h.WarehouseId == toWarehouse.Id)
                .Sum(x => x.QuantityReceived - x.QuantityIssued);

            var balanceAfterIssue = fromBalance - batch.Quantity;
            var balanceAfterReceive = toBalance + batch.Quantity;

            var arNumber = await context
                .MaterialSamplings.Where(s =>
                    s.MaterialBatchId == shelfMaterialBatch.MaterialBatch.Id
                )
                .OrderByDescending(s => s.CreatedAt)
                .Select(s => s.ArNumber)
                .FirstOrDefaultAsync();

            var supplier = shelfMaterialBatch.MaterialBatch.Checklist?.Supplier?.Name;
            var manufacturer = shelfMaterialBatch.MaterialBatch.Checklist?.Manufacturer?.Name;

            var toBinCardEvent = new BinCardInformation
            {
                MaterialBatchId = shelfMaterialBatch.MaterialBatch.Id,
                Description = fromWarehouse.Name,
                WayBill = "N/A",
                ArNumber = arNumber ?? "N/A",
                Supplier = supplier,
                Manufacturer = manufacturer,
                QuantityReceived = 0,
                QuantityIssued = batch.Quantity,
                BalanceQuantity = balanceAfterIssue,
                UoMId = shelfMaterialBatch.MaterialBatch.UoMId,
                ProductId = product.Id,
                CreatedAt = DateTime.UtcNow,
                CreatedById = userId,
                WarehouseId = fromWarehouse.Id,
            };

            await context.BinCardInformation.AddAsync(toBinCardEvent);

            var fromBinCardEvent = new BinCardInformation
            {
                MaterialBatchId = shelfMaterialBatch.MaterialBatch.Id,
                Description = productionWarehouse.Name,
                WayBill = "N/A",
                ArNumber = arNumber ?? "N/A",
                Supplier = supplier,
                Manufacturer = manufacturer,
                QuantityReceived = batch.Quantity,
                QuantityIssued = 0,
                BalanceQuantity = balanceAfterReceive,
                UoMId = shelfMaterialBatch.MaterialBatch.UoMId,
                ProductId = product.Id,
                CreatedAt = DateTime.UtcNow,
                CreatedById = userId,
                WarehouseId = productionWarehouse.Id,
            };

            await context.BinCardInformation.AddAsync(fromBinCardEvent);
        }

        await context.SaveChangesAsync();
        return Result.Success();
    }

    // Get paginated list of Stock Requisitions
    public async Task<Result<Paginateable<IEnumerable<RequisitionDto>>>> GetRequisitions(
        int page,
        int pageSize,
        string searchQuery,
        RequestStatus? status,
        RequisitionType? requisitionType,
        Guid? departmentId,
        MaterialKind? materialKind
    )
    {
        var query = context
            .Requisitions.AsSplitQuery()
            .Include(r => r.RequestedBy)
            .Include(r => r.Approvals)
                .ThenInclude(r => r.User)
            .Include(r => r.Approvals)
                .ThenInclude(r => r.Role)
            .Include(r => r.Items)
                .ThenInclude(i => i.Material)
            .Include(r => r.ProductionScheduleProduct)
                .ThenInclude(rp => rp.Product)
            .Include(r => r.ProductionScheduleProduct)
                .ThenInclude(rp => rp.ProductionSchedule)
            .Include(r => r.ProductionScheduleProduct)
                .ThenInclude(rp => rp.ProductionActivity)
            .OrderByDescending(s => s.CreatedAt)
            .AsQueryable();

        if (departmentId.HasValue)
        {
            query = query.Where(q => q.DepartmentId == departmentId);
        }

        if (status.HasValue)
        {
            // Only return requisitions that have at least one item with the requested status
            query = query.Where(r => r.Items.Any(i => i.Status == status));
        }

        if (requisitionType.HasValue)
        {
            query = query.Where(r => r.RequisitionType == requisitionType);

            // Apply filtering based on materialKind when requisitionType is Stock
            if (requisitionType == RequisitionType.Stock && materialKind.HasValue)
            {
                switch (materialKind.Value)
                {
                    case MaterialKind.Raw:
                        query = query.Where(r => r.Code.StartsWith("RM"));
                        break;
                    case MaterialKind.Package:
                        query = query.Where(r => r.Code.StartsWith("PM"));
                        break;
                }
            }
        }

        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.WhereSearch(searchQuery, r => r.Comments, r => r.Code);
        }

        var result = await PaginationHelper.GetPaginatedResultAsync(
            query,
            page,
            pageSize,
            r =>
            {
                var dto = mapper.Map<RequisitionDto>(r);
                if (status.HasValue)
                {
                    // Filter out only items with the given status
                    dto.Items = dto.Items.Where(i => i.Status == status).ToList();
                }
                return dto;
            }
        );

        return result;
    }

    // Update Stock Requisition
    public async Task<Result> UpdateRequisition(
        CreateRequisitionRequest request,
        Guid requisitionId,
        Guid userId
    )
    {
        var existingRequisition = await context.Requisitions.FirstOrDefaultAsync(r =>
            r.Id == requisitionId
        );
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
        var requisition = await context.Requisitions.FirstOrDefaultAsync(r =>
            r.Id == requisitionId
        );
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
    public async Task<Result> ApproveRequisition(
        ApproveRequisitionRequest request,
        Guid requisitionId,
        Guid userId,
        List<Guid> roleIds
    )
    {
        // Get the requisition and its approvals
        var requisition = await context
            .Requisitions.Include(r => r.Approvals)
            .Include(requisition => requisition.ProductionActivityStep)
            .Include(requisition => requisition.RequestedBy)
                .ThenInclude(r => r.Department)
                    .ThenInclude(d => d.Warehouses)
            .Include(requisition => requisition.Items)
            .AsSplitQuery()
            .FirstOrDefaultAsync(r => r.Id == requisitionId);

        if (requisition == null)
        {
            return RequisitionErrors.NotFound(requisitionId);
        }

        if (
            !requisition.ProductionActivityStepId.HasValue
            || requisition.RequisitionType == RequisitionType.Purchase
        )
        {
            return Error.Validation(
                "Requisition.Approve",
                "You cant approve a purchase requisition"
            );
        }

        requisition.Approved = true;
        requisition.ProductionActivityStep.Status = ProductionStatus.Completed;
        requisition.ProductionActivityStep.CompletedAt = DateTime.UtcNow;
        context.ProductionActivitySteps.Update(requisition.ProductionActivityStep);

        await context.SaveChangesAsync();
        return Result.Success();
    }

    // ************* CRUD for SourceRequisition *************

    // Create Source Requisition
    public async Task<Result> CreateSourceRequisition(
        CreateSourceRequisitionRequest request,
        Guid userId
    )
    {
        var requisition = await context
            .Requisitions.AsSplitQuery()
            .Include(r => r.Items) // Include items since their status will be updated
            .FirstOrDefaultAsync(r => r.Id == request.RequisitionId);

        if (requisition is null)
            return RequisitionErrors.NotFound(request.RequisitionId);

        var supplierGroupedItems = request
            .Items.SelectMany(item => item.Suppliers.Select(supplier => new { item, supplier }))
            .GroupBy(x => x.supplier.SupplierId);

        foreach (var supplierGroup in supplierGroupedItems)
        {
            var supplierId = supplierGroup.Key;

            var existingSourceRequisition = await context
                .SourceRequisitions.Include(sr => sr.Items)
                .FirstOrDefaultAsync(sr =>
                    sr.SupplierId == supplierId && !sr.SentQuotationRequestAt.HasValue
                );

            if (existingSourceRequisition is not null)
            {
                foreach (var groupItem in supplierGroup)
                {
                    existingSourceRequisition.Items.Add(
                        new SourceRequisitionItem
                        {
                            MaterialId = groupItem.item.MaterialId,
                            UoMId = groupItem.item.UoMId,
                            Quantity = groupItem.item.Quantity,
                            Source = groupItem.item.Source,
                            RequisitionId = request.RequisitionId,
                        }
                    );

                    // Update the status of the corresponding item in the requisition
                    var requisitionItem = requisition.Items.FirstOrDefault(i =>
                        i.MaterialId == groupItem.item.MaterialId && i.UoMId == groupItem.item.UoMId
                    );
                    if (requisitionItem is not null)
                    {
                        requisitionItem.Status = RequestStatus.Sourced;
                    }
                }
                context.SourceRequisitions.Update(existingSourceRequisition);
            }
            else
            {
                var requisitionForSupplier = new SourceRequisition
                {
                    Code = request.Code,
                    SupplierId = supplierId,
                    SentQuotationRequestAt = null,
                    Items = supplierGroup
                        .Select(x => new SourceRequisitionItem
                        {
                            MaterialId = x.item.MaterialId,
                            UoMId = x.item.UoMId,
                            Quantity = x.item.Quantity,
                            Source = x.item.Source,
                            RequisitionId = request.RequisitionId,
                        })
                        .ToList(),
                };

                await context.SourceRequisitions.AddAsync(requisitionForSupplier);

                // Update the status of all items in this group
                foreach (var x in supplierGroup)
                {
                    var requisitionItem = requisition.Items.FirstOrDefault(i =>
                        i.MaterialId == x.item.MaterialId && i.UoMId == x.item.UoMId
                    );
                    if (requisitionItem is not null)
                    {
                        requisitionItem.Status = RequestStatus.Sourced;
                    }
                }
            }
        }

        // No need to update `requisition.Status`, since status is now tracked at the item level
        requisition.Status = RequestStatus.Sourced;
        context.Requisitions.Update(requisition);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    // Get Source Requisition by ID
    public async Task<Result<SourceRequisitionDto>> GetSourceRequisition(Guid sourceRequisitionId)
    {
        var sourceRequisition = await context
            .SourceRequisitions.AsSplitQuery()
            .Include(sr => sr.Supplier)
            .Include(sr => sr.Items)
                .ThenInclude(item => item.Material)
            .Include(sr => sr.Items)
                .ThenInclude(item => item.UoM)
            .FirstOrDefaultAsync(sr => sr.Id == sourceRequisitionId);

        return sourceRequisition is null
            ? RequisitionErrors.NotFound(sourceRequisitionId)
            : mapper.Map<SourceRequisitionDto>(
                sourceRequisition,
                opt =>
                {
                    opt.Items[AppConstants.ModelType] = nameof(SourceRequisition);
                }
            );
    }

    // Get paginated list of Source Requisitions
    public async Task<
        Result<Paginateable<IEnumerable<SourceRequisitionDto>>>
    > GetSourceRequisitions(int page, int pageSize, string searchQuery)
    {
        var query = context
            .SourceRequisitions.AsSplitQuery()
            .Include(sr => sr.Supplier)
                .ThenInclude(sr => sr.AssociatedManufacturers)
                    .ThenInclude(sr => sr.Manufacturer)
            .Include(sr => sr.Items)
                .ThenInclude(item => item.Material)
            .Include(sr => sr.Items)
                .ThenInclude(item => item.UoM)
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

    public async Task<
        Result<Paginateable<IEnumerable<SourceRequisitionItemDto>>>
    > GetSourceRequisitionItems(int page, int pageSize, ProcurementSource source)
    {
        var query = context
            .SourceRequisitionItems.AsSplitQuery()
            .Include(sr => sr.SourceRequisition)
            .Include(sr => sr.Material)
            .Include(sr => sr.UoM)
            .Where(sr => sr.Source == source)
            .OrderByDescending(s => s.CreatedAt)
            .AsQueryable();

        return await PaginationHelper.GetPaginatedResultAsync(
            query,
            page,
            pageSize,
            mapper.Map<SourceRequisitionItemDto>
        );
    }

    // Update Source Requisition
    public async Task<Result> UpdateSourceRequisition(
        CreateSourceRequisitionRequest request,
        Guid sourceRequisitionId
    )
    {
        var existingSourceRequisition = await context.SourceRequisitions.FirstOrDefaultAsync(sr =>
            sr.Id == sourceRequisitionId
        );
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
        var sourceRequisition = await context.SourceRequisitions.FirstOrDefaultAsync(sr =>
            sr.Id == sourceRequisitionId
        );
        if (sourceRequisition is null)
        {
            return RequisitionErrors.NotFound(sourceRequisitionId);
        }

        sourceRequisition.DeletedAt = DateTime.UtcNow;
        context.SourceRequisitions.Update(sourceRequisition);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<
        Result<Paginateable<IEnumerable<SupplierQuotationRequest>>>
    > GetSuppliersWithSourceRequisitionItems(int page, int pageSize, SupplierType source, bool sent)
    {
        // Base query
        var query = context
            .SourceRequisitions.AsSplitQuery()
            .Include(sr => sr.Supplier)
                .ThenInclude(s => s.AssociatedManufacturers)
                    .ThenInclude(s => s.Manufacturer)
            .Include(sr => sr.Items)
                .ThenInclude(item => item.Material)
            .Include(sr => sr.Items)
                .ThenInclude(item => item.UoM)
            .Where(sr => sr.Supplier.Type == source)
            .AsQueryable();

        query = sent
            ? query.Where(s => s.SentQuotationRequestAt != null)
            : query.Where(s => s.SentQuotationRequestAt == null);

        return await PaginationHelper.GetPaginatedResultAsync(
            query,
            page,
            pageSize,
            mapper.Map<SupplierQuotationRequest>
        );
    }

    public async Task<Result<SupplierQuotationRequest>> GetSuppliersWithSourceRequisitionItems(
        Guid supplierId
    )
    {
        // Base query
        var query = await context
            .SourceRequisitions.AsSplitQuery()
            .Include(sr => sr.Supplier)
                .ThenInclude(s => s.AssociatedManufacturers)
                    .ThenInclude(m => m.Manufacturer)
            .Include(sr => sr.Items)
                .ThenInclude(item => item.Material)
            .Include(sr => sr.Items)
                .ThenInclude(item => item.UoM)
            .Where(sr => sr.SupplierId == supplierId)
            .OrderBy(sr => sr.SentQuotationRequestAt.HasValue)
            .ThenByDescending(sr => sr.CreatedAt)
            .FirstOrDefaultAsync();

        if (query is null)
        {
            return Error.NotFound(
                "Supplier.QuotationRequest.NotFound",
                "No quotation request was found for the specified supplier."
            );
        }

        return mapper.Map<SupplierQuotationRequest>(query);
    }

    public async Task<Result> SendQuotationToSupplier(Guid supplierId)
    {
        var sourceRequisition = await context
            .SourceRequisitions.AsSplitQuery()
            .Include(sr => sr.Supplier)
            .Include(sr => sr.Items)
                .ThenInclude(item => item.Material)
            .Include(sr => sr.Items)
                .ThenInclude(item => item.UoM)
            .FirstOrDefaultAsync(s =>
                s.SupplierId == supplierId && !s.SentQuotationRequestAt.HasValue
            );

        if (sourceRequisition is null)
        {
            return Error.Validation(
                "Supplier.Quotation",
                "No items found to mark as quotation sent for the specified supplier."
            );
        }

        if (await CheckIfSupplierHasPendingPriceComparison(supplierId))
        {
            return Error.Validation(
                "Supplier.Quotation",
                "This supplier is in a pending price comparison, process that before proceeding"
            );
        }

        var supplierQuotationDto = mapper.Map<SupplierQuotationRequest>(sourceRequisition);

        var sourceRequisitionDto = mapper.Map<SourceRequisitionDto>(sourceRequisition);

        if (supplierQuotationDto.Items.Count == 0)
        {
            return Error.Validation(
                "Supplier.Quotation",
                "No items found to mark as quotation sent for the specified supplier."
            );
        }

        var mailAttachments = new List<(byte[] fileContent, string fileName, string fileType)>();
        var fileContent = pdfService.GeneratePdfFromHtml(
            PdfTemplate.QuotationRequestTemplate(supplierQuotationDto)
        );
        mailAttachments.Add((fileContent, $"Quotation Request from Entrance", "application/pdf"));

        try
        {
            emailService.SendMail(
                supplierQuotationDto.Supplier.Name,
                supplierQuotationDto.Supplier.Email,
                "Sales Quote From Entrance",
                "Please find attached to this email a sales quote from us.",
                mailAttachments
            );
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
            Items = sourceRequisitionDto
                .Items.Select(i => new SupplierQuotationItem
                {
                    MaterialId = i.Material.Id,
                    UoMId = i.UoM.Id,
                    Quantity = i.Quantity,
                })
                .ToList(),
        };

        context.SourceRequisitions.UpdateRange(sourceRequisition);
        await context.SupplierQuotations.AddAsync(supplierQuotation);
        // Save changes to the database
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<bool> CheckIfSupplierHasPendingPriceComparison(Guid supplierId)
    {
        return await context.SupplierQuotations.AnyAsync(s =>
            s.SupplierId == supplierId
            && s.Items.Any(si => si.Status == SupplierQuotationItemStatus.NotProcessed)
        );
    }

    public async Task<
        Result<Paginateable<IEnumerable<SupplierQuotationDto>>>
    > GetSupplierQuotations(int page, int pageSize, SupplierType supplierType, bool received)
    {
        var query = context
            .SupplierQuotations.AsSplitQuery()
            .Include(s => s.Items)
                .ThenInclude(s => s.Material)
            .Include(s => s.Items)
                .ThenInclude(s => s.UoM)
            .Include(s => s.Supplier)
            .Where(s => s.Supplier.Type == supplierType)
            .AsQueryable();

        var supplierQuotations = received
            ? query.Where(s => s.ReceivedQuotation)
            : query.Where(s => !s.ReceivedQuotation);

        return await PaginationHelper.GetPaginatedResultAsync(
            supplierQuotations,
            page,
            pageSize,
            mapper.Map<SupplierQuotationDto>
        );
    }

    public async Task<Result<SupplierQuotationDto>> GetSupplierQuotation(Guid supplierQuotationId)
    {
        var supplierQuotation = await context
            .SupplierQuotations.AsSplitQuery()
            .Include(s => s.Items)
                .ThenInclude(s => s.Material)
            .Include(s => s.Items)
                .ThenInclude(s => s.UoM)
            .Include(sr => sr.Supplier)
                .ThenInclude(s => s.AssociatedManufacturers)
                    .ThenInclude(m => m.Manufacturer)
            .FirstOrDefaultAsync(s => s.Id == supplierQuotationId);

        if (supplierQuotation is null)
        {
            return Error.NotFound(
                "Supplier.Quotation.NotFound",
                "No supplier quotation was found for the specified quotation."
            );
        }

        return mapper.Map<SupplierQuotationDto>(supplierQuotation);
    }

    public async Task<Result> ReceiveQuotationFromSupplier(
        List<SupplierQuotationResponseDto> supplierQuotationResponse,
        Guid supplierQuotationId
    )
    {
        var supplierQuotation = await context
            .SupplierQuotations.AsSplitQuery()
            .Include(s => s.Items)
                .ThenInclude(s => s.Material)
            .Include(s => s.Items)
                .ThenInclude(s => s.UoM)
            .Include(s => s.Supplier)
            .FirstOrDefaultAsync(s => s.Id == supplierQuotationId);

        if (supplierQuotation is null)
        {
            return Error.NotFound(
                "Supplier.Quotation.NotFound",
                "No supplier quotation was found for the specified quotation."
            );
        }

        if (supplierQuotation.Items.Count == 0)
        {
            return Error.Validation(
                "Supplier.Quotation",
                "No items found to mark as quotation sent for the specified supplier."
            );
        }

        var responseLookup = supplierQuotationResponse
            .Where(s => s is not null)
            .GroupBy(s => s.Id)
            .ToDictionary(g => g.Key, g => g.Last());

        foreach (var item in supplierQuotation.Items)
        {
            // Only touch items actually present in the payload. Assigning
            // unconditionally would wipe QuotedPrice and PriceUoM off every item a
            // partial re-submit happens to omit.
            if (!responseLookup.TryGetValue(item.Id, out var response))
                continue;

            item.QuotedPrice = response.Price;
            item.PriceUoM = response.PriceUoM;
        }

        supplierQuotation.ReceivedQuotation = true;
        context.SupplierQuotations.Update(supplierQuotation);
        context.SupplierQuotationItems.UpdateRange(supplierQuotation.Items);
        // Save changes to the database
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result<List<SupplierPriceComparison>>> GetPriceComparisonOfMaterial(
        SupplierType supplierType
    )
    {
        var sourceRequisitionItemSuppliers = await context
            .SupplierQuotationItems.AsSplitQuery()
            .Include(s => s.Material)
            .Include(s => s.UoM)
            .Include(s => s.SupplierQuotation)
                .ThenInclude(s => s.Supplier)
                    .ThenInclude(s => s.AssociatedManufacturers)
                        .ThenInclude(m => m.Manufacturer)
            .Include(s => s.SupplierQuotation)
                .ThenInclude(s => s.Supplier)
                    .ThenInclude(s => s.Currency)
            .Include(s => s.SupplierQuotation)
                .ThenInclude(s => s.SourceRequisition)
            .Where(s =>
                s.QuotedPrice != null
                && s.Status == SupplierQuotationItemStatus.NotProcessed
                && s.SupplierQuotation.Supplier.Type == supplierType
            )
            .ToListAsync();

        return sourceRequisitionItemSuppliers
            .GroupBy(s => new { s.Material, s.UoM })
            .Select(item => new SupplierPriceComparison
            {
                Material = mapper.Map<CollectionItemDto>(item.Key.Material),
                UoM = mapper.Map<UnitOfMeasureDto>(item.Key.UoM),
                Quantity = item.Select(s => s.Quantity).First(),
                SupplierQuotation = item.GroupBy(s => s.SupplierQuotation.SupplierId)
                    .Select(sg => sg.OrderByDescending(s => s.SupplierQuotation.CreatedAt).First())
                    .Select(s => new SupplierPrice
                    {
                        Supplier = mapper.Map<SupplierDto>(s.SupplierQuotation.Supplier),
                        SourceRequisition = mapper.Map<CollectionItemDto>(
                            s.SupplierQuotation.SourceRequisition
                        ),
                        DefaultManufacturer = mapper.Map<ManufacturerListDto>(
                            s.SupplierQuotation.Supplier.AssociatedManufacturers.First(m =>
                                m.MaterialId == item.Key.Material.Id && m.Default
                            ).Manufacturer
                        ),
                        Status = s.Status,
                        Price = s.QuotedPrice,
                        PriceUoM = s.PriceUoM,
                    })
                    .ToList(),
            })
            .ToList();
    }

    public async Task<
        Result<List<SupplierPriceComparison>>
    > GetPriceComparisonOfMaterialByPurchaseOrderIdAndMaterialId(
        SupplierType supplierType,
        Guid materialId,
        Guid purchaseOrderId,
        SupplierQuotationItemStatus? status
    )
    {
        var sourceRequisitionItemSuppliers = await context
            .SupplierQuotationItems.AsSplitQuery()
            .Include(s => s.Material)
            .Include(s => s.UoM)
            .Include(s => s.SupplierQuotation)
                .ThenInclude(s => s.Supplier)
                    .ThenInclude(s => s.AssociatedManufacturers)
            .Include(s => s.SupplierQuotation)
                .ThenInclude(s => s.Supplier)
                    .ThenInclude(s => s.Currency)
            .Include(s => s.SupplierQuotation)
                .ThenInclude(s => s.SourceRequisition)
            .Where(s =>
                s.QuotedPrice != null
                && s.SupplierQuotation.Supplier.Type == supplierType
                && s.MaterialId == materialId
                && s.PurchaseOrderId == purchaseOrderId
            )
            .ToListAsync();

        if (status.HasValue)
        {
            sourceRequisitionItemSuppliers = sourceRequisitionItemSuppliers
                .Where(s => s.Status == status)
                .ToList();
        }

        return sourceRequisitionItemSuppliers
            .GroupBy(s => new { s.Material, s.UoM })
            .Select(item => new SupplierPriceComparison
            {
                Material = mapper.Map<CollectionItemDto>(item.Key.Material),
                UoM = mapper.Map<UnitOfMeasureDto>(item.Key.UoM),
                Quantity = item.Select(s => s.Quantity).First(),
                SupplierQuotation = item.GroupBy(s => s.SupplierQuotation.SupplierId)
                    .Select(sg => sg.OrderByDescending(s => s.SupplierQuotation.CreatedAt).First())
                    .Select(s => new SupplierPrice
                    {
                        Supplier = mapper.Map<SupplierDto>(s.SupplierQuotation.Supplier),
                        SourceRequisition = mapper.Map<CollectionItemDto>(
                            s.SupplierQuotation.SourceRequisition
                        ),
                        Status = s.Status,
                        Price = s.QuotedPrice,
                        PriceUoM = s.PriceUoM,
                    })
                    .ToList(),
            })
            .ToList();
    }

    public async Task<Result> ProcessQuotationAndCreatePurchaseOrder(
        List<ProcessQuotation> processQuotations,
        SupplierType type,
        Guid userId
    )
    {
        foreach (var quotation in processQuotations)
        {
            var sourceRequisition = await context
                .SourceRequisitions.AsSplitQuery()
                .Include(sourceRequisition => sourceRequisition.Items)
                .FirstOrDefaultAsync(s => s.Id == quotation.SourceRequisitionId);
            if (sourceRequisition == null)
                return Error.NotFound("Source.Requisition", "Source requisition not found");

            if (quotation.Items.Count > sourceRequisition.Items.Count)
            {
                return Error.Validation(
                    "SourceRequisition.Items",
                    "Source requisition items count is greater"
                        + " than what is in the source requisition item count"
                );
            }

            if (
                !quotation
                    .Items.Select(qi => qi.MaterialId)
                    .All(id => sourceRequisition.Items.Select(si => si.MaterialId).Contains(id))
            )
            {
                return Error.Validation(
                    "SourceRequisition.Materials",
                    "Quotation contains materials that are not in the source requisition."
                );
            }

            var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user is null)
                return UserErrors.NotFound(userId);

            // Resolve the awarded quotation line for every requested item BEFORE the
            // purchase order is created. The quotation is the source of truth for both
            // Price and PriceUoM; the client payload is not trusted, because a price
            // stored without the unit it was quoted in cannot be interpreted downstream.
            var awardedItems =
                new List<(
                    CreatePurchaseOrderItemRequest Request,
                    SupplierQuotationItem Quotation
                )>();

            foreach (var processSupplierQuote in quotation.Items)
            {
                var awardedQuotationItem = await context.SupplierQuotationItems.FirstOrDefaultAsync(
                    s =>
                        s.SupplierQuotation.SupplierId == quotation.SupplierId
                        && s.MaterialId == processSupplierQuote.MaterialId
                        && s.UoMId == processSupplierQuote.UomId
                        && s.Status == SupplierQuotationItemStatus.NotProcessed
                );

                if (awardedQuotationItem is null)
                {
                    return Error.Validation(
                        "SupplierQuotation.Item",
                        "No open quotation line was found for one of the selected materials."
                    );
                }

                if (string.IsNullOrWhiteSpace(awardedQuotationItem.PriceUoM))
                {
                    return Error.Validation(
                        "SupplierQuotation.PriceUoM",
                        "The selected quotation has no price UoM. Capture the price UoM on"
                            + " the quotation before awarding it."
                    );
                }

                processSupplierQuote.Price =
                    awardedQuotationItem.QuotedPrice ?? processSupplierQuote.Price;
                processSupplierQuote.PriceUoM = awardedQuotationItem.PriceUoM;

                awardedItems.Add((processSupplierQuote, awardedQuotationItem));
            }

            var poId = (
                await procurementRepository.CreatePurchaseOrder(
                    new CreatePurchaseOrderRequest
                    {
                        Code = await GeneratePurchaseOrderCode(),
                        SupplierId = quotation.SupplierId,
                        DepartmentId = user.DepartmentId,
                        SourceRequisitionId = quotation.SourceRequisitionId,
                        RequestDate = DateTime.UtcNow,
                        Items = quotation.Items,
                    },
                    userId
                )
            ).Value;

            foreach (var (processSupplierQuote, supplierQuotationItem) in awardedItems)
            {
                //Process for the supplier
                supplierQuotationItem.Status = SupplierQuotationItemStatus.Processed;
                supplierQuotationItem.PurchaseOrderId = poId;
                context.SupplierQuotationItems.Update(supplierQuotationItem);
                await context.SaveChangesAsync();

                // Process for everyone else. The losing quotes deliberately keep the
                // winner's PurchaseOrderId: GetPriceComparisonOfMaterialByPurchaseOrderIdAndMaterialId
                // (the reassign-supplier picker) looks them up by it. That means a PO id
                // alone does NOT identify the awarded supplier, so every read path that
                // resolves PriceUoM from quotations must also scope by the purchase
                // order's own SupplierId and Status == Processed. See PriceUoMExtensions.
                var supplierQuotationItems = await context
                    .SupplierQuotationItems.AsSplitQuery()
                    .Include(s => s.SupplierQuotation)
                        .ThenInclude(s => s.Supplier)
                    .Where(s =>
                        s.MaterialId == processSupplierQuote.MaterialId
                        && s.SupplierQuotation.Supplier.Type == type
                        && s.Status != SupplierQuotationItemStatus.Processed
                        && !s.PurchaseOrderId.HasValue
                    )
                    .ToListAsync();

                foreach (var supplierQuotation in supplierQuotationItems)
                {
                    supplierQuotation.PurchaseOrderId = poId;
                    context.SupplierQuotationItems.Update(supplierQuotation);
                }
            }

            await context.SaveChangesAsync();
        }

        var supplierQuotations = await context
            .SupplierQuotations.AsSplitQuery()
            .Include(s => s.Items)
                .ThenInclude(s => s.SupplierQuotation)
                    .ThenInclude(s => s.Supplier)
            .Where(s =>
                s.ReceivedQuotation
                && s.Items.Any(i =>
                    i.Status == SupplierQuotationItemStatus.NotProcessed
                    && i.SupplierQuotation.Supplier.Type == type
                )
            )
            .ToListAsync();

        foreach (var supplierQuotation in supplierQuotations)
        {
            foreach (
                var item in supplierQuotation.Items.Where(item =>
                    item.Status == SupplierQuotationItemStatus.NotProcessed
                )
            )
            {
                item.Status = SupplierQuotationItemStatus.NotUsed;
            }
        }

        context.SupplierQuotations.UpdateRange(supplierQuotations);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    private async Task<string> GeneratePurchaseOrderCode()
    {
        // Fetch the configuration for PurchaseOrder
        var config = await context.Configurations.FirstOrDefaultAsync(c =>
            c.ModelType == nameof(PurchaseOrder)
        );
        if (config is null)
            throw new Exception("No configuration exists");

        var seriesCount = await configurationRepository.GetCountForCodeConfiguration(
            nameof(PurchaseOrder),
            config.Prefix
        );
        return seriesCount.IsFailure
            ? throw new Exception("No configuration exists")
            : CodeGenerator.GenerateCode(config, seriesCount.Value);
    }
}
