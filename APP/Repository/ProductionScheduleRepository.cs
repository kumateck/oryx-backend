using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.BinCards;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.ProductionSchedules;
using DOMAIN.Entities.ProductionSchedules.Packing;
using DOMAIN.Entities.ProductionSchedules.StockTransfers;
using DOMAIN.Entities.ProductionSchedules.StockTransfers.Request;
using DOMAIN.Entities.Products;
using DOMAIN.Entities.Products.Production;
using DOMAIN.Entities.Requisitions;
using DOMAIN.Entities.Users;
using DOMAIN.Entities.Warehouses;
using INFRASTRUCTURE.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public class ProductionScheduleRepository(ApplicationDbContext context, IMapper mapper, UserManager<User> userManager, IMaterialRepository materialRepository) 
    : IProductionScheduleRepository
{
    public async Task<Result<Guid>> CreateProductionSchedule(CreateProductionScheduleRequest request, Guid userId) 
    { 
        var productionSchedule = mapper.Map<ProductionSchedule>(request); 
        productionSchedule.CreatedById = userId;
        await context.ProductionSchedules.AddAsync(productionSchedule); 
        await context.SaveChangesAsync();
        
        return productionSchedule.Id;
    }

    public async Task<Result<ProductionScheduleDto>> GetProductionSchedule(Guid scheduleId) 
    { 
        var productionSchedule = await context.ProductionSchedules
            .Include(s => s.Products).ThenInclude(s => s.Product)
            .FirstOrDefaultAsync(s => s.Id == scheduleId);

        return productionSchedule is null ? Error.NotFound("ProductionSchedule.NotFound", "Production schedule is not found") : mapper.Map<ProductionScheduleDto>(productionSchedule);
    }
    
    public async Task<Result<List<ProductionScheduleProcurementDto>>> GetProductionScheduleDetail(Guid scheduleId, Guid userId)
    {
        // Fetch the production schedule with related data
        var productionSchedule = await context.ProductionSchedules
            .Include(s => s.Products).ThenInclude(s => s.Product)
            .FirstOrDefaultAsync(s => s.Id == scheduleId);

        if (productionSchedule is null)
            return Error.NotFound("ProductionSchedule.NotFound", "Production schedule is not found");

        // Fetch the user with related department data
        var user = await context.Users.Include(user => user.Department)
            .ThenInclude(u => u.Warehouses)
            .FirstOrDefaultAsync(u => u.Id == userId);
        
        if (user is null)
            return Error.NotFound("User.NotFound", $"User with id {userId} not found");


        return new List<ProductionScheduleProcurementDto>();
    }
    

    public async Task<Result<Paginateable<IEnumerable<ProductionScheduleDto>>>> GetProductionSchedules(int page, int pageSize, string searchQuery) 
    { 
        var query = context.ProductionSchedules
            .Include(s => s.Products).ThenInclude(s => s.Product)
            .AsQueryable();
        
        return await PaginationHelper.GetPaginatedResultAsync(
            query, 
            page, 
            pageSize, 
            mapper.Map<ProductionScheduleDto>);
    }
    
    public async Task<Result> UpdateProductionSchedule(UpdateProductionScheduleRequest request, Guid scheduleId, Guid userId) 
    { 
        var existingSchedule = await context.ProductionSchedules
            .FirstOrDefaultAsync(s => s.Id == scheduleId);

        if (existingSchedule is null) 
        { 
            return Error.NotFound("ProductionSchedule.NotFound", "Production schedule is not found");
        }
        
        mapper.Map(request, existingSchedule);
        existingSchedule.LastUpdatedById = userId;
        
        context.ProductionSchedules.Update(existingSchedule); 
        await context.SaveChangesAsync(); 
        return Result.Success();
    }
    
    public async Task<Result> DeleteProductionSchedule(Guid scheduleId, Guid userId) 
    { 
        var schedule = await context.ProductionSchedules.FirstOrDefaultAsync(s => s.Id == scheduleId); 
        if (schedule is null) 
        { 
            return Error.NotFound("ProductionSchedule.NotFound", "Production schedule is not found");
        }
        
        schedule.DeletedAt = DateTime.UtcNow; 
        schedule.LastDeletedById = userId; 
        context.ProductionSchedules.Update(schedule); 
        await context.SaveChangesAsync(); 
        return Result.Success();
    }

    public async Task<Result<Guid>> StartProductionActivity(Guid productionScheduleId, Guid productId, Guid userId)
    {

        if (context.ProductionActivities.Any(p =>
                p.ProductionScheduleId == productionScheduleId && p.ProductId == productId))
        {
            return Error.NotFound("ProductionActivity.AlreadyExist", "A production activity already exists for this product and schedule");
        }
        
        var productionSchedule =
            await context.ProductionSchedules.Include(productionSchedule => productionSchedule.Products).FirstOrDefaultAsync(p => p.Id == productionScheduleId);
        
        var product = await context.Products.Include(product => product.Routes).ThenInclude(route => route.Resources)
            .Include(product => product.Routes).ThenInclude(route => route.WorkCenters)
            .Include(product => product.Routes).ThenInclude(route => route.ResponsibleUsers)
            .Include(product => product.Routes).ThenInclude(route => route.ResponsibleRoles).AsSplitQuery().FirstOrDefaultAsync(p => p.Id == productId);
        
        
        if(productionSchedule is null)
            return Error.NotFound("ProductionSchedule.NotFound", "Production schedule is not found");

        if(product is null)
            return Error.NotFound("Product.Validation", "Product was not found");

        if (product.Routes.Count == 0)
            return Error.Validation("Product.Validation", "This product has no procedures defined hence a production activity cannot commence.");

        if (productionSchedule.Products.All(p => p.ProductId != productId))
            return Error.Validation("Product.Validation", "This product is not associated with this production scheduled");

        var users = product.Routes.SelectMany(r => r.ResponsibleUsers).Select(r => r.User).ToList();

        var roles = product.Routes.SelectMany(r => r.ResponsibleRoles).Select(r => r.Role).ToList();

        var usersInRole = new List<User>();

        foreach (var role in roles)
        {
            usersInRole.AddRange(await userManager.GetUsersInRoleAsync(role?.Name ?? ""));
        }

        var totalUsers = users.Concat(usersInRole).Distinct().ToList();

        if (totalUsers.Count == 0)
            return Error.Validation("Product.Validation", "This product has no users associated for procedures defined hence a production activity cannot commence.");
        
        var quantity = productionSchedule.Products.First(p => p.ProductId == productId).Quantity;

        await FreezeMaterialInProduction(productionScheduleId, productId,  userId);
        
        var activity = new ProductionActivity
        {
            ProductId = productId,
            ProductionScheduleId = productionScheduleId,
            Code = Guid.NewGuid().ToString(),
            StartedAt = DateTime.UtcNow,
            Steps = product.Routes.Select(r => new ProductionActivityStep
            {
                OperationId = r.OperationId,
                WorkflowId = r.WorkflowId,
                Order = r.Order,
                Resources = r.Resources.Select(re => new ProductionActivityStepResource
                {
                    ResourceId = re.ResourceId
                }).ToList(),
                WorkCenters = r.WorkCenters.Select(re => new ProductionActivityStepWorkCenter
                {
                    WorkCenterId = re.WorkCenterId
                }).ToList(),
                ResponsibleUsers = totalUsers.Select(u => new ProductionActivityStepUser
                {
                    UserId = u.Id
                }).ToList(),
            }).ToList(),
            ActivityLogs =
            [
                new ProductionActivityLog
                {
                    Message = "Production activity started.",
                    UserId = userId, 
                    Timestamp = DateTime.UtcNow
                }
            ]
        };

        await context.ProductionActivities.AddAsync(activity);
        await context.SaveChangesAsync();
        await CreateBatchManufacturingRecord(new CreateBatchManufacturingRecord
        {
            ProductId = productId,
            ProductionScheduleId = productionScheduleId,
            ProductionActivityStepId = activity.Steps.OrderBy(s => s.Order).First().Id,
            BatchQuantity = quantity
        });
        await CreateBatchPackagingRecord(new CreateBatchPackagingRecord
        {
            ProductId = productId,
            ProductionScheduleId = productionScheduleId,
            ProductionActivityStepId = activity.Steps.OrderBy(s => s.Order).First().Id,
            BatchQuantity = quantity
        });
        
        return activity.Id;
    }

    public async Task<Result> UpdateStatusOfProductionActivityStep(Guid productionStepId, ProductionStatus status, Guid userId)
    {
        var activityStep = await context.ProductionActivitySteps
            .AsSplitQuery()
            .Include(productionActivityStep => productionActivityStep.ResponsibleUsers)
            .Include(productionActivityStep => productionActivityStep.ProductionActivity)
            .Include(productionActivityStep => productionActivityStep.Operation).FirstOrDefaultAsync(p => p.Id == productionStepId);
        if(activityStep is null)
            return Error.NotFound("ProductActivity.NotFound", "Activity step was not found");

        if (activityStep.ResponsibleUsers.All(u => u.UserId != userId))
            return Error.Validation("ProductActivity.Validation", "You are not responsible for changing the status of this activity");
        
        activityStep.Status = status;

        switch (status)
        {
            case ProductionStatus.InProgress:
                activityStep.StartedAt = DateTime.UtcNow;
                
                bool isFirstStep = await context.ProductionActivitySteps
                    .Where(s => s.ProductionActivityId == activityStep.ProductionActivityId)
                    .OrderBy(s => s.Order)
                    .Select(s => s.Id)
                    .FirstOrDefaultAsync() == productionStepId;

                if (isFirstStep)
                {
                    activityStep.ProductionActivity.Status = ProductionStatus.InProgress;
                }
                
                if (activityStep.Operation.Name == "Production Preparation")
                {
                    var productionActivity = activityStep.ProductionActivity;
                    var product = await context.Products.IgnoreQueryFilters()
                        .AsSplitQuery()
                       .FirstOrDefaultAsync(p => p.Id == productionActivity.ProductId);
                    
                    if (product is not null)
                    {
                        var productionWarehouse = await context.Warehouses
                            .IgnoreQueryFilters()
                            .FirstOrDefaultAsync(w => w.DepartmentId == product.DepartmentId && w.Type == WarehouseType.Production);

                        if (productionWarehouse is not null)
                        {
                            var stockRequisitions = await context.Requisitions
                                .AsSplitQuery()
                                .Include(r => r.Items)
                                .Where(r => r.ProductionActivityStepId == productionStepId).ToListAsync();
                            

                            foreach (var stockRequisition in stockRequisitions)
                            {
                                foreach (var item in stockRequisition.Items)
                                {
                                    var batchesToConsume =
                                        await materialRepository.GetReservedBatchesAndQuantityForProductionWarehouse(item.MaterialId,
                                            productionWarehouse.Id, stockRequisition.ProductionScheduleId, stockRequisition.ProductId);

                                    foreach (var batch in batchesToConsume)
                                    {
                                        await materialRepository.ConsumeMaterialAtLocation(batch.MaterialBatchId, productionWarehouse.Id, batch.Quantity, userId);
                                    }
                                    
                                    context.MaterialBatchReservedQuantities.RemoveRange(batchesToConsume);
                                    await context.SaveChangesAsync();
                                }
                            }
                        }
                        else
                        {
                            return Error.Validation("Production.Consumption",
                                "Unable to consume materials on production floor because production floor for department cant be found");
                        }
                    }
                    else
                    {
                        return ProductErrors.NotFound(productionActivity.ProductId);
                    }
                }
                break;
            
            case ProductionStatus.Completed:
                activityStep.CompletedAt = DateTime.UtcNow;
                

                bool isLastStep = await context.ProductionActivitySteps
                    .Where(s => s.ProductionActivityId == activityStep.ProductionActivityId)
                    .OrderByDescending(s => s.Order)
                    .Select(s => s.Id)
                    .FirstOrDefaultAsync() == productionStepId;

                if (isLastStep)
                {
                    activityStep.ProductionActivity.CompletedAt = DateTime.UtcNow;
                    activityStep.ProductionActivity.Status = ProductionStatus.Completed;
                }
                break;
        }
        
        // 🔹 Add Activity Log Entry
        var logMessage = $"Step {activityStep.Order} status changed to {status.ToString()}.";
        activityStep.ProductionActivity.ActivityLogs.Add(new ProductionActivityLog
        {
            Message = logMessage,
            UserId = userId,
            Timestamp = DateTime.UtcNow
        });

        context.ProductionActivitySteps.Update(activityStep);
        await context.SaveChangesAsync();
        return Result.Success();
    }
    
    public async Task<Result<Paginateable<IEnumerable<ProductionActivityListDto>>>> GetProductionActivities(ProductionFilter filter)
    {
        var query = context.ProductionActivities
            .AsSplitQuery()
            .Include(pa => pa.ProductionSchedule)
            .Include(pa => pa.Product)
            .Include(pa => pa.Steps).ThenInclude(step => step.ResponsibleUsers)
            .Include(pa => pa.Steps).ThenInclude(step => step.Resources)
            .Include(pa => pa.Steps).ThenInclude(step => step.WorkCenters)
            .Include(pa => pa.Steps).ThenInclude(step => step.WorkFlow)
            .Include(pa => pa.Steps).ThenInclude(step => step.Operation)
            .AsQueryable();

        if (filter.UserIds.Count != 0)
        {
            query = query.Where(pa => pa.Steps.Any(step => step.ResponsibleUsers.Any(ru => filter.UserIds.Contains(ru.UserId))));
        }

        if (filter.Status.HasValue)
        {
            query = query.Where(pa => pa.Status == filter.Status);
        }

        return await PaginationHelper.GetPaginatedResultAsync(
            query, 
            filter,
            mapper.Map<ProductionActivityListDto>
        );
    }
    
    public async Task<Result<ProductionActivityDto>> GetProductionActivityById(Guid productionActivityId)
    {
        var productionActivity = await context.ProductionActivities
            .AsSplitQuery()
            .Include(pa => pa.ProductionSchedule)
            .Include(pa => pa.Product)
            .Include(pa => pa.Steps.OrderBy(p => p.Order))
            .Include(pa => pa.Steps).ThenInclude(step => step.ResponsibleUsers)
            .Include(pa => pa.Steps).ThenInclude(step => step.Resources)
            .Include(pa => pa.Steps).ThenInclude(step => step.WorkCenters)
            .Include(pa => pa.Steps).ThenInclude(step => step.WorkFlow)
            .Include(pa => pa.Steps).ThenInclude(step => step.Operation)
            .Include(pa => pa.ActivityLogs).ThenInclude(a => a.User)
            .FirstOrDefaultAsync(pa => pa.Id == productionActivityId);

        if (productionActivity is null)
            return Error.NotFound("ProductionActivity.NotFound", "Production activity not found");

        return Result.Success(mapper.Map<ProductionActivityDto>(productionActivity));
    }
    
    public async Task<Result<ProductionActivityDto>> GetProductionActivityByProductionScheduleIdAndProductId(Guid productionScheduleId, Guid productId)
    {
        var productionActivity = await context.ProductionActivities
            .AsSplitQuery()
            .Include(pa => pa.ProductionSchedule)
            .Include(pa => pa.Product)
            .Include(pa => pa.Steps.OrderBy(p => p.Order))
            .Include(pa => pa.Steps).ThenInclude(step => step.ResponsibleUsers)
            .Include(pa => pa.Steps).ThenInclude(step => step.Resources)
            .Include(pa => pa.Steps).ThenInclude(step => step.WorkCenters)
            .Include(pa => pa.Steps).ThenInclude(step => step.WorkFlow)
            .Include(pa => pa.Steps).ThenInclude(step => step.Operation)
            .FirstOrDefaultAsync(pa => pa.ProductionScheduleId == productionScheduleId && pa.ProductId == productId);

        return Result.Success(mapper.Map<ProductionActivityDto>(productionActivity));
    }
    
    public async Task<Result<Paginateable<IEnumerable<ProductionActivityStepDto>>>> GetProductionActivitySteps(ProductionFilter filter)
    {
        var query = context.ProductionActivitySteps
            .Include(pas => pas.ProductionActivity)
            .Include(pas => pas.ResponsibleUsers)
            .Include(pas => pas.Resources)
            .Include(pas => pas.WorkCenters)
            .Include(psa => psa.Operation)
            .AsQueryable();

        if (filter.UserIds.Count != 0)
        {
            query = query.Where(pas => pas.ResponsibleUsers.Any(ru => filter.UserIds.Contains(ru.UserId)));
        }

        if (filter.Status.HasValue)
        {
            query = query.Where(pa => pa.Status == filter.Status);
        }

        return await PaginationHelper.GetPaginatedResultAsync(
            query, 
            filter,
            mapper.Map<ProductionActivityStepDto>
        );
    }
    
    public async Task<Result<ProductionActivityStepDto>> GetProductionActivityStepById(Guid productionActivityStepId)
    {
        var productionActivityStep = await context.ProductionActivitySteps
            .AsSplitQuery()
            .Include(pas => pas.ProductionActivity)
            .Include(pas => pas.ResponsibleUsers)
            .Include(pas => pas.Resources)
            .Include(pas => pas.WorkCenters)
            .Include(pas => pas.WorkFlow)
            .Include(psa => psa.Operation)
            .FirstOrDefaultAsync(pas => pas.Id == productionActivityStepId);

        if (productionActivityStep is null)
            return Error.NotFound("ProductionActivityStep.NotFound", "Production activity step not found");

        return Result.Success(mapper.Map<ProductionActivityStepDto>(productionActivityStep));
    }
    
    public async Task<Result<Dictionary<string, List<ProductionActivityDto>>>> GetProductionActivityGroupedByStatus()
    {
        var groupedData = await context.ProductionActivities
            .Include(pa => pa.ProductionSchedule)
            .Include(pa => pa.Product)
            .Include(pa => pa.Steps.OrderBy(p => p.Order))
            .Include(pa => pa.Steps).ThenInclude(step => step.ResponsibleUsers)
            .Include(pa => pa.Steps).ThenInclude(step => step.Resources)
            .Include(pa => pa.Steps).ThenInclude(step => step.WorkCenters)
            .Include(pa => pa.Steps).ThenInclude(step => step.WorkFlow)
            .Include(pa => pa.Steps).ThenInclude(step => step.Operation)
            .GroupBy(pas => pas.Status)
            .ToDictionaryAsync(
                g => g.Key.ToString(),
                g => g.Select(mapper.Map<ProductionActivityDto>).ToList()
            );

        return groupedData;
    }
    
     public async Task<Result<List<ProductionActivityGroupResultDto>>> GetProductionActivityGroupedByOperation()
    {
        // Fetch all unique operation names in the correct order
        var allOperations = await context.Operations
            .OrderBy(o => o.Order) 
            .Select(o => new CollectionItemDto { Id = o.Id, Name = o.Name })
            .AsNoTracking()
            .ToListAsync();

        // Fetch production activities with only necessary data
        var productionActivities = await context.ProductionActivities
            .AsSplitQuery()
            .Include(pa => pa.ProductionSchedule).ThenInclude(productionSchedule => productionSchedule.Products)
            .Include(pa => pa.Product)
            .Include(pa => pa.Steps)
                .ThenInclude(s => s.Operation) 
            .Include(pa => pa.Steps)
                .ThenInclude(s => s.ResponsibleUsers) 
            .AsNoTracking()
            .ToListAsync();

        // Process CurrentStep in memory
        var productionActivityDtos = productionActivities
            .Select(pa => new ProductionActivityGroupDto
            {
                Id = pa.Id,
                CreatedAt = pa.CreatedAt,
                ProductionSchedule = mapper.Map<CollectionItemDto>(pa.ProductionSchedule),
                Product = mapper.Map<CollectionItemDto>(pa.Product),
                Status = pa.Status,
                StartedAt = pa.StartedAt,
                CompletedAt = pa.CompletedAt,
                BatchNumber = pa.ProductionSchedule.Products.First(p => p.Id == pa.ProductId).BatchNumber,
                CurrentStep = mapper.Map<ProductionActivityStepDto>(
                    pa.Steps
                        .OrderBy(s => s.Order)
                        .FirstOrDefault(s => !s.CompletedAt.HasValue) ?? // First unfinished step
                    pa.Steps.OrderBy(s => s.Order).LastOrDefault() // Fallback: last step
                )
            })
            .Where(p => p.CurrentStep?.Operation != null) // Ensure CurrentStep has an operation
            .ToList();

        // Group activities by operation
        var groupedActivities = productionActivityDtos
            .GroupBy(p => new CollectionItemDto { Id = p.CurrentStep.Operation.Id, Name = p.CurrentStep.Operation.Name })
            .ToList();

        // Construct response list
        var result = allOperations
            .Select(op => new ProductionActivityGroupResultDto
            {
                Operation = op,
                Activities = groupedActivities.FirstOrDefault(g => g.Key.Id == op.Id)?.ToList() ?? []
            })
            .ToList();

        return result;
    }
    
    public async Task<Result<Dictionary<string, List<ProductionActivityStepDto>>>> GetProductionActivityStepsGroupedByStatus()
    {
        var groupedData = await context.ProductionActivitySteps
            .Include(pas => pas.ProductionActivity)
            .Include(pas => pas.ResponsibleUsers)
            .Include(pas => pas.Resources)
            .Include(pas => pas.WorkCenters)
            .Include(pas => pas.WorkFlow)
            .Include(pas => pas.Operation)
            .GroupBy(pas => pas.Status)
            .ToDictionaryAsync(
                g => g.Key.ToString(),
                g => g.Select(mapper.Map<ProductionActivityStepDto>).ToList()
            );

        return groupedData;
    }
    
    public async Task<Result<Dictionary<string, List<ProductionActivityStepDto>>>> GetProductionActivityStepsGroupedByOperation()
    {
        // Retrieve all operations
        var allOperations = await context.Operations
            .Select(o => o.Name)
            .Where(name => name != "Dispatch")
            .ToListAsync();

        // Retrieve existing grouped ProductionActivitySteps
        var groupedData = await context.ProductionActivitySteps
            .Include(pas => pas.ProductionActivity)
            .Include(pas => pas.ResponsibleUsers)
            .Include(pas => pas.Resources)
            .Include(pas => pas.WorkCenters)
            .Include(pas => pas.WorkFlow)
            .Include(pas => pas.Operation)
            .GroupBy(pas => pas.Operation.Name)
            .ToDictionaryAsync(
                g => g.Key,
                g => g.Select(mapper.Map<ProductionActivityStepDto>).ToList()
            );

        // Ensure all operations are included in the dictionary, even if empty
        var result = allOperations.ToDictionary(
            op => op,
            op => groupedData.TryGetValue(op, out var value) ? value : []
        );

        return result;
    }


    public async Task<Result<List<ProductionScheduleProcurementDto>>> CheckMaterialStockLevelsForProductionSchedule(Guid productionScheduleId, Guid productId, MaterialRequisitionStatus? status, Guid userId)
    {
        var product = await context.Products
            .AsSplitQuery()
            .IgnoreAutoIncludes()
            .Include(product => product.BillOfMaterials)
            .ThenInclude(p => p.BillOfMaterial)
            .ThenInclude(p => p.Items).ThenInclude(billOfMaterialItem => billOfMaterialItem.Material)
            .Include(product => product.BillOfMaterials)
            .ThenInclude(productBillOfMaterial => productBillOfMaterial.BillOfMaterial)
            .ThenInclude(billOfMaterial => billOfMaterial.Items)
            .ThenInclude(billOfMaterialItem => billOfMaterialItem.BaseUoM)
            .FirstOrDefaultAsync(p => p.Id == productId);
        
        if (product is null)
            return ProductErrors.NotFound(productId);

        var productionSchedule =
            await context.ProductionSchedules.AsSplitQuery().Include(productionSchedule => productionSchedule.Products).FirstOrDefaultAsync(p => p.Id == productionScheduleId);
        if(productionSchedule is null)
            return ProductErrors.NotFound(productionScheduleId);
        
        if (productionSchedule.Products.All(p => p.ProductId != productId))
            return Error.Validation("Product.Validation", "This product is not associated with this production scheduled");

        var quantityRequired = productionSchedule.Products.First(p => p.ProductId == productId).Quantity;

        var activeBoM = product.BillOfMaterials
            .OrderByDescending(p => p.EffectiveDate)
            .FirstOrDefault(p => p.IsActive);

        if (activeBoM is null)
            return Error.NotFound("Product.BoM", "No active bom found for this product");

        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null)
            return UserErrors.NotFound(userId);
        
        var stockLevels = new Dictionary<Guid, decimal>();
        if (user.Department == null)
            return Error.NotFound("User.Department", "User has no association to any department");
        
        if(user.Department.Warehouses.Count == 0)
            return Error.NotFound("User.Warehouse", "No raw material warehouse is associated with current user");
        
        var warehouse = user.Department.Warehouses.FirstOrDefault(i => i.Type == WarehouseType.RawMaterialStorage);
        if (warehouse is null)
            return Error.NotFound("User.Warehouse", "No raw material warehouse is associated with current user");
        
        var productionWarehouse = user.Department.Warehouses.FirstOrDefault(i => i.Type == WarehouseType.Production);
        if (productionWarehouse is null)
            return Error.NotFound("User.Warehouse", "No production warehouse is associated with current user");

        var sourceRequisitionItems = new List<SourceRequisitionItem>();

        var stockTransfers = await context.StockTransfers.Where(s =>
            s.ProductId == productId && s.ProductionScheduleId == productionScheduleId)
            .ToListAsync();

        var stockRequisition = await context.Requisitions.Include(requisition => requisition.Items).FirstOrDefaultAsync(r =>
            r.ProductId == productId && r.ProductionScheduleId == productionScheduleId && r.RequisitionType == RequisitionType.Stock);
        
        var purchaseRequisition = await context.Requisitions.Include(requisition => requisition.Items).Where(r =>
            r.ProductId == productId && r.ProductionScheduleId == productionScheduleId && r.RequisitionType == RequisitionType.Purchase).ToListAsync();
        
        if (purchaseRequisition.Count != 0)
        {
            sourceRequisitionItems = await context.SourceRequisitionItems
                .Where(sr => purchaseRequisition.Select(pr => pr.Id).Contains(sr.RequisitionId))
                .ToListAsync();
        }
        
        // Fetch stock levels for each material ID individually
        foreach (var materialId in activeBoM.BillOfMaterial.Items.Select(item => item.MaterialId).Distinct())
        {
            var stockLevel = await materialRepository.GetMaterialStockInWarehouse(materialId, warehouse.Id);
            stockLevels[materialId] = stockLevels.GetValueOrDefault(materialId, 0) + stockLevel.Value;
        }
        
        var materialDetails = activeBoM.BillOfMaterial.Items.Select(item =>
        {
            var quantityOnHand = stockLevels.GetValueOrDefault(item.MaterialId, 0);
            var quantityNeeded =
                CalculateRequiredItemQuantity(quantityRequired, item.BaseQuantity, product.BaseQuantity);

            return new ProductionScheduleProcurementDto
            {
                Material = mapper.Map<MaterialDto>(item.Material),
                BaseUoM = mapper.Map<UnitOfMeasureDto>(item.BaseUoM),
                BaseQuantity = item.BaseQuantity,
                QuantityNeeded = quantityNeeded,
                QuantityOnHand = quantityOnHand,
                Status = quantityOnHand >= quantityNeeded ? MaterialRequisitionStatus.InHouse : GetStatusOfProductionMaterial(stockTransfers, stockRequisition?.Items ?? [], purchaseRequisition.SelectMany(p => p.Items).ToList(),  sourceRequisitionItems, item.MaterialId),
                StorageWarehouseId = warehouse.Id,
                ProductionWarehouseId = productionWarehouse.Id
            };
        }).ToList();

        if (status.HasValue)
        {
            materialDetails = materialDetails.Where(m => m.Status == status).ToList();
        }

        return materialDetails;
    }
    
    private MaterialRequisitionStatus GetStatusOfProductionMaterial(List<StockTransfer> stockTransfers,
        List<RequisitionItem> stockRequisitionItems, List<RequisitionItem> purchaseRequisitionItems, List<SourceRequisitionItem> sourceRequisitionItems, Guid materialId)
    {
        if (stockRequisitionItems.Count != 0 && stockRequisitionItems.Any(r => r.MaterialId == materialId))
        {
            return stockRequisitionItems.First(r => r.MaterialId == materialId).Requisition.Approved
                ? MaterialRequisitionStatus.Issued
                : MaterialRequisitionStatus.StockRequisition;
        }
        
        if (stockTransfers.Count != 0 && stockTransfers.Any(r => r.MaterialId == materialId))
            return MaterialRequisitionStatus.StockTransfer;
        
        if (sourceRequisitionItems.Count != 0 && sourceRequisitionItems.Any(s => s.MaterialId == materialId))
            return sourceRequisitionItems.First(s => s.MaterialId == materialId).Source == ProcurementSource.Foreign
                ? MaterialRequisitionStatus.Foreign
                : MaterialRequisitionStatus.Local;

        if (purchaseRequisitionItems.Count != 0 && purchaseRequisitionItems.Any(r => r.MaterialId == materialId))
            return MaterialRequisitionStatus.PurchaseRequisition;

        return MaterialRequisitionStatus.None;
    }
    
    public async Task<Result<List<ProductionScheduleProcurementPackageDto>>> CheckPackageMaterialStockLevelsForProductionSchedule(Guid productionScheduleId, Guid productId, MaterialRequisitionStatus? status, Guid userId)
    {
        var product = await context.Products.Include(product => product.BillOfMaterials)
            .Include(product => product.Packages).ThenInclude(productPackage => productPackage.BaseUoM)
            .Include(product => product.Packages).ThenInclude(productPackage => productPackage.DirectLinkMaterial)
            .AsSplitQuery()
            .FirstOrDefaultAsync(p => p.Id == productId);
        if (product is null)
            return ProductErrors.NotFound(productId);
        
        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null)
            return UserErrors.NotFound(userId);
        
        var productionSchedule =
            await context.ProductionSchedules.Include(productionSchedule => productionSchedule.Products).FirstOrDefaultAsync(p => p.Id == productionScheduleId);
        if(productionSchedule is null)
            return ProductErrors.NotFound(productionScheduleId);
        
        if (productionSchedule.Products.All(p => p.ProductId != productId))
            return Error.Validation("Product.Validation", "This product is not associated with this production scheduled");

        var quantityRequired = productionSchedule.Products.First(p => p.ProductId == productId).Quantity;
        
        var stockLevels = new Dictionary<Guid, decimal>();
        if (user.Department == null)
            return Error.NotFound("User.Department", "User has no association to any department");
        
        if(user.Department.Warehouses.Count == 0)
            return Error.NotFound("User.Warehouse", "No package material warehouse is associated with current user");
        
        var warehouse = user.Department.Warehouses.FirstOrDefault(i => i.Type == WarehouseType.PackagedStorage);
        if (warehouse is null)
            return Error.NotFound("User.Warehouse", "No package material warehouse is associated with current user");
        
        var productionWarehouse = user.Department.Warehouses.FirstOrDefault(i => i.Type == WarehouseType.Production);
        if (productionWarehouse is null)
            return Error.NotFound("User.Warehouse", "No production warehouse is associated with current user");
        
        var sourceRequisitionItems = new List<SourceRequisitionItem>();

        var stockTransfers = await context.StockTransfers.Where(s =>
                s.ProductId == productId && s.ProductionScheduleId == productionScheduleId)
            .ToListAsync();

        var stockRequisition = await context.Requisitions.Include(requisition => requisition.Items).FirstOrDefaultAsync(r =>
            r.ProductId == productId && r.ProductionScheduleId == productionScheduleId && r.RequisitionType == RequisitionType.Stock);
        
        var purchaseRequisition = await context.Requisitions.Include(requisition => requisition.Items).Where(r =>
            r.ProductId == productId && r.ProductionScheduleId == productionScheduleId && r.RequisitionType == RequisitionType.Purchase).ToListAsync();
        
        if (purchaseRequisition.Count != 0)
        {
            sourceRequisitionItems = await context.SourceRequisitionItems
                .Where(sr => purchaseRequisition.Select(pr => pr.Id).Contains(sr.RequisitionId))
                .ToListAsync();
        }
        
        foreach (var materialId in product.Packages.Select(item => item.MaterialId).Distinct())
        {
            var stockLevel = await materialRepository.GetMaterialStockInWarehouse(materialId, warehouse.Id);
            stockLevels[materialId] = stockLevels.GetValueOrDefault(materialId, 0) + stockLevel.Value;
        }
        
        var materialDetails = product.Packages.Select(item =>
        {
            var quantityOnHand = stockLevels.GetValueOrDefault(item.MaterialId, 0);
            var quantityNeeded = Math.Floor(GetQuantityNeeded(item, product.Packages.ToList(), quantityRequired,
                product.BasePackingQuantity));

            return new ProductionScheduleProcurementPackageDto
            {
                Material = mapper.Map<MaterialDto>(item.Material),
                DirectLinkMaterial = mapper.Map<MaterialDto>(item.DirectLinkMaterial),
                BaseUoM = mapper.Map<UnitOfMeasureDto>(product.BasePackingUoM),
                BaseQuantity = item.BaseQuantity,
                UnitCapacity = item.UnitCapacity,
                Status = quantityOnHand >= quantityNeeded ? MaterialRequisitionStatus.InHouse : GetStatusOfProductionMaterial(stockTransfers, stockRequisition?.Items ?? [], purchaseRequisition.SelectMany(p => p.Items).ToList(),  sourceRequisitionItems, item.MaterialId),
                QuantityNeeded = quantityNeeded,
                QuantityOnHand = quantityOnHand,
                PackingExcessMargin = item.PackingExcessMargin,
                StorageWarehouseId = warehouse.Id,
                ProductionWarehouseId = productionWarehouse.Id
            };
        }).ToList();
        
        if (status.HasValue)
        {
            materialDetails = materialDetails.Where(m => m.Status == status).ToList();
        }

        return materialDetails;
    }
    
    private static decimal CalculateRequiredItemQuantity(decimal targetProductQuantity, decimal itemBaseQuantity, decimal productBaseQuantity)
    {
        return Math.Round(targetProductQuantity * itemBaseQuantity / productBaseQuantity, 2);
    }
    
    private decimal GetQuantityNeeded(ProductPackage item, List<ProductPackage> allPackages, decimal quantityRequired, decimal basePackingQuantity, HashSet<Guid> visitedMaterials = null)
    {
        visitedMaterials ??= [];

        // Check for circular reference
        if (!visitedMaterials.Add(item.MaterialId))
        {
            throw new InvalidOperationException($"Circular reference detected for MaterialId: {item.MaterialId}");
        }

        if (!item.DirectLinkMaterialId.HasValue)
        {
            return CalculateRequiredItemQuantity(quantityRequired, item.BaseQuantity, basePackingQuantity);
        }

        var linkedPackage = allPackages.FirstOrDefault(p => p.MaterialId == item.DirectLinkMaterialId);

        if (linkedPackage is null)
        {
            return CalculateRequiredItemQuantity(quantityRequired, item.BaseQuantity, basePackingQuantity);
        }

        if (!linkedPackage.DirectLinkMaterialId.HasValue)
        {
            return CalculateRequiredItemQuantity(quantityRequired, linkedPackage.BaseQuantity, basePackingQuantity) / item.UnitCapacity;
        }

        // Recursively calculate, now with a visited set to prevent infinite loops
        return GetQuantityNeeded(linkedPackage, allPackages, quantityRequired, basePackingQuantity, visitedMaterials) / item.UnitCapacity;
    }
    
     public async Task<Result<Guid>> CreateBatchManufacturingRecord(CreateBatchManufacturingRecord request)
    {
        var batchRecord = mapper.Map<BatchManufacturingRecord>(request);
        await context.BatchManufacturingRecords.AddAsync(batchRecord);
        await context.SaveChangesAsync();

        var productionScheduleProduct = await context.ProductionScheduleProducts.FirstOrDefaultAsync(p =>
            p.ProductionScheduleId == request.ProductionScheduleId && p.ProductId == request.ProductId);

        if (productionScheduleProduct is not null)
        {
            productionScheduleProduct.BatchNumber = request.BatchNumber;
            context.ProductionScheduleProducts.Update(productionScheduleProduct);
            await context.SaveChangesAsync();
        }
        return batchRecord.Id;
    }
     
    public async Task<Result<BatchManufacturingRecordDto>> GetBatchManufacturingRecordByProductionAndScheduleId(Guid productionId, Guid productionScheduleId)
    {
        var batchManufacturingRecord = await context.BatchManufacturingRecords
            .Include(b => b.Product)
            .Include(b => b.ProductionSchedule)
            .FirstOrDefaultAsync(b => b.ProductId == productionId && b.ProductionScheduleId == productionScheduleId);
    
        if (batchManufacturingRecord == null)
        {
            return Error.NotFound("BatchManufacturingRecord.NotFound", "Batch manufacturing record not found for the specified production and schedule.");
        }
    
        return Result.Success(mapper.Map<BatchManufacturingRecordDto>(batchManufacturingRecord));
    }
    
    public async Task<Result> CreateFinishedGoodsTransferNote(CreateFinishedGoodsTransferNoteRequest request, Guid userId)
    {
        var bmr = await context.BatchManufacturingRecords
            .AsSplitQuery().Include(batchManufacturingRecord => batchManufacturingRecord.Product)
            .FirstOrDefaultAsync(r => r.Id == request.BatchManufacturingRecordId);
        
        if (bmr is null)
            return RequisitionErrors.NotFound(request.BatchManufacturingRecordId);
        
        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null)
            return UserErrors.NotFound(userId);
        
        if (user.Department == null)
            return Error.NotFound("User.Department", "User has no association to any department");
        
        if(user.Department.Warehouses.Count == 0)
            return Error.NotFound("User.Warehouse", "No raw material warehouse is associated with current user");
        
        var productionWarehouse = user.Department.Warehouses.FirstOrDefault(i => i.Type == WarehouseType.Production);
        if (productionWarehouse is null)
            return Error.NotFound("User.Warehouse", "No production warehouse is associated with current user");
        
        var finishedGoodsWarehouse = user.Department.Warehouses.FirstOrDefault(i => i.Type == WarehouseType.FinishedGoodsStorage);
        if (finishedGoodsWarehouse is null)
            return Error.NotFound("User.Warehouse", "No finished goods warehouse is associated with current user");
        
        var transferNote = mapper.Map<FinishedGoodsTransferNote>(request);
        context.FinishedGoodsTransferNotes.Add(transferNote);

        var movement = new FinishedProductBatchMovement()
        {
            BatchId = bmr.ProductId,
            FromWarehouseId = productionWarehouse.Id,
            ToWarehouseId = finishedGoodsWarehouse.Id,
            Quantity = request.TotalQuantity,
            MovedAt = DateTime.UtcNow,
            MovedById = userId
        };
            
        await context.FinishedProductBatchMovements.AddAsync(movement);
            
        var batchEvent = new FinishedProductBatchEvent
        {
            BatchId = bmr.ProductId,
            Type = EventType.Moved,
            Quantity =request.TotalQuantity,
            UserId = userId
        };
        await context.FinishedProductBatchEvents.AddAsync(batchEvent);
        
        var binCardEvent = new ProductBinCardInformation
        {
            BatchId = bmr.ProductId,
            Description = finishedGoodsWarehouse.Name,
            WayBill = "N/A",
            ArNumber = "N/A",
            QuantityReceived = request.TotalQuantity,
            QuantityIssued = 0,
            BalanceQuantity = (await materialRepository.GetProductStockInWarehouseByBatch(bmr.ProductId, finishedGoodsWarehouse.Id)).Value + request.TotalQuantity,
            UoMId = bmr.Product.BaseUomId,
            CreatedAt = DateTime.UtcNow
        };
        
        await context.ProductBinCardInformation.AddAsync(binCardEvent);

        await context.SaveChangesAsync();
        return Result.Success();
    }
    
    public async Task<Result<Paginateable<IEnumerable<BatchManufacturingRecordDto>>>> GetBatchManufacturingRecords(int page, int pageSize, string searchQuery = null, ProductionStatus? status = null)
    {
        var query = context.BatchManufacturingRecords
            .Include(b => b.CreatedBy)
            .Include(p => p.ProductionActivityStep)
            .Include(p => p.ProductionSchedule)
            .Include(p => p.Product)
            .Where(p => !p.ProductionActivityStep.CompletedAt.HasValue)
            .AsQueryable();

        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.WhereSearch(searchQuery, p => p.BatchNumber, p => p.Product.Name);
        }

        if (status.HasValue)
        {
            query = query.Where(b => b.ProductionActivityStep.Status == status);
        }

        return await PaginationHelper.GetPaginatedResultAsync(
            query,
            page,
            pageSize,
            mapper.Map<BatchManufacturingRecordDto>
        );
    }

    public async Task<Result<BatchManufacturingRecordDto>> GetBatchManufacturingRecord(Guid id)
    {
        return mapper.Map<BatchManufacturingRecordDto>(
            await context.BatchManufacturingRecords
                .Include(b => b.CreatedBy)
                .Include(p => p.ProductionActivityStep)
                .Include(p => p.ProductionSchedule)
                .Include(p => p.Product)
                .FirstOrDefaultAsync(b => b.Id == id));
    }

    public async Task<Result> UpdateBatchManufacturingRecord(UpdateBatchManufacturingRecord request, Guid id)
    {
        var batchRecord = await context.BatchManufacturingRecords
            .Include(batchManufacturingRecord => batchManufacturingRecord.ProductionActivityStep).FirstOrDefaultAsync(p => p.Id == id);
        if (batchRecord is null)
        {
            return ProductErrors.NotFound(id);
        }

        mapper.Map(request, batchRecord);
        batchRecord.ProductionActivityStep.Status = ProductionStatus.InProgress;
        batchRecord.ProductionActivityStep.StartedAt = DateTime.UtcNow;
        context.BatchManufacturingRecords.Update(batchRecord);
        await context.SaveChangesAsync();
        return Result.Success();
    }
    
    public async Task<Result> IssueBatchManufacturingRecord(Guid id, Guid userId)
    {
        var batchRecord = await context.BatchManufacturingRecords
            .Include(batchManufacturingRecord => batchManufacturingRecord.ProductionActivityStep).FirstOrDefaultAsync(p => p.Id == id);
        if (batchRecord is null)
        {
            return ProductErrors.NotFound(id);
        }

        batchRecord.ProductionActivityStep.Status = ProductionStatus.Completed;
        batchRecord.ProductionActivityStep.CompletedAt = DateTime.UtcNow;
        batchRecord.IssuedById = userId;
        context.BatchManufacturingRecords.Update(batchRecord);
        await context.SaveChangesAsync();
        return Result.Success();
    }
    
    public async Task<Result<Guid>> CreateBatchPackagingRecord(CreateBatchPackagingRecord request)
    {
        var batchRecord = mapper.Map<BatchPackagingRecord>(request);
        await context.BatchPackagingRecords.AddAsync(batchRecord);
        await context.SaveChangesAsync();
        return batchRecord.Id;
    }
    
    public async Task<Result<Paginateable<IEnumerable<BatchPackagingRecordDto>>>> GetBatchPackagingRecords(int page, int pageSize, string searchQuery = null, ProductionStatus? status = null)
    {
        var query = context.BatchPackagingRecords
            .Include(b => b.CreatedBy)
            .Include(p => p.ProductionActivityStep)
            .Include(p => p.ProductionSchedule)
            .Include(p => p.Product)
            .AsQueryable();

        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.WhereSearch(searchQuery, p => p.BatchNumber, p => p.Product.Name);
        }
        
        if (status.HasValue)
        {
            query = query.Where(b => b.ProductionActivityStep.Status == status);
        }

        return await PaginationHelper.GetPaginatedResultAsync(
            query,
            page,
            pageSize,
            mapper.Map<BatchPackagingRecordDto>
        );
    }

    public async Task<Result<BatchPackagingRecordDto>> GetBatchPackagingRecord(Guid id)
    {
        return mapper.Map<BatchPackagingRecordDto>(
            await context.BatchPackagingRecords
                .Include(b => b.CreatedBy)
                .Include(p => p.ProductionActivityStep)
                .Include(p => p.ProductionSchedule)
                .Include(p => p.Product)
                .FirstOrDefaultAsync(b => b.Id == id));
    }

    public async Task<Result> UpdateBatchPackagingRecord(UpdateBatchPackagingRecord request, Guid id)
    {
        var batchRecord = await context.BatchPackagingRecords
            .Include(batchPackagingRecord => batchPackagingRecord.ProductionActivityStep).FirstOrDefaultAsync(p => p.Id == id);
        if (batchRecord is null)
        {
            return ProductErrors.NotFound(id);
        }

        mapper.Map(request, batchRecord);
        batchRecord.ProductionActivityStep.Status = ProductionStatus.InProgress;
        context.BatchPackagingRecords.Update(batchRecord);
        await context.SaveChangesAsync();
        return Result.Success();
    }
    
    public async Task<Result> IssueBatchPackagingRecord(Guid id, Guid userId)
    {
        var batchRecord = await context.BatchPackagingRecords
            .Include(batchManufacturingRecord => batchManufacturingRecord.ProductionActivityStep).FirstOrDefaultAsync(p => p.Id == id);
        if (batchRecord is null)
        {
            return ProductErrors.NotFound(id);
        }

        batchRecord.ProductionActivityStep.Status = ProductionStatus.Completed;
        batchRecord.IssuedById = userId;
        context.BatchPackagingRecords.Update(batchRecord);
        await context.SaveChangesAsync();
        return Result.Success();
    }
    
    public async Task FreezeMaterialInProduction(Guid productionScheduleId, Guid productId, Guid userId)
    {
        var materialResult = await CheckMaterialStockLevelsForProductionSchedule(productionScheduleId, productId,null, userId);
        if (materialResult.IsFailure) return;

        var materialDetails = materialResult.Value;

        foreach (var material in materialDetails)
        {
            var batchResult = await materialRepository.BatchesToSupplyForGivenQuantity(material.Material.Id, material.StorageWarehouseId,
                material.QuantityNeeded);

            if (batchResult.IsSuccess)
            {
                var batches = batchResult.Value;
                foreach (var batch in batches)
                {
                    await materialRepository.ReserveQuantityFromBatchForProduction(batch.Batch.Id, material.ProductionWarehouseId, productionScheduleId, productId,
                        batch.QuantityToTake);
                }
            }
        }
        
        var packageMaterialResult = await CheckPackageMaterialStockLevelsForProductionSchedule(productionScheduleId, productId,null, userId);
        if (packageMaterialResult.IsFailure) return;
        
        var packageMaterialDetails = packageMaterialResult.Value;
        
        foreach (var material in packageMaterialDetails)
        {
            var batchResult = await materialRepository.BatchesToSupplyForGivenQuantity(material.Material.Id, material.StorageWarehouseId,
                material.QuantityNeeded);

            if (batchResult.IsSuccess)
            {
                var batches = batchResult.Value;
                foreach (var batch in batches)
                {
                    await materialRepository.ReserveQuantityFromBatchForProduction(batch.Batch.Id, material.ProductionWarehouseId, productionScheduleId, productId,
                        batch.QuantityToTake);
                }
            }
        }
    }
    
     public async Task<Result<Guid>> CreateStockTransfer(CreateStockTransferRequest request, Guid userId)
     {
         var stockTransfer = mapper.Map<StockTransfer>(request);
         if (stockTransfer.Sources.Count == 0)
             return Error.Validation("StockTransfer.Validation", "Stock transfer sources cannot be empty");

         var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId);
         if (user is null)
             return UserErrors.NotFound(userId);

         if (!user.DepartmentId.HasValue)
             return Error.NotFound("User.Department", "User is not associated with any department");
         
         foreach (var source in stockTransfer.Sources)
         {
             source.ToDepartmentId = user.DepartmentId.Value;
         }
         
         await context.StockTransfers.AddAsync(stockTransfer);
         await context.SaveChangesAsync();
         return Result.Success(stockTransfer.Id);
    }

    // Get Stock Transfers
    public async Task<Result<IEnumerable<StockTransferDto>>> GetStockTransfers(Guid? fromDepartmentId = null, Guid? toDepartmentId = null, Guid? materialId = null)
    {
        var query = context.StockTransfers
            .AsSplitQuery()
            .Include(s=>s.UoM)
            .Include(st => st.Sources).ThenInclude(s => s.FromDepartment)
            .Include(st => st.Material)
            .AsQueryable();
        
        if (fromDepartmentId.HasValue)
        {
            query = query.Where(st => st.Sources.Any(s => s.FromDepartmentId == fromDepartmentId.Value));
        }
        
        if (toDepartmentId.HasValue)
        {
            query = query.Where(st => st.Sources.Any(s => s.FromDepartmentId == toDepartmentId.Value));
        }
        
        if (materialId.HasValue)
        {
            query = query.Where(st => st.MaterialId == materialId.Value);
        }
        
        var transfers = await query.ToListAsync();
        return mapper.Map<List<StockTransferDto>>(transfers);
    }
    
    public async Task<Result<Paginateable<IEnumerable<StockTransferDto>>>> GetStockTransfersForUserDepartment(Guid userId, int page, int pageSize, string searchQuery = null, StockTransferStatus? status = null)
    {

        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null)
            return UserErrors.NotFound(userId);
        
        var query = context.StockTransfers
            .Include(st => st.Sources).ThenInclude(s => s.FromDepartment)
            .Include(st => st.Sources).ThenInclude(s => s.ToDepartment)
            .Include(st => st.Material)
            .Where(st => st.Sources.Any(s => s.ToDepartmentId == user.DepartmentId))
            .AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(q => q.Sources.Any(so => so.Status == status));
        }

        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.WhereSearch(searchQuery, q => q.Code);
        }

        return await PaginationHelper.GetPaginatedResultAsync(
            query,
            page,
            pageSize,
            mapper.Map<StockTransferDto>
        );
    }

    public async Task<Result<DepartmentStockTransferDto>> GetStockTransferSource(Guid stockTransferId)
    {
        return mapper.Map<DepartmentStockTransferDto>(
            await context.StockTransferSources
                .Include(s => s.FromDepartment)
                .Include(s => s.ToDepartment)
                .Include(st => st.StockTransfer).ThenInclude(st => st.Material)
                .Include(st => st.StockTransfer).ThenInclude(st => st.UoM)
                .FirstOrDefaultAsync(s => s.Id == stockTransferId));
    }
    
    public async Task<Result<Paginateable<IEnumerable<DepartmentStockTransferDto>>>> GetInBoundStockTransferSourceForUserDepartment(Guid userId, int page, int pageSize, string searchQuery = null, 
        StockTransferStatus? status = null, Guid? toDepartmentId = null)
    {

        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null)
            return UserErrors.NotFound(userId);
        
        var query = context.StockTransferSources
            .Include(s => s.FromDepartment)
            .Include(s => s.ToDepartment)
            .Include(st => st.StockTransfer).ThenInclude(st => st.Material)
            .Include(st => st.StockTransfer).ThenInclude(st => st.UoM)
            .Where(st => st.FromDepartmentId == user.DepartmentId)
            .AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(q => q.Status == status);
        }

        if (toDepartmentId.HasValue)
        {
            query = query.Where(q => q.ToDepartmentId == toDepartmentId);
        }

        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.WhereSearch(searchQuery, q => q.StockTransfer.Code);
        }

        return await PaginationHelper.GetPaginatedResultAsync(
            query,
            page,
            pageSize,
            mapper.Map<DepartmentStockTransferDto>
        );
    }
    
    
    public async Task<Result<Paginateable<IEnumerable<DepartmentStockTransferDto>>>> GetOutBoundStockTransferSourceForUserDepartment(Guid userId, int page, int pageSize, string searchQuery = null, 
        StockTransferStatus? status = null, Guid? fromDepartmentId = null)
    {

        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null)
            return UserErrors.NotFound(userId);
        
        var query = context.StockTransferSources
            .AsSplitQuery()
            .Include(s => s.FromDepartment)
            .Include(s => s.ToDepartment)
            .Include(st => st.StockTransfer).ThenInclude(st => st.Material)
            .Include(st => st.StockTransfer).ThenInclude(st => st.UoM)
            .Where(st => st.ToDepartmentId == user.DepartmentId)
            .AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(q => q.Status == status);
        }

        if (fromDepartmentId.HasValue)
        {
            query = query.Where(q => q.FromDepartmentId == fromDepartmentId);
        }

        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.WhereSearch(searchQuery, q => q.StockTransfer.Code);
        }

        return await PaginationHelper.GetPaginatedResultAsync(
            query,
            page,
            pageSize,
            mapper.Map<DepartmentStockTransferDto>
        );
    }
    
    
    public async Task<Result> ApproveStockTransfer(Guid id, Guid userId)
    {
        var stockTransfer = await context.StockTransferSources
            .FirstOrDefaultAsync(st => st.Id == id);
        
        if (stockTransfer == null)
        {
            return Error.NotFound("StockTransfer.NotFound", "Stock transfer not found");
        }

        stockTransfer.ApprovedAt = DateTime.UtcNow;
        stockTransfer.ApprovedById = userId;
        stockTransfer.Status = StockTransferStatus.Approved;
        context.StockTransferSources.Update(stockTransfer);
        await context.SaveChangesAsync();
        return Result.Success();
    }
    
    public async Task<Result> RejectStockTransfer(Guid id, Guid userId)
    {
        var stockTransfer = await context.StockTransferSources
            .FirstOrDefaultAsync(st => st.Id == id);
        
        if (stockTransfer == null)
        {
            return Error.NotFound("StockTransfer.NotFound", "Stock transfer not found");
        }

        stockTransfer.Status = StockTransferStatus.Rejected;
        context.StockTransferSources.Update(stockTransfer);
        await context.SaveChangesAsync();
        return Result.Success();
    }
    
    public async Task<Result<List<BatchToSupply>>> BatchesToSupplyForStockTransfer(Guid stockTransferId)
    {
        var stockTransferSource = await context.StockTransferSources
            .Include(st => st.StockTransfer).ThenInclude(s => s.Material)
            .FirstOrDefaultAsync(st => st.Id == stockTransferId);
        
        if (stockTransferSource == null)
        {
            return Error.NotFound("StockTransfer.NotFound", "Stock transfer not found");
        }

        var materialKind = stockTransferSource.StockTransfer.Material.Kind;

        var warehouse = materialKind == MaterialKind.Raw
            ? await context.Warehouses.IgnoreQueryFilters()
                .FirstOrDefaultAsync(w =>
                    w.DepartmentId == stockTransferSource.FromDepartmentId && w.Type == WarehouseType.RawMaterialStorage)
            : await context.Warehouses.IgnoreQueryFilters()
                .FirstOrDefaultAsync(
                    w => w.DepartmentId == stockTransferSource.FromDepartmentId && w.Type == WarehouseType.PackagedStorage);

        if (warehouse is null)
            return UserErrors.WarehouseNotFound(materialKind);

        return await materialRepository.BatchesToSupplyForGivenQuantity(stockTransferSource.StockTransfer.MaterialId, warehouse.Id, stockTransferSource.Quantity);
    }

    // Issue Stock Transfer with Batch Selection
    public async Task<Result> IssueStockTransfer(Guid id, List<BatchTransferRequest> batches, Guid userId)
    {
        var stockTransferSource = await context.StockTransferSources
            .Include(st => st.StockTransfer).ThenInclude(st => st.Material)
            .FirstOrDefaultAsync(st => st.Id == id);
        
        if (stockTransferSource == null)
        {
            return Error.NotFound("StockTransfer.NotFound", "Stock transfer not found");
        }

        var fromWarehouse = stockTransferSource.StockTransfer.Material.Kind == MaterialKind.Raw
            ? await context.Warehouses.IgnoreQueryFilters()
                .FirstOrDefaultAsync(w =>
                    w.DepartmentId == stockTransferSource.FromDepartmentId &&
                    w.Type == WarehouseType.RawMaterialStorage)
            : await context.Warehouses.IgnoreQueryFilters()
                .FirstOrDefaultAsync(w =>
                    w.DepartmentId == stockTransferSource.FromDepartmentId && w.Type == WarehouseType.PackagedStorage);
        
        var toWarehouse =  stockTransferSource.StockTransfer.Material.Kind == MaterialKind.Raw
            ? await context.Warehouses.IgnoreQueryFilters()
                .FirstOrDefaultAsync(w =>
                    w.DepartmentId == stockTransferSource.ToDepartmentId &&
                    w.Type == WarehouseType.RawMaterialStorage)
            : await context.Warehouses.IgnoreQueryFilters()
                .FirstOrDefaultAsync(w =>
                    w.DepartmentId == stockTransferSource.ToDepartmentId && w.Type == WarehouseType.PackagedStorage);

        decimal remainingQuantity = stockTransferSource.Quantity;

        foreach (var batchRequest in batches)
        {
            var batch = await context.MaterialBatches.FirstOrDefaultAsync(b => b.Id == batchRequest.BatchId);
            
            if (batch == null || batch.RemainingQuantity < batchRequest.Quantity)
            {
                return Error.Failure("Batch.InsufficientStock", $"Not enough stock in batch {batchRequest.BatchId}");
            }

            batch.QuantityAssigned = 0;
            var shelfMaterialBatches =
                await context.ShelfMaterialBatches.Where(sb => sb.MaterialBatchId == batch.Id).ToListAsync();
            context.ShelfMaterialBatches.RemoveRange(shelfMaterialBatches);

            var movement = new MassMaterialBatchMovement
            {
                BatchId = batch.Id,
                FromWarehouseId = fromWarehouse.Id,
                ToWarehouseId = toWarehouse.Id,
                Quantity = batchRequest.Quantity,
                MovedAt = DateTime.UtcNow,
                MovedById = userId
            };
            
            await context.MassMaterialBatchMovements.AddAsync(movement);
            
            var batchEvent = new MaterialBatchEvent
            {
                BatchId = batch.Id,
                Type = EventType.Moved,
                Quantity = batchRequest.Quantity,
                UserId = userId
            };
            await context.MaterialBatchEvents.AddAsync(batchEvent);
            
            context.MaterialBatches.Update(batch);
            remainingQuantity -= batchRequest.Quantity;

            if (remainingQuantity <= 0) break;
        }

        if (remainingQuantity > 0)
        {
            return Error.Failure("StockTransfer.InsufficientStock", "Not enough batches to fulfill the transfer");
        }

        stockTransferSource.IssuedAt = DateTime.UtcNow;
        stockTransferSource.IssuedById = userId;
        stockTransferSource.Status = StockTransferStatus.Issued;
        context.StockTransferSources.Update(stockTransferSource);
        await context.SaveChangesAsync();
        return Result.Success();
    }
    
    public async Task<Result<List<ProductionScheduleProcurementDto>>> GetMaterialsWithInsufficientStock(Guid productionScheduleId, Guid productId, Guid userId)
    {
        var productionSchedule = await context.ProductionSchedules
            .Include(ps => ps.Products)
            .FirstOrDefaultAsync(ps => ps.Id == productionScheduleId);
        
        if (productionSchedule == null)
        {
            return Error.NotFound("ProductionSchedule.NotFound", "Production schedule not found");
        }

        var product = productionSchedule.Products.FirstOrDefault(p => p.ProductId == productId);
        if (product == null)
        {
            return Error.NotFound("Product.NotFound", "Product not found in the schedule");
        }

        var materialStockDetails =
            await CheckMaterialStockLevelsForProductionSchedule(productionScheduleId, productId, MaterialRequisitionStatus.None, userId);
        
        if (!materialStockDetails.IsSuccess)
        {
            return materialStockDetails.Error;
        }

        return materialStockDetails.Value;
    }
    
    public async Task<Result<List<ProductionScheduleProcurementPackageDto>>> GetPackageMaterialsWithInsufficientStock(Guid productionScheduleId, Guid productId, Guid userId)
    {
        var productionSchedule = await context.ProductionSchedules
            .Include(ps => ps.Products)
            .FirstOrDefaultAsync(ps => ps.Id == productionScheduleId);
        
        if (productionSchedule == null)
        {
            return Error.NotFound("ProductionSchedule.NotFound", "Production schedule not found");
        }

        var product = productionSchedule.Products.FirstOrDefault(p => p.ProductId == productId);
        if (product == null)
        {
            return Error.NotFound("Product.NotFound", "Product not found in the schedule");
        }

        var materialStockDetails =
            await CheckPackageMaterialStockLevelsForProductionSchedule(productionScheduleId,productId, null, userId);
        
        if (!materialStockDetails.IsSuccess)
        {
            return materialStockDetails.Error;
        }

        var insufficientMaterials = materialStockDetails.Value
            .Where(m => m.Status == MaterialRequisitionStatus.None)
            .ToList();

        return insufficientMaterials;
    }
    
    public async Task<Result<Guid>> CreateFinalPacking(CreateFinalPacking request) 
    { 
        var finalPacking = mapper.Map<FinalPacking>(request); 
        
        await context.FinalPackings.AddAsync(finalPacking); 
        await context.SaveChangesAsync();

        var activityStep =
            await context.ProductionActivitySteps.FirstOrDefaultAsync(p => p.Id == request.ProductionActivityStepId);
        if (activityStep is not null)
        {
            activityStep.Status = ProductionStatus.Completed;
            activityStep.StartedAt = DateTime.UtcNow;
            activityStep.CompletedAt = DateTime.UtcNow;
            context.ProductionActivitySteps.Update(activityStep);
            await context.SaveChangesAsync();
        }
        
        return finalPacking.Id;
    }

    public async Task<Result<FinalPackingDto>> GetFinalPacking(Guid finalPackingId) 
    { 
        var finalPacking = await context.FinalPackings
            .AsSplitQuery()
            .Include(fp => fp.ProductionSchedule)
            .Include(fp => fp.Product)
            .Include(fp => fp.Materials).ThenInclude(m => m.Material)
            .FirstOrDefaultAsync(fp => fp.Id == finalPackingId);

        return finalPacking is null 
            ? Error.NotFound("FinalPacking.NotFound", "Final Packing record not found") 
            : mapper.Map<FinalPackingDto>(finalPacking);
    }

    /// ✅ **Extra Method: Get Final Packing by ProductionScheduleId & ProductId**
    public async Task<Result<FinalPackingDto>> GetFinalPackingByScheduleAndProduct(Guid productionScheduleId, Guid productId) 
    { 
        var finalPacking = await context.FinalPackings
            .AsSplitQuery()
            .Include(fp => fp.ProductionSchedule)
            .Include(fp => fp.Product)
            .Include(fp => fp.Materials).ThenInclude(m => m.Material)
            .FirstOrDefaultAsync(fp => fp.ProductionScheduleId == productionScheduleId && fp.ProductId == productId);

        return finalPacking is null 
            ? Error.NotFound("FinalPacking.NotFound", "No Final Packing found for the given Production Schedule and Product") 
            : mapper.Map<FinalPackingDto>(finalPacking);
    }

    /// ✅ **Paginated List of Final Packings**
    public async Task<Result<Paginateable<IEnumerable<FinalPackingDto>>>> GetFinalPackings(int page, int pageSize, string searchQuery) 
    { 
        var query = context.FinalPackings
            .Include(fp => fp.ProductionSchedule)
            .Include(fp => fp.Product)
            .AsQueryable();

        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.Where(fp => fp.Product.Name.Contains(searchQuery) || 
                                      fp.ProductionSchedule.Code.Contains(searchQuery));
        }
        
        return await PaginationHelper.GetPaginatedResultAsync(
            query, 
            page, 
            pageSize, 
            mapper.Map<FinalPackingDto>);
    }

    public async Task<Result> UpdateFinalPacking(CreateFinalPacking request, Guid finalPackingId) 
    { 
        var existingFinalPacking = await context.FinalPackings
            .Include(f => f.Materials)
            .FirstOrDefaultAsync(fp => fp.Id == finalPackingId);

        if (existingFinalPacking is null) 
        { 
            return Error.NotFound("FinalPacking.NotFound", "Final Packing record not found");
        }

        context.FinalPackingMaterials.RemoveRange(existingFinalPacking.Materials);
        
        mapper.Map(request, existingFinalPacking);
        
        context.FinalPackings.Update(existingFinalPacking); 
        await context.SaveChangesAsync(); 
        return Result.Success();
    }

    /// ✅ **Delete Final Packing**
    public async Task<Result> DeleteFinalPacking(Guid finalPackingId, Guid userId) 
    { 
        var finalPacking = await context.FinalPackings.FirstOrDefaultAsync(fp => fp.Id == finalPackingId); 
        
        if (finalPacking is null) 
        { 
            return Error.NotFound("FinalPacking.NotFound", "Final Packing record not found");
        }
        
        finalPacking.DeletedAt = DateTime.UtcNow; 
        finalPacking.LastDeletedById = userId; 
        context.FinalPackings.Update(finalPacking); 
        await context.SaveChangesAsync(); 
        return Result.Success();
    }
    
    public async Task<Result<RequisitionDto>> GetStockRequisitionForPackaging(Guid productionScheduleId, Guid productId)
    {
        var requisition = await context.Requisitions
            .AsSplitQuery()
            .Include(r => r.Items)
            .ThenInclude(i => i.Material)
            .Include(r => r.RequestedBy)
            .Include(r => r.Approvals).ThenInclude(a => a.User)
            .Include(r => r.Approvals).ThenInclude(a => a.Role)
            .Where(r => r.ProductionScheduleId == productionScheduleId 
                        && r.ProductId == productId 
                        && r.Code.EndsWith("-package") // Ensure the code ends in "package"
                        && r.RequisitionType == RequisitionType.Stock) // Ensure it's a stock requisition
            .FirstOrDefaultAsync();

        return requisition is null 
            ? Error.NotFound("StockRequisition.NotFound", "No stock requisition for packaging found.") 
            : mapper.Map<RequisitionDto>(requisition);
    }

}