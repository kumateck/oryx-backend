using APP.Extensions;
using APP.IRepository;
using APP.Services.Background;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.BinCards;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.Notifications;
using DOMAIN.Entities.ProductionSchedules;
using DOMAIN.Entities.ProductionSchedules.Packing;
using DOMAIN.Entities.ProductionSchedules.StockTransfers;
using DOMAIN.Entities.ProductionSchedules.StockTransfers.Request;
using DOMAIN.Entities.Products;
using DOMAIN.Entities.Products.Production;
using DOMAIN.Entities.Requisitions;
using DOMAIN.Entities.Routes;
using DOMAIN.Entities.Users;
using DOMAIN.Entities.Warehouses;
using INFRASTRUCTURE.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public class ProductionScheduleRepository(
    ApplicationDbContext context,
    IMapper mapper,
    UserManager<User> userManager,
    IMaterialRepository materialRepository,
    IBackgroundWorkerService backgroundWorkerService)
    : IProductionScheduleRepository
{
    public async Task<Result<Guid>> CreateProductionSchedule(CreateProductionScheduleRequest request, Guid userId)
    {
        if (request.ScheduledEndTime < request.ScheduledStartTime)
        {
            return Error.Validation("ProductionSchedule.Validation",
                "Scheduled end time cannot be before scheduled start time");
        }

        if (request.ScheduledEndTime < DateTime.UtcNow)
        {
            return Error.Validation("ProductionSchedule.Validation",
                "Scheduled end time cannot be before current time");
        }

        if (await context.ProductionSchedules.AnyAsync(p => p.Code.ToLower() == request.Code.ToLower()))
        {
            return Error.Validation("ProductionSchedule.ProductionSchedule", "Code is already in use");
        }

        var productionSchedule = mapper.Map<ProductionSchedule>(request);
        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null) return UserErrors.NotFound(userId);

        if (user.DepartmentId is null) return UserErrors.DepartmentNotFound;

        productionSchedule.DepartmentId = user.DepartmentId;
        productionSchedule.CreatedById = userId;
        await context.ProductionSchedules.AddAsync(productionSchedule);
        await context.SaveChangesAsync();

        return productionSchedule.Id;
    }

    public async Task<Result<ProductionScheduleDto>> GetProductionSchedule(Guid scheduleId)
    {
        var productionSchedule = await context.ProductionSchedules
            .AsSplitQuery()
            .Include(s => s.Products).ThenInclude(s => s.ProductPacking).ThenInclude(p => p.BasePackingUoM)
            .Include(s => s.Products).ThenInclude(s => s.Product)
            .Include(s => s.Products)
            .ThenInclude(s => s.ProductPacking).ThenInclude(p => p.PackingLists).ThenInclude(p => p.Uom)
            .FirstOrDefaultAsync(s => s.Id == scheduleId);

        return productionSchedule is null
            ? Error.NotFound("ProductionSchedule.NotFound", "Production schedule is not found")
            : mapper.Map<ProductionScheduleDto>(productionSchedule);
    }

    public async Task<Result<List<ProductionScheduleProcurementDto>>> GetProductionScheduleDetail(Guid scheduleId,
        Guid userId)
    {
        // Fetch the production schedule with related data
        var productionSchedule = await context.ProductionSchedules
            .AsSplitQuery()
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


    public async Task<Result<Paginateable<IEnumerable<ProductionScheduleDto>>>> GetProductionSchedules(int page,
        int pageSize, string searchQuery, Guid departmentId)
    {
        var query = context.ProductionSchedules
            .AsSplitQuery()
            .Include(s => s.Products.Where(p => p.Product.DepartmentId == departmentId))
            .ThenInclude(p => p.Product)
            .Include(s => s.Products).ThenInclude(s => s.ProductPacking).ThenInclude(p => p.PackingLists)
            .Where(s => s.Products.Any(p => p.Product.DepartmentId == departmentId))
            .AsQueryable();

        return await PaginationHelper.GetPaginatedResultAsync(
            query,
            page,
            pageSize,
            mapper.Map<ProductionScheduleDto>);
    }

    public async Task<Result> UpdateProductionSchedule(UpdateProductionScheduleRequest request, Guid scheduleId,
        Guid userId)
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

    public async Task<Result<Guid>> StartProductionActivity(Guid productionScheduleProductId, Guid userId)
    {
        await using var transaction = await context.Database.BeginTransactionAsync();

        try
        {
            if (context.ProductionActivities.Any(p =>
                    p.ProductionScheduleProductId == productionScheduleProductId))
            {
                return Error.NotFound("ProductionActivity.AlreadyExist",
                    "A production activity already exists for this product and schedule");
            }

            var productionScheduleProduct = await context.ProductionScheduleProducts
                .AsSplitQuery()
                .Include(productionSchedule => productionSchedule.ProductionSchedule)
                .FirstOrDefaultAsync(p => p.Id == productionScheduleProductId);

            if (productionScheduleProduct is null)
                return Error.NotFound("ProductionScheduleProduct.NotFound",
                    "Production schedule product not found");

            var product = await context.Products
                .AsSplitQuery()
                .Include(product => product.Routes).ThenInclude(route => route.Resources)
                .Include(product => product.Routes).ThenInclude(route => route.WorkCenters)
                .Include(product => product.Routes).ThenInclude(route => route.ResponsibleUsers)
                .ThenInclude(routeResponsibleUser => routeResponsibleUser.ProductAnalyticalRawData)
                .Include(product => product.Routes).ThenInclude(route => route.ResponsibleRoles)
                .ThenInclude(routeResponsibleRole => routeResponsibleRole.ProductAnalyticalRawData)
                .FirstOrDefaultAsync(p => p.Id == productionScheduleProduct.ProductId);

            if (product is null)
                return Error.NotFound("Product.Validation", "Product was not found");

            if (product.Routes.Count == 0)
                return Error.Validation("Product.Validation",
                    "This product has no procedures defined hence a production activity cannot commence.");

            var users = product.Routes.SelectMany(r => r.ResponsibleUsers).Select(r => r.User).ToList();
            var roles = product.Routes.SelectMany(r => r.ResponsibleRoles).Select(r => r.Role).ToList();
            var usersInRole = new List<User>();

            foreach (var role in roles)
            {
                var userRoles = await userManager.GetUsersInRoleAsync(role?.Name ?? "");
                userRoles = userRoles.Where(u => u.DepartmentId == product.DepartmentId).ToList();
                usersInRole.AddRange(userRoles);
            }

            var quantity = productionScheduleProduct.Quantity;

            try
            {
                await FreezeMaterialInProduction(productionScheduleProduct.Id);
            }
            catch (Exception e)
            {
                return Error.Failure("Reserve.Material", $"Failed reserving material for production: {e.Message}");
            }

            // Build user actions map
            var userActionsMap =
                new Dictionary<(Guid userId, int order), (Guid? productArdId, OperationAction action)>();

            foreach (var route in product.Routes)
            {
                foreach (var ru in route.ResponsibleUsers)
                {
                    userActionsMap[(ru.UserId, route.Order)] = (ru.ProductAnalyticalRawDataId, ru.Action);
                }
            }

            foreach (var route in product.Routes)
            {
                foreach (var rr in route.ResponsibleRoles)
                {
                    var roleName = rr.Role?.Name ?? "";
                    var usersInThisRole = await userManager.GetUsersInRoleAsync(roleName);
                    foreach (var user in usersInThisRole)
                    {
                        if (userActionsMap.ContainsKey((user.Id, route.Order)))
                            continue;
                        userActionsMap[(user.Id, route.Order)] = (rr.ProductAnalyticalRawDataId, rr.Action);
                    }
                }
            }

            var totalUsers = userActionsMap.Keys
                .Select(k => k.userId)
                .Distinct()
                .Select(uId => users.FirstOrDefault(u => u.Id == uId) ?? usersInRole.FirstOrDefault(u => u.Id == uId))
                .Where(u => u != null)
                .Distinct()
                .ToList();

            if (totalUsers.Count == 0)
                return Error.Validation("Product.Validation",
                    "This product has no users associated for procedures defined hence a production activity cannot commence.");

            var activity = new ProductionActivity
            {
                ProductionScheduleProductId = productionScheduleProductId,
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
                    ResponsibleUsers = userActionsMap
                        .Where(kvp => kvp.Key.order == r.Order)
                        .Select(kvp => new ProductionActivityStepUser
                        {
                            UserId = kvp.Key.userId,
                            ProductAnalyticalRawDataId = kvp.Value.productArdId,
                            Action = kvp.Value.action
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
                ProductionScheduleProductId = productionScheduleProductId,
                ProductionActivityStepId = activity.Steps.OrderBy(s => s.Order).First().Id,
                BatchQuantity = quantity
            });

            await CreateBatchPackagingRecord(new CreateBatchPackagingRecord
            {
                ProductionScheduleProductId = productionScheduleProductId,
                ProductionActivityStepId = activity.Steps.OrderBy(s => s.Order).First().Id,
                BatchQuantity = quantity
            });

            await transaction.CommitAsync();

            return activity.Id;
        }
        catch (Exception e)
        {
            await transaction.RollbackAsync();
            return Error.Failure("Production.Start", $"Failed to start production activity: {e.Message}");
        }
    }


    public async Task<Result> UpdateStatusOfProductionActivityStep(Guid productionStepId, ProductionStatus status,
        Guid userId)
    {
        var activityStep = await context.ProductionActivitySteps
            .AsSplitQuery()
            .Include(productionActivityStep => productionActivityStep.ResponsibleUsers)
            .Include(productionActivityStep => productionActivityStep.ProductionActivity)
            .ThenInclude(productionActivity => productionActivity.ProductionScheduleProduct)
            .Include(productionActivityStep => productionActivityStep.Operation)
            .FirstOrDefaultAsync(p => p.Id == productionStepId);
        if (activityStep is null)
            return Error.NotFound("ProductActivity.NotFound", "Activity step was not found");

        if (activityStep.ResponsibleUsers.All(u => u.UserId != userId))
            return Error.Validation("ProductActivity.Validation",
                "You are not responsible for changing the status of this activity");

        activityStep.Status = status;

        switch (status)
        {
            case ProductionStatus.InProgress:
                activityStep.StartedAt = DateTime.UtcNow;

                var isFirstStep = await context.ProductionActivitySteps
                    .Where(s => s.ProductionActivityId == activityStep.ProductionActivityId)
                    .OrderBy(s => s.Order)
                    .Select(s => s.Id)
                    .FirstOrDefaultAsync() == productionStepId;

                if (isFirstStep)
                {
                    activityStep.ProductionActivity.Status = ProductionStatus.InProgress;
                }

                if (activityStep.Operation.Name == "Product Preparation")
                {
                    var productionActivity = activityStep.ProductionActivity;
                    var product = await context.Products.IgnoreQueryFilters()
                        .AsSplitQuery()
                        .FirstOrDefaultAsync(p => p.Id == productionActivity.ProductionScheduleProduct.ProductId);

                    if (product is not null)
                    {
                        var productionWarehouse = await context.Warehouses
                            .IgnoreQueryFilters()
                            .FirstOrDefaultAsync(w =>
                                w.DepartmentId == product.DepartmentId && w.Type == WarehouseType.Production);

                        if (productionWarehouse is not null)
                        {
                            var stockRequisitions = await context.Requisitions
                                .AsSplitQuery()
                                .Include(r => r.Items)
                                .Where(r => r.ProductionScheduleProductId == 
                                            activityStep.ProductionActivity.ProductionScheduleProductId).ToListAsync();


                            foreach (var stockRequisition in stockRequisitions)
                            {
                                foreach (var item in stockRequisition.Items)
                                {
                                    var batchesToConsume =
                                        await materialRepository.GetReservedBatchesAndQuantityForProductionWarehouse(
                                            item.MaterialId,
                                            productionWarehouse.Id, stockRequisition.ProductionScheduleProductId.Value);

                                    foreach (var batch in batchesToConsume)
                                    {
                                        await materialRepository.ConsumeMaterialAtLocation(batch.MaterialBatch.Id,
                                            productionWarehouse.Id, batch.Quantity, userId);
                                    }

                                    var batchesToRemove = await context.MaterialBatchReservedQuantities
                                        .Where(b => batchesToConsume.Select(bc => bc.Id).Contains(b.Id))
                                        .ToListAsync();

                                    context.MaterialBatchReservedQuantities.RemoveRange(batchesToRemove);
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
                        return ProductErrors.NotFound(productionActivity.ProductionScheduleProductId);
                    }
                }

                break;

            case ProductionStatus.Completed:
                activityStep.CompletedAt = DateTime.UtcNow;


                var isLastStep = await context.ProductionActivitySteps
                    .Where(s => s.ProductionActivityId == activityStep.ProductionActivityId)
                    .OrderByDescending(s => s.Order)
                    .Select(s => s.Id)
                    .FirstOrDefaultAsync() == productionStepId;

                if (isLastStep)
                {
                    activityStep.ProductionActivity.CompletedAt = DateTime.UtcNow;
                    activityStep.ProductionActivity.Status = ProductionStatus.Completed;

                }
                else
                {
                    var nextStep = await context
                        .ProductionActivitySteps
                        .AsSplitQuery()
                        .Include(productionActivityStep => productionActivityStep.ResponsibleUsers)
                        .FirstOrDefaultAsync(s =>
                            s.ProductionActivityId == activityStep.ProductionActivityId &&
                            s.Order == activityStep.Order + 1);
                    if (nextStep is not null)
                    {
                        var nextStepUsers = nextStep.ResponsibleUsers.Select(u => u.User).ToList();
                        backgroundWorkerService
                            .EnqueueNotification("You have been assigned a production task",
                                NotificationType.ProductionStageChanged, null, nextStepUsers);
                    }
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

    public async Task<Result<Paginateable<IEnumerable<ProductionActivityListDto>>>> GetProductionActivities(
        ProductionFilter filter)
    {
        var query = context.ProductionActivities
            .AsSplitQuery()
            .Include(pa => pa.ProductionScheduleProduct)
            .ThenInclude(pa => pa.Product)
            .Include(pa => pa.ProductionScheduleProduct)
            .ThenInclude(pa => pa.ProductionSchedule)
            .Include(pa => pa.Steps).ThenInclude(step => step.ResponsibleUsers)
            .Include(pa => pa.Steps).ThenInclude(step => step.Resources)
            .Include(pa => pa.Steps).ThenInclude(step => step.WorkCenters)
            .Include(pa => pa.Steps).ThenInclude(step => step.WorkFlow)
            .Include(pa => pa.Steps).ThenInclude(step => step.Operation)
            .AsQueryable();

        if (filter.UserIds.Count != 0)
        {
            query = query.Where(pa =>
                pa.Steps.Any(step => step.ResponsibleUsers.Any(ru => filter.UserIds.Contains(ru.UserId))));
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
            .Include(pa => pa.ProductionScheduleProduct)
            .ThenInclude(pa => pa.Product)
            .Include(pa => pa.ProductionScheduleProduct)
            .ThenInclude(pa => pa.ProductionSchedule)
            .Include(pa => pa.Steps.OrderBy(p => p.Order))
            .Include(pa => pa.Steps).ThenInclude(step => step.ResponsibleUsers)
            .ThenInclude(ru => ru.ProductAnalyticalRawData)
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

    public async Task<Result<ProductionActivityDto>> GetProductionActivityByProductionScheduleProduct(
        Guid productionScheduleProductId)
    {
        var productionActivity = await context.ProductionActivities
            .AsSplitQuery()
            .Include(pa => pa.ProductionScheduleProduct)
            .ThenInclude(pa => pa.Product)
            .Include(pa => pa.ProductionScheduleProduct)
            .ThenInclude(pa => pa.ProductPacking)
            .ThenInclude(p => p.PackingLists)
            .Include(pa => pa.ProductionScheduleProduct)
            .ThenInclude(pa => pa.ProductionSchedule)
            .Include(pa => pa.Steps.OrderBy(p => p.Order))
            .Include(pa => pa.Steps).ThenInclude(step => step.ResponsibleUsers)
            .ThenInclude(pas => pas.ProductAnalyticalRawData)
            .Include(pa => pa.Steps).ThenInclude(step => step.Resources)
            .Include(pa => pa.Steps).ThenInclude(step => step.WorkCenters)
            .Include(pa => pa.Steps).ThenInclude(step => step.WorkFlow)
            .Include(pa => pa.Steps).ThenInclude(step => step.Operation)
            .FirstOrDefaultAsync(pa => pa.ProductionScheduleProductId == productionScheduleProductId);

        return Result.Success(mapper.Map<ProductionActivityDto>(productionActivity));
    }

    public async Task<Result<Paginateable<IEnumerable<ProductionActivityStepDto>>>> GetProductionActivitySteps(
        ProductionFilter filter)
    {
        var query = context.ProductionActivitySteps
            .AsSplitQuery()
            .Include(pas => pas.ProductionActivity)
            .Include(pas => pas.ResponsibleUsers)
            .ThenInclude(pas => pas.ProductAnalyticalRawData)
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
            .ThenInclude(pas => pas.ProductAnalyticalRawData)
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
            .AsSplitQuery()
            .Include(pa => pa.ProductionScheduleProduct)
            .ThenInclude(pa => pa.Product)
            .Include(pa => pa.ProductionScheduleProduct)
            .ThenInclude(pa => pa.ProductionSchedule)
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

    public async Task<Result<List<ProductionActivityGroupResultDto>>> GetProductionActivityGroupedByOperation(
        Guid? departmentId)
    {
        // Fetch all unique operation names in the correct order

        var allOperations = departmentId.HasValue
            ? await context.Operations
                .OrderBy(o => o.Order)
                .Where(o => o.DepartmentId == departmentId)
                .Select(o => new OperationDto
                { Id = o.Id, Name = o.Name, Description = o.Description, Order = o.Order })
                .AsNoTracking()
                .ToListAsync()
            : await context.Operations
                .OrderBy(o => o.Order)
                .Select(o => new OperationDto
                { Id = o.Id, Name = o.Name, Description = o.Description, Order = o.Order })
                .AsNoTracking()
                .ToListAsync();

        // Fetch production activities with only necessary data
        var productionActivities = await context.ProductionActivities
            .AsSplitQuery()
            .Include(pa => pa.ProductionScheduleProduct)
            .ThenInclude(productionSchedule => productionSchedule.Product)
            .Include(pa => pa.ProductionScheduleProduct)
            .ThenInclude(p => p.ProductionSchedule)
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
                ProductionScheduleProduct = mapper.Map<ProductionScheduleProductDto>(pa.ProductionScheduleProduct),
                Status = pa.Status,
                StartedAt = pa.StartedAt,
                CompletedAt = pa.CompletedAt,
                BatchNumber = pa.ProductionScheduleProduct.BatchNumber ??
                              context.BatchManufacturingRecords.FirstOrDefault(b =>
                                  b.ProductionScheduleProductId == pa.ProductionScheduleProductId)?.BatchNumber,
                Quantity = pa.ProductionScheduleProduct?.Quantity ?? 0,
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
            .GroupBy(p => new CollectionItemDto
            { Id = p.CurrentStep.Operation.Id, Name = p.CurrentStep.Operation.Name })
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

    public async Task<Result<Dictionary<string, List<ProductionActivityStepDto>>>>
        GetProductionActivityStepsGroupedByStatus()
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

    public async Task<Result<Dictionary<string, List<ProductionActivityStepDto>>>>
        GetProductionActivityStepsGroupedByOperation()
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


    public async Task<Result<List<ProductionScheduleProcurementDto>>> CheckMaterialStockLevelsForProductionSchedule(
        Guid productionScheduleProductId, MaterialRequisitionStatus? status)
    {
        var productionScheduleProduct = await context.ProductionScheduleProducts
            .FirstOrDefaultAsync(p => p.Id == productionScheduleProductId);

        if (productionScheduleProduct == null) return ProductErrors.NotFound(productionScheduleProductId);

        var product = await context.Products
            .AsSplitQuery()
            .IgnoreAutoIncludes()
            .Include(product => product.BillOfMaterials)
            .ThenInclude(p => p.BillOfMaterial)
            .ThenInclude(p => p.Items).ThenInclude(billOfMaterialItem => billOfMaterialItem.Material)
            .ThenInclude(m => m.Batches)
            .Include(product => product.BillOfMaterials)
            .ThenInclude(productBillOfMaterial => productBillOfMaterial.BillOfMaterial)
            .ThenInclude(billOfMaterial => billOfMaterial.Items)
            .ThenInclude(billOfMaterialItem => billOfMaterialItem.BaseUoM)
            .FirstOrDefaultAsync(p => p.Id == productionScheduleProduct.ProductId);

        if (product is null)
            return ProductErrors.NotFound(productionScheduleProduct.ProductId);

        var productionSchedule =
            await context.ProductionSchedules
                .AsSplitQuery()
                .Include(productionSchedule => productionSchedule.Products)
                .Include(p => p.Department).Include(baseEntity => baseEntity.CreatedBy)
                .FirstOrDefaultAsync(p => p.Id == productionScheduleProduct.ProductionScheduleId);
        if (productionSchedule is null)
            return ProductErrors.NotFound(productionScheduleProduct.ProductionScheduleId);

        var batchSize = productionScheduleProduct.BatchSize;

        var activeBoM = product.BillOfMaterials
            .OrderByDescending(p => p.EffectiveDate)
            .FirstOrDefault(p => p.IsActive);

        if (activeBoM is null)
            return Error.NotFound("Product.BoM", "No active bom found for this product");

        var user = productionSchedule.CreatedBy;
        var department = productionSchedule.Department ?? user.Department;
        if (department is null)
            return Error.NotFound("Product.Department", "No department found for this production scheduled");

        var stockLevels = new Dictionary<Guid, decimal>();
        if (department == null)
            return Error.NotFound("User.Department", "User has no association to any department");

        if (department.Warehouses.Count == 0)
            return Error.NotFound("User.Warehouse", "No raw material warehouse is associated with current user");

        var warehouse = user.Department.Warehouses.FirstOrDefault(i => i.Type == WarehouseType.RawMaterialStorage);
        if (warehouse is null)
            return Error.NotFound("User.Warehouse", "No raw material warehouse is associated with current user");

        var productionWarehouse = user.Department.Warehouses.FirstOrDefault(i => i.Type == WarehouseType.Production);
        if (productionWarehouse is null)
            return Error.NotFound("User.Warehouse", "No production warehouse is associated with current user");

        var sourceRequisitionItems = new List<SourceRequisitionItem>();

        var stockTransfers = await context.StockTransfers.Where(s =>
                s.ProductionScheduleProductId == productionScheduleProductId)
            .ToListAsync();

        var stockRequisition = await context.Requisitions.Include(requisition => requisition.Items)
            .FirstOrDefaultAsync(r =>
                r.ProductionScheduleProductId == productionScheduleProductId &&
                r.RequisitionType == RequisitionType.Stock);

        var purchaseRequisition = await context.Requisitions.Include(requisition => requisition.Items).Where(r =>
            r.ProductionScheduleProductId == productionScheduleProductId &&
            r.RequisitionType == RequisitionType.Purchase).ToListAsync();

        if (purchaseRequisition.Count != 0)
        {
            sourceRequisitionItems = await context.SourceRequisitionItems
                .Where(sr => purchaseRequisition.Select(pr => pr.Id).Contains(sr.RequisitionId))
                .ToListAsync();
        }

        // Fetch stock levels for each material ID individually
        foreach (var materialId in activeBoM.BillOfMaterial.Items.Select(item => item.MaterialId).Distinct())
        {
            var stockLevel = await materialRepository
                .GetShelfMaterialStockInWarehouse(materialId, warehouse.Id);
            stockLevels[materialId] = stockLevels.GetValueOrDefault(materialId, 0) + stockLevel.Value;
        }

        var materialDepartments = await context.MaterialDepartments
            .AsSplitQuery()
            .IgnoreQueryFilters()
            .Include(m => m.UoM)
            .Include(m => m.Material)
            .Include(m => m.Department)
            .Where(m => activeBoM.BillOfMaterial.Items.Select(i => i.MaterialId).Distinct().Contains(m.MaterialId)
                        && m.DepartmentId == department.Id && !m.DeletedAt.HasValue)
            .ToDictionaryAsync(k => k.MaterialId, v => v);

        var materialDetails = activeBoM.BillOfMaterial.Items
            .Where(i => materialDepartments.ContainsKey(i.MaterialId))
            .Select(item =>
            {
                var quantityOnHand = stockLevels.GetValueOrDefault(item.MaterialId, 0);

                var quantityNeeded =
                    batchSize == BatchSize.Full ? item.PrescribedQuantity : item.PrescribedQuantity / 2;

                var materialDepartment = materialDepartments.GetValueOrDefault(item.MaterialId);

                var reservedQuantityBatches =
                    materialRepository.GetReservedBatchesAndQuantityForProductionWarehouse(item.MaterialId,
                        productionWarehouse.Id, productionScheduleProduct.Id).Result;
                
                var reservedQuantity = reservedQuantityBatches.Sum(r => r.Quantity);
                
                var consumedQuantityBatches = 
                    materialRepository.GetConsumedBatchesAndQuantityForProductionWarehouse(item.MaterialId,
                        productionWarehouse.Id, productionScheduleProduct.Id).Result;
                
                var consumedQuantity = consumedQuantityBatches.Sum(r => r.Quantity);

                return new ProductionScheduleProcurementDto
                {
                    Material = mapper.Map<MaterialDto>(item.Material),
                    BaseUoM = mapper.Map<UnitOfMeasureDto>(item.BaseUoM),
                    BaseQuantity = item.BaseQuantity,
                    QuantityNeeded = quantityNeeded,
                    QuantityOnHand = quantityOnHand,
                    Status = quantityOnHand >= quantityNeeded || reservedQuantity > 0
                        ? MaterialRequisitionStatus.InHouse
                        : GetStatusOfProductionMaterial(stockTransfers, stockRequisition?.Items ?? [],
                            purchaseRequisition.SelectMany(p => p.Items).ToList(), sourceRequisitionItems,
                            item.MaterialId),
                    StorageWarehouseId = warehouse.Id,
                    ProductionWarehouseId = productionWarehouse.Id,
                    MaterialDepartment = new MaterialDepartmentDetails
                    {
                        Department = mapper.Map<CollectionItemDto>(materialDepartment?.Department),
                        UoM = mapper.Map<UnitOfMeasureDto>(materialDepartment?.UoM),
                        ReOrderLevel = materialDepartment?.ReOrderLevel ?? 0,
                        MaximumStockLevel = materialDepartment?.MaximumStockLevel ?? 0,
                        MinimumStockLevel = materialDepartment?.MinimumStockLevel ?? 0,
                    },
                    FrozenQuantity = reservedQuantity,
                    ConsumedQuantity = consumedQuantity
                };
            }).ToList();

        if (status.HasValue)
        {
            materialDetails = materialDetails.Where(m => m.Status == status).ToList();
        }

        return materialDetails;
    }

    private MaterialRequisitionStatus GetStatusOfProductionMaterial(List<StockTransfer> stockTransfers,
        List<RequisitionItem> stockRequisitionItems, List<RequisitionItem> purchaseRequisitionItems,
        List<SourceRequisitionItem> sourceRequisitionItems, Guid materialId)
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

    public async Task<Result<List<ProductionScheduleProcurementPackageDto>>>
        CheckPackageMaterialStockLevelsForProductionSchedule(Guid productionScheduleProductId,
            MaterialRequisitionStatus? status)
    {
        var productionScheduleProduct = await
            context.ProductionScheduleProducts.FirstOrDefaultAsync(p => p.Id == productionScheduleProductId);
        if (productionScheduleProduct is null) return ProductErrors.NotFound(productionScheduleProductId);

        var product = await context.Products
            .AsSplitQuery()
            .Include(product => product.Packages).ThenInclude(productPackage => productPackage.Material)
            .ThenInclude(m => m.Batches)
            .Include(product => product.Packages).ThenInclude(productPackage => productPackage.DirectLinkMaterial)
            .AsSplitQuery()
            .FirstOrDefaultAsync(p => p.Id == productionScheduleProduct.ProductId);
        if (product is null)
            return ProductErrors.NotFound(productionScheduleProduct.ProductId);

        var productionSchedule =
            await context.ProductionSchedules
                .AsSplitQuery()
                .Include(productionSchedule => productionSchedule.Products)
                .Include(p => p.CreatedBy).ThenInclude(u => u.Department)
                .Include(productionSchedule => productionSchedule.Department)
                .FirstOrDefaultAsync(p => p.Id == productionScheduleProduct.ProductionScheduleId);

        if (productionSchedule is null)
            return ProductErrors.NotFound(productionScheduleProduct.ProductionScheduleId);

        var batchSize = productionScheduleProduct.BatchSize;
        var stockLevels = new Dictionary<Guid, decimal>();

        var user = productionSchedule.CreatedBy;
        var department = productionSchedule.Department ?? user.Department;

        if (department is null)
            return Error.NotFound("Product.Department", "No department found for this production scheduled");

        if (department.Warehouses.Count == 0)
            return Error.NotFound("User.Warehouse", "No package material warehouse is associated with current user");

        var warehouse = user.Department.Warehouses.FirstOrDefault(i => i.Type == WarehouseType.PackagedStorage);
        if (warehouse is null)
            return Error.NotFound("User.Warehouse", "No package material warehouse is associated with current user");

        var productionWarehouse = user.Department.Warehouses.FirstOrDefault(i => i.Type == WarehouseType.Production);
        if (productionWarehouse is null)
            return Error.NotFound("User.Warehouse", "No production warehouse is associated with current user");

        var sourceRequisitionItems = new List<SourceRequisitionItem>();

        var stockTransfers = await context.StockTransfers.Where(s =>
                s.ProductionScheduleProductId == productionScheduleProductId)
            .ToListAsync();

        var stockRequisition = await context.Requisitions.Include(requisition => requisition.Items)
            .FirstOrDefaultAsync(r =>
                r.ProductionScheduleProductId == productionScheduleProductId &&
                r.RequisitionType == RequisitionType.Stock);

        var purchaseRequisition = await context.Requisitions.Include(requisition => requisition.Items).Where(r =>
            r.ProductionScheduleProductId == productionScheduleProductId &&
            r.RequisitionType == RequisitionType.Purchase).ToListAsync();

        if (purchaseRequisition.Count != 0)
        {
            sourceRequisitionItems = await context.SourceRequisitionItems
                .Where(sr => purchaseRequisition.Select(pr => pr.Id).Contains(sr.RequisitionId))
                .ToListAsync();
        }

        foreach (var materialId in product.Packages.Select(item => item.MaterialId).Distinct())
        {
            var stockLevel = await materialRepository.GetShelfMaterialStockInWarehouse(materialId, warehouse.Id);
            stockLevels[materialId] = stockLevels.GetValueOrDefault(materialId, 0) + stockLevel.Value;
        }

        var materialDepartments = await context.MaterialDepartments
            .AsSplitQuery()
            .IgnoreQueryFilters()
            .Include(m => m.UoM)
            .Include(m => m.Material)
            .Include(m => m.Department)
            .Where(m => product.Packages.Select(i => i.MaterialId).Distinct().Contains(m.MaterialId)
                        && m.DepartmentId == department.Id && !m.DeletedAt.HasValue)
            .ToDictionaryAsync(k => k.MaterialId, v => v);

        var materialDetails = product.Packages
            .Where(p =>
                p.ProductPackingId == productionScheduleProduct.ProductPackingId || !p.ProductPackingId.HasValue &&
                materialDepartments.ContainsKey(p.MaterialId))
            .Select(item =>
            {
                var quantityOnHand = stockLevels.GetValueOrDefault(item.MaterialId, 0);
                var quantityNeeded = batchSize == BatchSize.Full
                    ? item.PrescribedQuantity + item.Loose
                    : item.PrescribedQuantity / 2 + item.Loose;

                var materialDepartment = materialDepartments.GetValueOrDefault(item.MaterialId);

                var reservedQuantityBatches =
                    materialRepository.GetReservedBatchesAndQuantityForProductionWarehouse(item.MaterialId,
                        productionWarehouse.Id, productionScheduleProduct.Id).Result;
                
                var reservedQuantity = reservedQuantityBatches.Sum(r => r.Quantity);

                var consumedQuantityBatches = 
                    materialRepository.GetConsumedBatchesAndQuantityForProductionWarehouse(item.MaterialId,
                        productionWarehouse.Id, productionScheduleProduct.Id).Result;
                
                var consumedQuantity = consumedQuantityBatches.Sum(r => r.Quantity);

                return new ProductionScheduleProcurementPackageDto
                {
                    Material = mapper.Map<MaterialDto>(item.Material),
                    DirectLinkMaterial = mapper.Map<MaterialDto>(item.DirectLinkMaterial),
                    BaseQuantity = item.BaseQuantity,
                    UnitCapacity = item.UnitCapacity,
                    Status = quantityOnHand >= quantityNeeded || reservedQuantity > 0
                        ? MaterialRequisitionStatus.InHouse
                        : GetStatusOfProductionMaterial(stockTransfers, stockRequisition?.Items ?? [],
                            purchaseRequisition.SelectMany(p => p.Items).ToList(), sourceRequisitionItems,
                            item.MaterialId),
                    PrescribedQuantity = item.PrescribedQuantity,
                    QuantityNeeded = quantityNeeded,
                    QuantityOnHand = quantityOnHand,
                    PackingExcessMargin = item.PackingExcessMargin,
                    StorageWarehouseId = warehouse.Id,
                    ProductionWarehouseId = productionWarehouse.Id,
                    MaterialDepartment = new MaterialDepartmentDetails
                    {
                        Department = mapper.Map<CollectionItemDto>(materialDepartment?.Department),
                        UoM = mapper.Map<UnitOfMeasureDto>(materialDepartment?.UoM),
                        ReOrderLevel = materialDepartment?.ReOrderLevel ?? 0,
                        MaximumStockLevel = materialDepartment?.MaximumStockLevel ?? 0,
                        MinimumStockLevel = materialDepartment?.MinimumStockLevel ?? 0,
                    },
                    FrozenQuantity = reservedQuantity,
                    ConsumedQuantity = consumedQuantity
                };
            }).ToList();

        if (status.HasValue)
        {
            materialDetails = materialDetails.Where(m => m.Status == status).ToList();
        }

        return materialDetails;
    }

    // private static decimal CalculateRequiredItemQuantity(decimal targetProductQuantity, decimal itemBaseQuantity, decimal productBaseQuantity)
    // {
    //     return Math.Round(targetProductQuantity * itemBaseQuantity / productBaseQuantity, 2);
    // }

    /*private decimal GetQuantityNeeded(ProductPackage item, List<ProductPackage> allPackages, decimal quantityRequired, decimal basePackingQuantity, HashSet<Guid> visitedMaterials = null)
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
    }*/

    public async Task<Result<Guid>> CreateBatchManufacturingRecord(CreateBatchManufacturingRecord request)
    {
        var batchRecord = mapper.Map<BatchManufacturingRecord>(request);
        await context.BatchManufacturingRecords.AddAsync(batchRecord);
        await context.SaveChangesAsync();

        var productionScheduleProduct = await context.ProductionScheduleProducts.FirstOrDefaultAsync(p =>
            p.Id == request.ProductionScheduleProductId);

        if (productionScheduleProduct is not null)
        {
            productionScheduleProduct.BatchNumber = request.BatchNumber;
            context.ProductionScheduleProducts.Update(productionScheduleProduct);
            await context.SaveChangesAsync();
        }

        backgroundWorkerService.EnqueueNotification("Batch manufacturing record created",
            NotificationType.BmrBprRequested);

        return batchRecord.Id;
    }

    public async Task<Result<BatchManufacturingRecordDto>> GetBatchManufacturingRecordByProductionAndScheduleId(
        Guid productionScheduleProductId)
    {
        var batchManufacturingRecord = await context.BatchManufacturingRecords
            .IgnoreQueryFilters()
            .AsSplitQuery()
            .Include(b => b.ProductionScheduleProduct)
            .ThenInclude(b => b.Product)
            .Include(b => b.ProductionScheduleProduct)
            .ThenInclude(b => b.ProductionSchedule)
            .Include(b => b.ProductionScheduleProduct)
            .ThenInclude(b => b.ProductPacking)
            .ThenInclude(p => p.PackingLists)
            .ThenInclude(p => p.Uom)
            .Include(b => b.ProductionScheduleProduct)
            .ThenInclude(b => b.ProductPacking)
            .ThenInclude(p => p.BasePackingUoM)
            .FirstOrDefaultAsync(b => b.ProductionScheduleProductId == productionScheduleProductId);

        return mapper.Map<BatchManufacturingRecordDto>(batchManufacturingRecord);
    }

    public async Task<Result> CreateFinishedGoodsTransferNoteQuantity(
        CreateFinishedGoodsTransferNoteQuantityRequest request,
        Guid userId)
    {
        if (request.Quantity > await GetRemainderOfFinishedGoodsQuantityFromBmr(request.BatchManufacturingRecordId))
        {
            return Error.Validation("Bmr.Quantity",
                "Requested quantity is greater than remaining quantity in bmr");
        }

        var finishedGoodsTransferNote = await context.FinishedGoodsTransferNotes
            .FirstOrDefaultAsync(f =>
                f.BatchManufacturingRecordId == request.BatchManufacturingRecordId);

        if (finishedGoodsTransferNote is not null)
        {
            finishedGoodsTransferNote.Quantities.Add(new FinishedGoodsTransferNoteQuantity
            {
                Quantity = request.Quantity,
                MovedAt = DateTime.UtcNow,
                MovedById = userId,
            });
            await context.SaveChangesAsync();
            return Result.Success();
        }

        await context.FinishedGoodsTransferNotes.AddAsync(new FinishedGoodsTransferNote
        {
            BatchManufacturingRecordId = request.BatchManufacturingRecordId,
        });
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<decimal> GetRemainderOfFinishedGoodsQuantityFromBmr(Guid batchManufacturingRecordId)
    {
        var bmr = await context.BatchManufacturingRecords
            .FirstOrDefaultAsync(b => b.Id == batchManufacturingRecordId);

        if (bmr is null) throw new Exception("Bmr not found");

        var finishedGoodsTransferNote = await context.FinishedGoodsTransferNotes
            .Include(f => f.Quantities)
            .FirstOrDefaultAsync(f => f.BatchManufacturingRecordId == batchManufacturingRecordId);

        if (finishedGoodsTransferNote is null) return bmr.BatchQuantity;

        return bmr.BatchQuantity -
               finishedGoodsTransferNote.Quantities.Sum(q => q.Quantity);
    }

    public async Task<Result> CreateFinishedGoodsTransferNote(CreateFinishedGoodsTransferNoteRequest request, Guid userId)
    {
        var bmr = await context.BatchManufacturingRecords
           .AsSplitQuery()
           .Include(batchManufacturingRecord => batchManufacturingRecord.ProductionScheduleProduct)
           .ThenInclude(p => p.Product)
           .FirstOrDefaultAsync(r => r.Id == request.BatchManufacturingRecordId);

        if (bmr is null)
            return RequisitionErrors.NotFound(request.BatchManufacturingRecordId);

        var product = bmr.ProductionScheduleProduct.Product;
        if (product is null)
            return ProductErrors.NotFound(request.BatchManufacturingRecordId);

        var user = await context.Users
            .AsSplitQuery()
            .Include(user => user.Department)
            .ThenInclude(department => department.Warehouses)
            .ThenInclude(warehouse => warehouse.ArrivalLocation)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user is null)
            return UserErrors.NotFound(userId);

        if (user.Department == null)
            return Error.NotFound("User.Department", "User has no association to any department");

        if (user.Department.Warehouses.Count == 0)
            return Error.NotFound("User.Warehouse",
                "No raw material warehouse is associated with current user");

        var productionWarehouse = user.Department.Warehouses.FirstOrDefault(i => i.Type == WarehouseType.Production);
        if (productionWarehouse is null)
            return Error.NotFound("User.Warehouse", "No production warehouse is associated with current user");

        var finishedGoodsWarehouse =
            await context.Warehouses
                .AsSplitQuery()
                .IgnoreQueryFilters()
                .Include(warehouse => warehouse.ArrivalLocation)
                .FirstOrDefaultAsync(w =>
                    w.Type == WarehouseType.FinishedGoodsStorage && w.Division == product.Division);
        if (finishedGoodsWarehouse is null)
            return Error.NotFound("User.Warehouse",
                "No finished goods warehouses found in the system");

        FinishedGoodsTransferNote transferNote = null;

        if (await context.FinishedGoodsTransferNotes.AnyAsync(f
                => f.BatchManufacturingRecordId == bmr.Id))
        {
            transferNote = await context.FinishedGoodsTransferNotes
                .FirstOrDefaultAsync(f => f.BatchManufacturingRecordId == bmr.Id);
        }

        if (transferNote is null)
        {
            transferNote = mapper.Map<FinishedGoodsTransferNote>(request);
            transferNote.ToWarehouseId = finishedGoodsWarehouse.Id;
            transferNote.FromWarehouseId = productionWarehouse.Id;
            context.FinishedGoodsTransferNotes.Add(transferNote);
        }
        else
        {
            mapper.Map(transferNote, request);
            transferNote.ToWarehouseId = finishedGoodsWarehouse.Id;
            transferNote.FromWarehouseId = productionWarehouse.Id;
            context.FinishedGoodsTransferNotes.Update(transferNote);
        }

        var movement = new FinishedProductBatchMovement
        {
            ProductId = bmr.ProductionScheduleProduct.ProductId,
            FromWarehouseId = productionWarehouse.Id,
            ToWarehouseId = finishedGoodsWarehouse.Id,
            Quantity = transferNote.TotalQuantity,
            MovedAt = DateTime.UtcNow,
            MovedById = userId
        };

        await context.FinishedProductBatchMovements.AddAsync(movement);

        var batchEvent = new FinishedProductBatchEvent
        {
            ProductId = bmr.ProductionScheduleProduct.ProductId,
            Type = EventType.Moved,
            Quantity = request.TotalQuantity,
            UserId = userId
        };
        await context.FinishedProductBatchEvents.AddAsync(batchEvent);

        var binCardEvent = new ProductBinCardInformation
        {
            BatchId = bmr.Id,
            Description = finishedGoodsWarehouse.Name,
            WayBill = "N/A",
            ArNumber = "N/A",
            QuantityReceived = request.TotalQuantity,
            QuantityIssued = 0,
            BalanceQuantity = (await materialRepository
                .GetProductStockInWarehouseByBatch(bmr.ProductionScheduleProduct.ProductId,
                    finishedGoodsWarehouse.Id)).Value + request.TotalQuantity,
            UoMId = bmr.ProductionScheduleProduct.Product.BaseUomId,
            CreatedAt = DateTime.UtcNow
        };

        await context.ProductBinCardInformation.AddAsync(binCardEvent);

        if (finishedGoodsWarehouse.ArrivalLocation == null)
        {
            finishedGoodsWarehouse.ArrivalLocation = new WarehouseArrivalLocation
            {
                WarehouseId = finishedGoodsWarehouse.Id,
                Name = "Default Arrival Location",
                FloorName = "Ground Floor",
                Description = "Automatically created arrival location"
            };
            await context.WarehouseArrivalLocations.AddAsync(finishedGoodsWarehouse.ArrivalLocation);
        }

        // Create distributed product record
        var distributedFinishedProduct = new DistributedFinishedProduct
        {
            ProductId = bmr.ProductionScheduleProduct.ProductId,
            TransferNoteId = transferNote.Id,
            UomId = bmr.ProductionScheduleProduct.Product.BaseUomId,
            Quantity = request.TotalQuantity,
            BatchManufacturingRecordId = bmr.Id,
            Status = DistributedFinishedProductStatus.Distributed,
            DistributedAt = DateTime.UtcNow,
            WarehouseArrivalLocationId = finishedGoodsWarehouse.ArrivalLocation.Id
        };

        await context.DistributedFinishedProducts.AddAsync(distributedFinishedProduct);

        await context.SaveChangesAsync();

        var productionActivityStep =
            await context.ProductionActivitySteps
                .FirstOrDefaultAsync(p => p.Id == request.ProductionActivityStepId);

        if (productionActivityStep is not null)
        {
            productionActivityStep.StartedAt = DateTime.UtcNow;
            productionActivityStep.CompletedAt = DateTime.UtcNow;
            productionActivityStep.Status = ProductionStatus.Completed;
            context.ProductionActivitySteps.Update(productionActivityStep);
            await context.SaveChangesAsync();
        }
        return Result.Success();
    }

    public async Task<Result<Paginateable<IEnumerable<FinishedGoodsTransferNoteDto>>>> GetFinishedGoodsTransferNote(
        bool? onlyApproved,
        int page,
        int pageSize,
        string searchQuery = null,
        Division? division = null)
    {
        var query = context.FinishedGoodsTransferNotes
            .AsSplitQuery()
            .IgnoreQueryFilters()
            .Include(b => b.BatchManufacturingRecord)
            .ThenInclude(b => b.ProductionScheduleProduct)
            .ThenInclude(b => b.Product)
            .Include(b => b.FromWarehouse)
            .Include(b => b.ToWarehouse)
            .Include(tn => tn.ProductPacking)
            .ThenInclude(p => p.PackingLists)
            .Include(tn => tn.ProductPacking)
            .ThenInclude(p => p.BasePackingUoM)
            .AsQueryable();

        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.WhereSearch(searchQuery, b => b.QarNumber);
        }

        if (division.HasValue)
        {
            query = query.Where(q =>
                q.BatchManufacturingRecord.ProductionScheduleProduct.Product.Division == division.Value);
        }

        if (onlyApproved.HasValue)
        {
            query = onlyApproved.Value ?
                query.Where(q => q.IsApproved) :
                query.Where(q => !q.IsApproved);
        }

        return await PaginationHelper.GetPaginatedResultAsync(
            query,
            page,
            pageSize,
            mapper.Map<FinishedGoodsTransferNoteDto>
        );
    }

    public async Task<Result<Paginateable<IEnumerable<ProductBinCardInformationDto>>>> GetProductBinCardInformation(int page, int pageSize,
        string searchQuery, Guid productId)
    {
        var query = context.ProductBinCardInformation
            .AsSplitQuery()
            .Include(bci => bci.Batch)
            .ThenInclude(mb => mb.ProductionScheduleProduct)
            .Include(bci => bci.UoM)
            .Where(bci => bci.Batch.ProductionScheduleProduct.ProductId == productId)
            .AsQueryable();

        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.WhereSearch(searchQuery, b => b.Description);
        }

        return await PaginationHelper.GetPaginatedResultAsync(
            query,
            page,
            pageSize,
            mapper.Map<ProductBinCardInformationDto>
        );
    }


    public async Task<Result<FinishedGoodsTransferNoteDto>> GetFinishedGoodsTransferNote(Guid id)
    {
        var transferNote = await context.FinishedGoodsTransferNotes
            .AsSplitQuery()
            .IgnoreQueryFilters()
            .Include(b => b.BatchManufacturingRecord)
            .ThenInclude(b => b.ProductionScheduleProduct)
            .ThenInclude(b => b.Product)
            .Include(b => b.FromWarehouse)
            .Include(u => u.UoM)
            .Include(b => b.ToWarehouse)
            .Include(tn => tn.ProductPacking)
            .ThenInclude(p => p.PackingLists)
            .Include(tn => tn.ProductPacking)
            .ThenInclude(p => p.BasePackingUoM)
            .Include(b => b.CreatedBy)
            .FirstOrDefaultAsync(f => f.Id == id);

        return transferNote is null ?
            Error.NotFound("TransferNote.NotFound", "Transfer note not found") :
            mapper.Map<FinishedGoodsTransferNoteDto>(transferNote);
    }

    public async Task<Result<Paginateable<IEnumerable<FinishedGoodsTransferNoteDto>>>>
        GetFinishedGoodsTransferNoteByProduct(
            Guid departmentId,
            int page,
            int pageSize,
            string searchQuery,
            Guid productId)
    {
        var query = context.FinishedGoodsTransferNotes
            .AsSplitQuery()
            .IgnoreQueryFilters()
            .Include(b => b.BatchManufacturingRecord)
            .ThenInclude(b => b.ProductionScheduleProduct)
            .ThenInclude(b => b.Product)
            .Include(b => b.FromWarehouse)
            .Include(u => u.UoM)
            .Include(b => b.ToWarehouse)
            .Include(tn => tn.ProductPacking)
            .ThenInclude(p => p.PackingLists)
            .Include(tn => tn.ProductPacking)
            .ThenInclude(p => p.BasePackingUoM)
            .Where(f => f.BatchManufacturingRecord.ProductionScheduleProduct.ProductId == productId && (f.ToWarehouse.DepartmentId == departmentId || f.FromWarehouse.DepartmentId == departmentId))
            .AsQueryable();

        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.WhereSearch(searchQuery, b => b.QarNumber, b => b.TransferNoteNumber);
        }

        return await PaginationHelper.GetPaginatedResultAsync(
            query,
            page,
            pageSize,
            mapper.Map<FinishedGoodsTransferNoteDto>
        );
    }

    public async Task<Result> ApproveTransferNote(Guid id, ApproveTransferNoteRequest request)
    {
        var transferNote = await context.FinishedGoodsTransferNotes.FirstOrDefaultAsync(f => f.Id == id);

        if (transferNote == null) return Error.NotFound("TransferNote.NotFound", "Transfer note not found");

        transferNote.IsApproved = true;
        transferNote.QuantityReceived = request.QuantityReceived;
        transferNote.Notes = request.Notes;
        //transferNote.Loose = request.Loose;

        context.FinishedGoodsTransferNotes.Update(transferNote);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> UpdateTransferNote(Guid id, CreateFinishedGoodsTransferNoteRequest request)
    {
        var transferNote = await context.FinishedGoodsTransferNotes.FirstOrDefaultAsync(f => f.Id == id);
        if (transferNote == null) return Error.NotFound("TransferNote.NotFound", "Transfer note not found");

        if (!transferNote.IsApproved) return Error.Validation("TransferNote.NotApproved", "Cannot edit transfer note that is not approved");

        var finishedGoodTransferNote = mapper.Map<FinishedGoodsTransferNote>(request);
        context.FinishedGoodsTransferNotes.Update(finishedGoodTransferNote);
        await context.SaveChangesAsync();

        return Result.Success();

    }

    public async Task<Result<IEnumerable<ApprovedProductDto>>> GetApprovedProducts()
    {
        var productsQuery = context.FinishedGoodsTransferNotes
            .AsSplitQuery()
            .Include(tn => tn.BatchManufacturingRecord)
            .ThenInclude(b => b.ProductionScheduleProduct)
            .ThenInclude(b => b.Product)
            .Include(tn => tn.ProductPacking)
            .ThenInclude(p => p.PackingLists)
            .Include(tn => tn.ProductPacking)
            .ThenInclude(p => p.BasePackingUoM)
            .Where(p => p.IsApproved)
            .AsQueryable();

        var products = await productsQuery.ToListAsync();

        return products
            .GroupBy(p => p.BatchManufacturingRecord.ProductionScheduleProduct)
            .Select(item => new ApprovedProductDto
            {
                Product = mapper.Map<ProductListDto>(item.Key.Product),
                TotalQuantity = item.Sum(p => p.QuantityReceived),
                TotalRemainingQuantity = item.Sum(p => p.RemainingQuantity),
                QuantityPerPack = item.Select(p => p.QuantityPerPack).First(),
                TotalLoose = item.Sum(p => p.Loose),
            }).ToList();
    }

    public async Task<Result<ApprovedProductDetailDto>> GetApprovedProduct(Guid productId)
    {
        var products = await context.FinishedGoodsTransferNotes
            .AsSplitQuery()
            .IgnoreQueryFilters()
            .Include(tn => tn.BatchManufacturingRecord)
            .ThenInclude(b => b.ProductionScheduleProduct)
            .ThenInclude(b => b.Product)
            .ThenInclude(p => p.Packings).ThenInclude(p => p.PackingLists)
            .Include(tn => tn.ProductPacking)
            .ThenInclude(p => p.PackingLists)
            .Include(tn => tn.ProductPacking)
            .ThenInclude(p => p.BasePackingUoM)
            .Where(p => p.IsApproved && p.BatchManufacturingRecord.ProductionScheduleProduct.ProductId == productId)
            .ToListAsync();

        if (products.Count == 0)
            return Error.NotFound("Product", "No `approved` finished good transfer notes for this product found");

        // get details
        var finishedGoodsTransferNoteResult = await GetApprovedProductDetails(productId);
        if (finishedGoodsTransferNoteResult.IsFailure)
            return finishedGoodsTransferNoteResult.Errors;

        var grouped = products.GroupBy(p => p.BatchManufacturingRecord.ProductionScheduleProduct).First();

        var dto = new ApprovedProductDetailDto
        {
            Product = mapper.Map<ProductListDto>(grouped.Key.Product),
            TotalQuantity = grouped.Sum(p => p.QuantityReceived),
            TotalRemainingQuantity = grouped.Sum(p => p.RemainingQuantity),
            QuantityPerPack = grouped.Select(p => p.QuantityPerPack).First(),
            TotalLoose = grouped.Sum(p => p.Loose),
            FinishedGoodsTransferNotes = finishedGoodsTransferNoteResult.Value.ToList()
        };

        return dto;
    }

    public async Task<Result<IEnumerable<FinishedGoodsTransferNoteDto>>> GetApprovedProductDetails(Guid productId)
    {
        var finishedGoods = await context.FinishedGoodsTransferNotes
            .IgnoreQueryFilters()
            .AsSplitQuery()
            .Include(tn => tn.BatchManufacturingRecord)
            .ThenInclude(b => b.ProductionScheduleProduct)
            .Include(tn => tn.ProductPacking)
            .ThenInclude(p => p.PackingLists)
            .Include(tn => tn.ProductPacking)
            .ThenInclude(p => p.BasePackingUoM)
            .Where(p => p.BatchManufacturingRecord.ProductionScheduleProduct.ProductId == productId && p.IsApproved)
            .ToListAsync();

        return mapper.Map<List<FinishedGoodsTransferNoteDto>>(finishedGoods);
    }


    /*public async Task<Result> AllocateProduct(AllocateProductionOrder request)
    {
        var productionOrder =  await context.ProductionOrders
            .AsSplitQuery()
            .Include(p => p.Products)
            .FirstOrDefaultAsync(f => f.Id == request.ProductionOrderId);
        if (productionOrder == null) return Error.NotFound("ProductionOrder.NotFound", "Production order not found");

        foreach (var product in request.Products)
        {
            var allocationProduct = productionOrder.Products.FirstOrDefault(p  => p.ProductId == product.ProductId);
            if (allocationProduct == null) return 
                Error.NotFound("ProductionOrder.ProductNotFound", 
                    $"Product {product.ProductId} not found in this production order");

            if (allocationProduct.RemainingQuantity == 0)
                return
                    Error.Validation("ProductionOrder.Product", 
                        $"Product {product.ProductId} has already been allocated completely.");

            if (allocationProduct.Fulfilled)
                return Error.Validation("ProductionOrder.Product", 
                    "Product has already been marked as fulfilled.");

            if (product.FulfilledQuantites.Sum(q => q.Quantity) > allocationProduct.RemainingQuantity)
            {
                return Error.Validation("ProductionOrder.Product",
                    $"Allocation quantity {product.FulfilledQuantites.Sum(q => q.Quantity)} is more than what is left to be fulfilled {allocationProduct.RemainingQuantity}");
            }

            foreach (var quantityToFulfill in product.FulfilledQuantites)
            {
                var finishedGoodsTransferNote = await context.FinishedGoodsTransferNotes
                    .FirstOrDefaultAsync(f => f.Id == quantityToFulfill.FinishedGoodsTransferNoteId);
                if(finishedGoodsTransferNote is null) 
                    return Error.NotFound("ProductionOrder.FinishedGoodsTransferNoteNotFound", 
                        "Finished goods transfer note not found.");
                if(finishedGoodsTransferNote.RemainingQuantity == 0)
                    return Error.Validation("ProductionOrder.FinishedGoodsTransferNoteValidation",
                        $"The finished good transfer note {quantityToFulfill.FinishedGoodsTransferNoteId} does not have any remaining quantity.");
                
                var existingAllocationProductForNote = allocationProduct
                    .FulfilledQuantities
                    .FirstOrDefault(p => p.FinishedGoodsTransferNoteId == quantityToFulfill.FinishedGoodsTransferNoteId);

                if (existingAllocationProductForNote is not null)
                {
                    productionOrder.Products.First(p => p.ProductId == product.ProductId)
                            .FulfilledQuantities
                            .First(q => q.FinishedGoodsTransferNoteId ==
                                        quantityToFulfill.FinishedGoodsTransferNoteId).Quantity +=
                        quantityToFulfill.Quantity;
                    context.ProductionOrders.Update(productionOrder);
                    continue;
                }

                productionOrder.Products.First(p => p.ProductId == product.ProductId)
                    .FulfilledQuantities.Add(new ProductionOrderProductQuantity
                    {
                        Quantity = quantityToFulfill.Quantity,
                        FinishedGoodsTransferNoteId = quantityToFulfill.FinishedGoodsTransferNoteId
                    });
                context.ProductionOrders.Update(productionOrder);
                
                finishedGoodsTransferNote.AllocatedQuantity += quantityToFulfill.Quantity;
                context.FinishedGoodsTransferNotes.Update(finishedGoodsTransferNote);
            }
        }
        await context.SaveChangesAsync();
        
        var updatedProductionOrder = await context.ProductionOrders
            .AsSplitQuery()
            .Include(p => p.Products)
            .FirstAsync(f => f.Id == request.ProductionOrderId);

        foreach (var product in request.Products)
        {
            var updatedAllocationProduct = updatedProductionOrder.Products.First(p  => p.ProductId == product.ProductId);
            if (updatedAllocationProduct.RemainingQuantity == 0)
            {
                updatedProductionOrder.Products.First(p => p.ProductId == product.ProductId).Fulfilled = true;
                context.ProductionOrders.Update(updatedProductionOrder);
            }
        }
        await context.SaveChangesAsync();
        return Result.Success();
    }*/

    public async Task<Result<Paginateable<IEnumerable<BatchManufacturingRecordDto>>>> GetBatchManufacturingRecords(int page, int pageSize, string searchQuery = null, ProductionStatus? status = null)
    {
        var query = context.BatchManufacturingRecords
            .AsSplitQuery()
            .Include(b => b.CreatedBy)
            .Include(p => p.ProductionActivityStep)
            .Include(p => p.ProductionScheduleProduct)
            .ThenInclude(p => p.Product)
            .Include(p => p.ProductionScheduleProduct)
            .ThenInclude(p => p.ProductionSchedule)
            .Where(p => !p.ProductionActivityStep.CompletedAt.HasValue)
            .AsQueryable();

        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.WhereSearch(searchQuery, p => p.BatchNumber, p => p.ProductionScheduleProduct.Product.Name);
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
                .AsSplitQuery()
                .IgnoreQueryFilters()
                .Include(b => b.CreatedBy)
                .Include(p => p.ProductionActivityStep)
                .Include(p => p.ProductionScheduleProduct)
                .ThenInclude(p => p.Product)
                .Include(p => p.ProductionScheduleProduct)
                .ThenInclude(p => p.ProductPacking)
                .ThenInclude(p => p.BasePackingUoM)
                .Include(p => p.ProductionScheduleProduct)
                .ThenInclude(p => p.ProductPacking)
                .ThenInclude(p => p.PackingLists)
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


        var batchPackingRecord = await context.BatchPackagingRecords
            .AsSplitQuery()
            .Include(batchPackagingRecord => batchPackagingRecord.ProductionActivityStep)
            .FirstOrDefaultAsync(p => p.ProductionActivityStepId == batchRecord.ProductionActivityStepId);
        if (batchPackingRecord is not null)
        {
            mapper.Map(request, batchPackingRecord);
            batchPackingRecord.ProductionActivityStep.Status = ProductionStatus.InProgress;
            batchPackingRecord.ProductionActivityStep.StartedAt = DateTime.UtcNow;
            context.BatchPackagingRecords.Update(batchPackingRecord);
        }

        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> IssueBatchManufacturingRecord(Guid id, Guid userId)
    {
        var batchRecord = await context.BatchManufacturingRecords
            .IgnoreQueryFilters()
            .Include(batchManufacturingRecord => batchManufacturingRecord.ProductionActivityStep).FirstOrDefaultAsync(p => p.Id == id);
        if (batchRecord is null)
        {
            return ProductErrors.NotFound(id);
        }

        batchRecord.ProductionActivityStep.Status = ProductionStatus.Completed;
        batchRecord.ProductionActivityStep.CompletedAt = DateTime.UtcNow;
        batchRecord.IssuedById = userId;
        batchRecord.IssuedDate = DateTime.UtcNow;
        context.BatchManufacturingRecords.Update(batchRecord);
        await context.ProductionActivityLogs.AddAsync(new ProductionActivityLog
        {
            ProductionActivityId = batchRecord.ProductionActivityStep.ProductionActivityId,
            UserId = userId,
            Message = "Issued batch manufacturing record",
            Timestamp = DateTime.UtcNow
        });

        var batchPackingRecord = await context.BatchPackagingRecords
            .AsSplitQuery()
            .Include(batchPackagingRecord => batchPackagingRecord.ProductionActivityStep)
            .FirstOrDefaultAsync(p => p.ProductionActivityStepId == batchRecord.ProductionActivityStepId);
        if (batchPackingRecord is not null)
        {
            batchPackingRecord.IssuedDate = DateTime.UtcNow;
            context.BatchPackagingRecords.Update(batchPackingRecord);

            await context.ProductionActivityLogs.AddAsync(new ProductionActivityLog
            {
                ProductionActivityId = batchRecord.ProductionActivityStep.ProductionActivityId,
                UserId = userId,
                Message = "Issued batch packing record",
                Timestamp = DateTime.UtcNow
            });
        }

        await context.SaveChangesAsync();
        backgroundWorkerService.EnqueueNotification("Batch manufacturing issued", NotificationType.BmrBprApproved);

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
            .AsSplitQuery()
            .Include(b => b.CreatedBy)
            .Include(p => p.ProductionActivityStep)
            .Include(p => p.ProductionScheduleProduct)
            .ThenInclude(p => p.Product)
            .Include(p => p.ProductionScheduleProduct)
            .ThenInclude(p => p.ProductionSchedule)
            .Include(p => p.ProductPacking)
            .ThenInclude(pp => pp.PackingLists)
            .AsQueryable();

        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.WhereSearch(searchQuery, p => p.BatchNumber, p => p.ProductionScheduleProduct.Product.Name);
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
                .AsSplitQuery()
                .Include(b => b.CreatedBy)
                .Include(p => p.ProductionActivityStep)
                .Include(p => p.ProductionScheduleProduct)
                .ThenInclude(p => p.Product)
                .Include(p => p.ProductionScheduleProduct)
                .ThenInclude(p => p.ProductionSchedule)
                .Include(p => p.ProductPacking)
                .ThenInclude(p => p.PackingLists.OrderBy(pp => pp.Order))
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
        await context.ProductionActivityLogs.AddAsync(new ProductionActivityLog
        {
            ProductionActivityId = batchRecord.ProductionActivityStep.ProductionActivityId,
            UserId = userId,
            Message = "Issued batch packaging record",
            Timestamp = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        backgroundWorkerService.EnqueueNotification("Batch packaging issued", NotificationType.BmrBprApproved);

        return Result.Success();
    }

    public async Task FreezeMaterialInProduction(Guid productionScheduleProductId)
    {
        var materialResult = await CheckMaterialStockLevelsForProductionSchedule(productionScheduleProductId, null);
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
                    var result = await materialRepository.ReserveQuantityFromBatchForProduction(batch.Batch.Id, material.ProductionWarehouseId, productionScheduleProductId,
                        batch.QuantityToTake, batch.Batch.UoM?.Id, batch.WarehouseLocationShelfId);
                    if (result.IsFailure) throw new Exception($"Unable to freeze material in production schedule. Error: {result.Error.Description}");
                }
            }
        }

        var packageMaterialResult = await CheckPackageMaterialStockLevelsForProductionSchedule(productionScheduleProductId, null);
        if (packageMaterialResult.IsFailure) return;

        var packageMaterialDetails = packageMaterialResult.Value;

        foreach (var material in packageMaterialDetails)
        {
            var batchResult = await materialRepository
                .BatchesToSupplyForGivenQuantity(material.Material.Id, material.StorageWarehouseId,
                material.QuantityNeeded);

            if (batchResult.IsSuccess)
            {
                var batches = batchResult.Value;
                foreach (var batch in batches)
                {
                    await materialRepository.ReserveQuantityFromBatchForProduction(batch.Batch.Id, 
                        material.ProductionWarehouseId, productionScheduleProductId,
                        batch.QuantityToTake, batch.Batch.UoM?.Id, batch.WarehouseLocationShelfId);
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
            .Include(s => s.UoM)
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
            .IgnoreQueryFilters()
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

    public async Task<Result<Paginateable<IEnumerable<DepartmentStockTransferDto>>>> GetIncomingStockTransferRequestForUserDepartment(Guid userId, int page, int pageSize, string searchQuery = null,
        StockTransferStatus? status = null, Guid? toDepartmentId = null)
    {

        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null)
            return UserErrors.NotFound(userId);

        var query = context.StockTransferSources
            .IgnoreQueryFilters()
            .AsSplitQuery()
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


    public async Task<Result<Paginateable<IEnumerable<DepartmentStockTransferDto>>>> GetOutgoingStockTransferRequestForUserDepartment(Guid userId, int page, int pageSize, string searchQuery = null,
        StockTransferStatus? status = null, Guid? fromDepartmentId = null)
    {

        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null)
            return UserErrors.NotFound(userId);

        var query = context.StockTransferSources
            .IgnoreQueryFilters()
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

        var toWarehouse = stockTransferSource.StockTransfer.Material.Kind == MaterialKind.Raw
            ? await context.Warehouses.IgnoreQueryFilters()
                .Include(w => w.ArrivalLocation)
                .FirstOrDefaultAsync(w =>
                    w.DepartmentId == stockTransferSource.ToDepartmentId &&
                    w.Type == WarehouseType.RawMaterialStorage)
            : await context.Warehouses.IgnoreQueryFilters()
                .Include(w => w.ArrivalLocation)
                .FirstOrDefaultAsync(w =>
                    w.DepartmentId == stockTransferSource.ToDepartmentId && w.Type == WarehouseType.PackagedStorage);

        var remainingQuantity = stockTransferSource.Quantity;

        foreach (var batchRequest in batches)
        {
            var batch = await context.MaterialBatches.FirstOrDefaultAsync(b => b.Id == batchRequest.BatchId);

            if (batch == null || batch.RemainingQuantity < batchRequest.Quantity)
            {
                return Error.Failure("Batch.InsufficientStock", $"Not enough stock in batch {batchRequest.BatchId}");
            }

            batch.QuantityAssigned = 0;
            var shelfMaterialBatches =
                await context.ShelfMaterialBatches
                    .IgnoreQueryFilters()
                    .OrderBy(s => s.Quantity)
                    .Where(sb => sb.MaterialBatchId == batch.Id
                                 && sb.WarehouseLocationShelf.WarehouseLocationRack.WarehouseLocation.Warehouse.DepartmentId == stockTransferSource.FromDepartmentId
                                 && !sb.DeletedAt.HasValue)
                    .ToListAsync();

            var totalAvailable = shelfMaterialBatches.Sum(sb => sb.Quantity);

            if (totalAvailable < batchRequest.Quantity)
            {
                return Error.Failure("Batch.InsufficientStock", $"Not enough stock in batch {batchRequest.BatchId}");
            }

            var quantityToDeduct = batchRequest.Quantity;

            foreach (var shelfMaterialBatch in shelfMaterialBatches)
            {
                if (quantityToDeduct <= 0) break;

                var deductAmount = Math.Min(shelfMaterialBatch.Quantity, quantityToDeduct);
                shelfMaterialBatch.Quantity -= deductAmount;
                quantityToDeduct -= deductAmount;

                if (shelfMaterialBatch.Quantity <= 0)
                {
                    context.ShelfMaterialBatches.Remove(shelfMaterialBatch);
                }
            }

            remainingQuantity -= batchRequest.Quantity;
            if (remainingQuantity <= 0) break;
        }

        if (remainingQuantity > 0)
        {
            return Error.Failure("StockTransfer.InsufficientStock", "Not enough batches to fulfill the transfer");
        }

        var holdingMaterial = new HoldingMaterialTransfer
        {
            ModelType = nameof(StockTransfer),
            StockTransferId = stockTransferSource.StockTransferId,
            Batches = batches.Select(b => new HoldingMaterialTransferBatch
            {
                MaterialBatchId = b.BatchId,
                Quantity = b.Quantity,
                SourceWarehouseId = fromWarehouse.Id,
                DestinationWarehouseId = toWarehouse.Id,
            }).ToList()
        };

        await context.HoldingMaterialTransfers.AddAsync(holdingMaterial);
        stockTransferSource.IssuedAt = DateTime.UtcNow;
        stockTransferSource.IssuedById = userId;
        stockTransferSource.Status = StockTransferStatus.Issued;
        context.StockTransferSources.Update(stockTransferSource);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result<List<ProductionScheduleProcurementDto>>> GetMaterialsWithInsufficientStock(Guid productionScheduleProductId)
    {
        var materialStockDetails =
            await CheckMaterialStockLevelsForProductionSchedule(productionScheduleProductId, MaterialRequisitionStatus.None);

        if (!materialStockDetails.IsSuccess)
        {
            return materialStockDetails.Error;
        }

        return materialStockDetails.Value;
    }

    public async Task<Result<List<ProductionScheduleProcurementPackageDto>>> GetPackageMaterialsWithInsufficientStock(Guid productionScheduleProductId)
    {
        var materialStockDetails =
            await CheckPackageMaterialStockLevelsForProductionSchedule(productionScheduleProductId, null);

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
            .Include(fp => fp.ProductionScheduleProduct)
            .ThenInclude(fp => fp.ProductionSchedule)
            .Include(fp => fp.ProductionScheduleProduct)
            .ThenInclude(fp => fp.Product)
            .Include(fp => fp.Materials).ThenInclude(m => m.Material)
            .Include(fp => fp.ProductPacking)
            .FirstOrDefaultAsync(fp => fp.Id == finalPackingId);

        return mapper.Map<FinalPackingDto>(finalPacking);
    }

    /// ✅ **Extra Method: Get Final Packing by ProductionScheduleId & ProductId**
    public async Task<Result<FinalPackingDto>> GetFinalPackingByScheduleProduct(Guid productionScheduleProductId)
    {
        var finalPacking = await context.FinalPackings
            .AsSplitQuery()
            .Include(fp => fp.ProductionScheduleProduct)
            .ThenInclude(fp => fp.ProductionSchedule)
            .Include(fp => fp.ProductionScheduleProduct)
            .ThenInclude(fp => fp.Product)
            .Include(fp => fp.Materials).ThenInclude(m => m.Material)
            .Include(fp => fp.ProductPacking)
            .FirstOrDefaultAsync(fp => fp.ProductionScheduleProductId == productionScheduleProductId);

        return mapper.Map<FinalPackingDto>(finalPacking);
    }

    /// ✅ **Paginated List of Final Packings**
    public async Task<Result<Paginateable<IEnumerable<FinalPackingDto>>>> GetFinalPackings(int page, int pageSize, string searchQuery)
    {
        var query = context.FinalPackings
            .AsSplitQuery()
            .Include(fp => fp.ProductionScheduleProduct)
            .ThenInclude(fp => fp.ProductionSchedule)
            .Include(fp => fp.ProductionScheduleProduct)
            .ThenInclude(fp => fp.Product)
            .Include(fp => fp.ProductPacking)
            .AsQueryable();

        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.Where(fp => fp.ProductionScheduleProduct.Product.Name.Contains(searchQuery) ||
                                      fp.ProductionScheduleProduct.Product.Code.Contains(searchQuery));
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

    public async Task<Result<RequisitionDto>> GetStockRequisitionForRaw(Guid productionScheduleProductId)
    {
        var requisition = await context.Requisitions
            .AsSplitQuery()
            .Include(r => r.Items)
            .ThenInclude(i => i.Material)
            .Include(r => r.RequestedBy)
            .Include(r => r.Approvals).ThenInclude(a => a.User)
            .Include(r => r.Approvals).ThenInclude(a => a.Role)
            .Where(r => r.ProductionScheduleProductId == productionScheduleProductId
                        && r.Code.StartsWith("RM") // Ensure the code starts with "RM"
                        && r.RequisitionType == RequisitionType.Stock) // Ensure it's a stock requisition
            .FirstOrDefaultAsync();

        return mapper.Map<RequisitionDto>(requisition);
    }

    public async Task<Result<RequisitionDto>> GetStockRequisitionForPackaging(Guid productionScheduleProductId)
    {
        var requisition = await context.Requisitions
            .AsSplitQuery()
            .Include(r => r.Items)
            .ThenInclude(i => i.Material)
            .Include(r => r.RequestedBy)
            .Include(r => r.Approvals).ThenInclude(a => a.User)
            .Include(r => r.Approvals).ThenInclude(a => a.Role)
            .Where(r => r.ProductionScheduleProductId == productionScheduleProductId
                        && r.Code.StartsWith("PM") // Ensure the code starts with "package"
                        && r.RequisitionType == RequisitionType.Stock) // Ensure it's a stock requisition
            .FirstOrDefaultAsync();

        return mapper.Map<RequisitionDto>(requisition);
    }

    public async Task<Result<ProductionScheduleProductDto>> GetProductDetailsInProductionSchedule(
        Guid productionScheduleProductId)
    {
        var product = await context.ProductionScheduleProducts
            .AsSplitQuery()
            .Include(p => p.Product)
            .Where(p => p.Id == productionScheduleProductId)
            .Select(p => mapper.Map<ProductionScheduleProductDto>(p))
            .FirstOrDefaultAsync();

        return product;
    }

    public async Task<Result> ReturnStockBeforeProductionBegins(Guid productionScheduleProductId, string reason)
    {
        var productionScheduleProduct = await context.ProductionScheduleProducts
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Id == productionScheduleProductId);
        if (productionScheduleProduct is null) return ProductErrors.NotFound(productionScheduleProductId);

        var product = await context.Products.IgnoreQueryFilters()
            .AsSplitQuery()
            .FirstOrDefaultAsync(p => p.Id == productionScheduleProduct.ProductId);
        if (product is null) return ProductErrors.NotFound(productionScheduleProduct.ProductId);

        var productionSchedule = await context.ProductionSchedules
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Id == productionScheduleProduct.ProductionScheduleId);
        if (productionSchedule is null) return ProductErrors.NotFound(productionScheduleProduct.ProductionScheduleId);

        var productionWarehouse = await context.Warehouses
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(w => w.DepartmentId == product.DepartmentId && w.Type == WarehouseType.Production);
        if (productionWarehouse is null) return Error.NotFound("Warehouse.Production", "Warehouse production record not found");

        var rawStorageWarehouse = await context.Warehouses
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(w => w.DepartmentId == product.DepartmentId && w.Type == WarehouseType.RawMaterialStorage);
        if (rawStorageWarehouse is null) return Error.NotFound("Warehouse.RawStorage", "Warehouse raw storage record not found");

        var packedStorageWarehouse = await context.Warehouses
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(w => w.DepartmentId == product.DepartmentId && w.Type == WarehouseType.PackagedStorage);
        if (packedStorageWarehouse is null) return Error.NotFound("Warehouse.PackedStorage", "Warehouse package storage record not found");

        var stockRequisitions = await context.Requisitions
            .AsSplitQuery()
            .Include(r => r.Items)
            .Where(r => r.ProductionScheduleProductId == productionScheduleProductId).ToListAsync();

        var materialReturnNote = new MaterialReturnNote
        {
            ProductionScheduleProductId = productionScheduleProductId,
            ReturnDate = DateTime.UtcNow,
            BatchNumber = (await context.BatchManufacturingRecords
                .FirstOrDefaultAsync(b => b.ProductionScheduleProductId == productionScheduleProductId))?.BatchNumber,
        };
        await context.MaterialReturnNotes.AddAsync(materialReturnNote);
        await context.SaveChangesAsync();

        foreach (var stockRequisition in stockRequisitions)
        {
            foreach (var item in stockRequisition.Items)
            {
                var batchesToConsume =
                    await materialRepository.GetReservedBatchesAndQuantityForProductionWarehouse(
                        item.MaterialId,
                        productionWarehouse.Id, productionScheduleProduct.Id);

                var material = await context.Materials.FirstOrDefaultAsync(m => m.Id == item.MaterialId);

                if (material is null) return Error.NotFound("material", "material not found");

                var fullReturns = batchesToConsume.Select(b => new MaterialReturnNoteFullReturn
                {
                    MaterialReturnNoteId = materialReturnNote.Id,
                    MaterialBatchReservedQuantityId = b.Id,
                    DestinationWarehouseId = material.Kind == MaterialKind.Raw
                        ? rawStorageWarehouse.Id
                        : packedStorageWarehouse.Id,
                    SourceWarehouseLocationShelfId = b.WarehouseLocationShelf?.Id
                }).ToList();

                var batchesToRemove = await context.MaterialBatchReservedQuantities
                    .Where(b => batchesToConsume.Select(bc => bc.Id).Contains(b.Id))
                    .ToListAsync();

                await context.MaterialReturnNoteFullReturns.AddRangeAsync(fullReturns);
                await context.SaveChangesAsync();
                context.MaterialBatchReservedQuantities.RemoveRange(batchesToRemove);
            }
        }

        productionScheduleProduct.ReasonForCancellation = reason;
        productionScheduleProduct.Cancelled = true;
        context.ProductionScheduleProducts.Update(productionScheduleProduct);
        var bmr = await context.BatchManufacturingRecords.FirstOrDefaultAsync(b => b.ProductionScheduleProductId == productionScheduleProduct.Id);
        if (bmr is not null)
        {
            context.BatchManufacturingRecords.Remove(bmr);
        }
        var bpr = await context.BatchPackagingRecords.FirstOrDefaultAsync(p => p.ProductionScheduleProductId == productionScheduleProduct.Id);
        if (bpr is not null)
        {
            context.BatchPackagingRecords.Remove(bpr);
        }
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> ReturnLeftOverStockAfterProductionEnds(Guid productionScheduleProductId, List<PartialMaterialToReturn> returns)
    {
        var productionScheduleProduct = await context.ProductionScheduleProducts
            .FirstOrDefaultAsync(p => p.Id == productionScheduleProductId);

        if (productionScheduleProduct is null) return ProductErrors.NotFound(productionScheduleProductId);

        var product = await context.Products
            .IgnoreQueryFilters()
            .AsSplitQuery()
            .FirstOrDefaultAsync(p => p.Id == productionScheduleProduct.ProductId);

        if (product is null) return ProductErrors.NotFound(productionScheduleProduct.ProductId);

        var productionWarehouse = await context.Warehouses
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(w => w.DepartmentId == product.DepartmentId && w.Type == WarehouseType.Production);
        if (productionWarehouse is null) return Error.NotFound("Warehouse.Production", "Warehouse production record not found");

        var rawStorageWarehouse = await context.Warehouses
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(w => w.DepartmentId == product.DepartmentId && w.Type == WarehouseType.RawMaterialStorage);
        if (rawStorageWarehouse is null) return Error.NotFound("Warehouse.RawStorage", "Warehouse raw storage record not found");

        var packedStorageWarehouse = await context.Warehouses
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(w => w.DepartmentId == product.DepartmentId && w.Type == WarehouseType.PackagedStorage);
        if (packedStorageWarehouse is null) return Error.NotFound("Warehouse.PackedStorage", "Warehouse package storage record not found");

        var materialReturnNote = new MaterialReturnNote
        {
            ProductionScheduleProductId = productionScheduleProduct.Id,
            ReturnDate = DateTime.UtcNow,
            BatchNumber = productionScheduleProduct.BatchNumber ?? (await context.BatchManufacturingRecords
                .FirstOrDefaultAsync(b => b.ProductionScheduleProductId == productionScheduleProduct.Id))?.BatchNumber,
        };
        await context.MaterialReturnNotes.AddAsync(materialReturnNote);
        await context.SaveChangesAsync();
        var partialReturns = returns.Select(r => new MaterialReturnNotePartialReturn
        {
            MaterialId = r.MaterialId,

            UoMId = r.UoMId,
            Quantity = r.Quantity,
            MaterialBatchId = context.MaterialBatchReservedQuantities
                .IgnoreQueryFilters()
                .FirstOrDefault(mb => mb.MaterialBatch.MaterialId == r.MaterialId &&
                                      mb.WarehouseId == productionWarehouse.Id && mb.ProductionScheduleProductId == productionScheduleProductId)?.MaterialBatchId,
            DestinationWarehouseId =
                context.Materials.FirstOrDefault(m => m.Id == r.MaterialId)?.Kind == MaterialKind.Raw
                    ? rawStorageWarehouse.Id
                    : packedStorageWarehouse.Id,
            SourceWarehouseLocationShelfId = r.SourceWarehouseLocationShelfId
        }).ToList();
        foreach (var partial in partialReturns)
        {
            partial.MaterialReturnNoteId = materialReturnNote.Id;
        }
        await context.MaterialReturnNotePartialReturns.AddRangeAsync(partialReturns);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result<Paginateable<IEnumerable<MaterialReturnNoteDto>>>> GetMaterialReturnNotes(int page, int pageSize,
        string searchQuery)
    {
        var query = context.MaterialReturnNotes
            .AsSplitQuery()
            .Include(m => m.ProductionScheduleProduct)
            .ThenInclude(m => m.Product)
            .Include(m => m.ProductionScheduleProduct)
            .ThenInclude(m => m.ProductionSchedule)
            .Include(m => m.FullReturns)
            .Include(m => m.PartialReturns)
            .ThenInclude(m => m.Material)
            .AsQueryable();

        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.WhereSearch(searchQuery, q => q.BatchNumber);
        }

        return await PaginationHelper.GetPaginatedResultAsync(
            query,
            page,
            pageSize,
            mapper.Map<MaterialReturnNoteDto>
            );
    }

    public async Task<Result<MaterialReturnNoteDto>> GetMaterialReturnNoteById(Guid materialReturnNoteId)
    {
        var materialReturnNote = await context.MaterialReturnNotes
            .AsSplitQuery()
            .IgnoreQueryFilters()
            .Include(m => m.ProductionScheduleProduct)
            .ThenInclude(m => m.Product)
            .Include(m => m.ProductionScheduleProduct)
            .ThenInclude(m => m.ProductionSchedule)
            .Include(m => m.FullReturns)
                .ThenInclude(mf => mf.MaterialBatchReservedQuantity)
                    .ThenInclude(mf => mf.MaterialBatch)
                        .ThenInclude(m => m.Material)
            .Include(m => m.FullReturns)
                .ThenInclude(mf => mf.SourceWarehouseLocationShelf)
                    .ThenInclude(s => s.WarehouseLocationRack)
            .Include(m => m.FullReturns)
                .ThenInclude(mf => mf.MaterialBatchReservedQuantity)
                    .ThenInclude(mf => mf.UoM)
            .Include(m => m.FullReturns)
                .ThenInclude(mf => mf.MaterialBatchReservedQuantity)
                    .ThenInclude(mf => mf.Warehouse)
            .Include(m => m.FullReturns)
                .ThenInclude(mf => mf.DestinationWarehouse)
            .Include(m => m.PartialReturns)
                .ThenInclude(mp => mp.Material)
            .Include(m => m.PartialReturns)
                .ThenInclude(mp => mp.MaterialBatch)
            .Include(m => m.PartialReturns)
                .ThenInclude(mp => mp.DestinationWarehouse)
            .Include(m => m.PartialReturns)
                .ThenInclude(mp => mp.MaterialBatch)
            .Include(m => m.PartialReturns)
                .ThenInclude(mf => mf.SourceWarehouseLocationShelf)
                    .ThenInclude(s => s.WarehouseLocationRack)
            .FirstOrDefaultAsync(m => m.Id == materialReturnNoteId);

        return mapper.Map<MaterialReturnNoteDto>(materialReturnNote);
    }

    public async Task<Result> CompleteMaterialReturn(Guid materialReturnNoteId)
    {
        var materialReturnNote = await context.MaterialReturnNotes.FirstOrDefaultAsync(m => m.Id == materialReturnNoteId);

        if (materialReturnNote is null) return Error.NotFound("MaterialReturnNote", "MaterialReturnNote not found");

        materialReturnNote.Status = MaterialReturnStatus.Completed;
        context.MaterialReturnNotes.Update(materialReturnNote);
        await context.SaveChangesAsync();

        return Result.Success();
    }


    public async Task<Result> CreateExtraPacking(Guid productionScheduleProductId, List<CreateProductionExtraPacking> extraPackings)
    {
        foreach (var extraPacking in extraPackings)
        {
            await context.ProductionExtraPackings.AddAsync(new ProductionExtraPacking
            {
                ProductionScheduleProductId = productionScheduleProductId,
                MaterialId = extraPacking.MaterialId,
                Quantity = extraPacking.Quantity,
                UoMId = extraPacking.UoMId,
            });
        }

        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result<Paginateable<IEnumerable<ProductionExtraPackingWithBatchesDto>>>> GetProductionExtraPackings(int page,
        int pageSize, string searchQuery)
    {
        var query = context.ProductionExtraPackings
            .AsSplitQuery()
            .Where(q => q.Status == ProductionExtraPackingStatus.InProgress)
            .Include(p => p.Material)
            .Include(p => p.ProductionScheduleProduct)
            .ThenInclude(p => p.Product)
            .Include(p => p.ProductionScheduleProduct)
            .ThenInclude(p => p.ProductionSchedule)
            .Include(p => p.UoM)
            .Include(p => p.CreatedBy)
            .AsQueryable();

        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.WhereSearch(searchQuery, q => q.Material.Name);
        }

        var results = await PaginationHelper.GetPaginatedResultAsync(
            query,
            page,
            pageSize,
            mapper.Map<ProductionExtraPackingWithBatchesDto>
        );

        results.Data = results.Data.ToList();
        foreach (var result in results.Data)
        {
            var batchesResult = await BatchesToSupplyForExtraPackingMaterial(result.Id);
            result.Batches = batchesResult.IsSuccess ? batchesResult.Value : [];
        }

        return results;
    }

    public async Task<Result<ProductionExtraPackingWithBatchesDto>> GetProductionExtraPackingById(Guid productionExtraPackingId)
    {
        var productionExtraPacking = mapper.Map<ProductionExtraPackingWithBatchesDto>(await context.ProductionExtraPackings
            .AsSplitQuery()
            .Include(p => p.Material)
            .Include(p => p.ProductionScheduleProduct)
            .ThenInclude(p => p.Product)
            .Include(p => p.ProductionScheduleProduct)
            .ThenInclude(p => p.ProductionSchedule)
            .Include(p => p.UoM)
            .Include(p => p.IssuedBy)
            .Include(p => p.CreatedBy)
            .FirstOrDefaultAsync(p => p.Id == productionExtraPackingId));

        var batchesResult = await BatchesToSupplyForExtraPackingMaterial(productionExtraPacking.Id);
        productionExtraPacking.Batches = batchesResult.IsSuccess ? batchesResult.Value : [];
        return productionExtraPacking;
    }

    public async Task<Result<List<ProductionExtraPackingWithBatchesDto>>> GetProductionExtraPackingByProduct(Guid productionScheduleProductId)
    {
        var productionExtraPackings = mapper.Map<List<ProductionExtraPackingWithBatchesDto>>(await context.ProductionExtraPackings
            .AsSplitQuery()
            .Include(p => p.Material)
            .Include(p => p.ProductionScheduleProduct)
            .ThenInclude(p => p.Product)
            .Include(p => p.ProductionScheduleProduct)
            .ThenInclude(p => p.ProductionSchedule)
            .Include(p => p.UoM)
            .Include(p => p.IssuedBy)
            .Include(p => p.CreatedBy)
            .Where(p => p.ProductionScheduleProductId == productionScheduleProductId)
            .ToListAsync());

        foreach (var productionExtraPacking in productionExtraPackings)
        {
            var batchesResult = await BatchesToSupplyForExtraPackingMaterial(productionExtraPacking.Id);
            productionExtraPacking.Batches = batchesResult.IsSuccess ? batchesResult.Value : [];
        }

        return productionExtraPackings;
    }

    public async Task<Result<List<BatchToSupply>>> BatchesToSupplyForExtraPackingMaterial(Guid extraPackingMaterialId)
    {
        var extraPacking = await context.ProductionExtraPackings
            .AsSplitQuery()
            .Include(s => s.Material).
            Include(productionExtraPacking => productionExtraPacking.ProductionScheduleProduct)
            .ThenInclude(p => p.Product)
            .FirstOrDefaultAsync(st => st.Id == extraPackingMaterialId);

        if (extraPacking == null)
        {
            return Error.NotFound("ExtraPacking.NotFound", "Extra packing not found");
        }

        var department = extraPacking.ProductionScheduleProduct.Product.Department;

        var fromWarehouse = department.Warehouses.FirstOrDefault(q => q.Type == WarehouseType.PackagedStorage);
        if (fromWarehouse is null)
            return UserErrors.WarehouseNotFound(MaterialKind.Package);

        var toWarehouse = department.Warehouses.FirstOrDefault(w => w.Type == WarehouseType.Production);
        if (toWarehouse is null)
            return Error.NotFound("Production.Warehouse", "Production.Warehouse not found");

        return await materialRepository.BatchesToSupplyForGivenQuantity(extraPacking.MaterialId, fromWarehouse.Id, extraPacking.Quantity);
    }


    public async Task<Result> ApproveProductionExtraPacking(Guid productionExtraPackingId, List<BatchTransferRequest> batches, Guid userId)
    {
        var productionExtraPacking = await context.ProductionExtraPackings
            .AsSplitQuery()
            .Include(p => p.ProductionScheduleProduct)
            .ThenInclude(p => p.Product)
            .ThenInclude(pp => pp.Department)
            .ThenInclude(p => p.Warehouses).ThenInclude(warehouse => warehouse.ArrivalLocation)
            .FirstOrDefaultAsync(p => p.Id == productionExtraPackingId);

        if (productionExtraPacking is null)
            return Error.NotFound("ProductionExtraPacking", "ProductionExtraPacking not found");

        var department = productionExtraPacking.ProductionScheduleProduct.Product.Department;

        var fromWarehouse = department.Warehouses.FirstOrDefault(q => q.Type == WarehouseType.PackagedStorage);
        if (fromWarehouse is null)
            return UserErrors.WarehouseNotFound(MaterialKind.Package);

        var toWarehouse = department.Warehouses.FirstOrDefault(w => w.Type == WarehouseType.Production);
        if (toWarehouse is null)
            return Error.NotFound("Production.Warehouse", "Production.Warehouse not found");

        var remainingQuantity = productionExtraPacking.Quantity;
        var distributedBatches = new List<MaterialBatch>();

        foreach (var batchRequest in batches)
        {
            var batch = await context.MaterialBatches.FirstOrDefaultAsync(b => b.Id == batchRequest.BatchId);
            if (batch == null || batch.RemainingQuantity < batchRequest.Quantity)
                return Error.Failure("Batch.InsufficientStock", $"Not enough stock in batch {batchRequest.BatchId}");

            batch.QuantityAssigned = 0;
            context.MaterialBatches.Update(batch);

            // ✅ New logic replacing direct delete
            var shelfMaterialBatches = await context.ShelfMaterialBatches
                .IgnoreQueryFilters()
                .OrderBy(s => s.Quantity)
                .Where(sb => sb.MaterialBatchId == batch.Id
                             && sb.WarehouseLocationShelf.WarehouseLocationRack.WarehouseLocation.WarehouseId == fromWarehouse.Id
                             && !sb.DeletedAt.HasValue)
                .ToListAsync();

            var quantityToDeduct = batchRequest.Quantity;

            foreach (var shelfMaterialBatch in shelfMaterialBatches)
            {
                if (quantityToDeduct <= 0) break;

                var deductAmount = Math.Min(shelfMaterialBatch.Quantity, quantityToDeduct);
                shelfMaterialBatch.Quantity -= deductAmount;
                quantityToDeduct -= deductAmount;

                if (shelfMaterialBatch.Quantity <= 0)
                {
                    context.ShelfMaterialBatches.Remove(shelfMaterialBatch);
                }
            }

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

            var warehouseIds = new List<Guid> { fromWarehouse.Id, toWarehouse.Id };

            var history = await context.BinCardInformation
                .IgnoreQueryFilters()
                .Where(b => b.MaterialBatch.MaterialId == batch.MaterialId
                            && warehouseIds.Contains(b.WarehouseId.Value))
                .Select(b => new { b.WarehouseId, b.QuantityReceived, b.QuantityIssued })
                .ToListAsync();

            var fromBalance = history
                .Where(h => h.WarehouseId == fromWarehouse.Id)
                .Sum(x => x.QuantityReceived - x.QuantityIssued);

            var toBalance = history
                .Where(h => h.WarehouseId == toWarehouse.Id)
                .Sum(x => x.QuantityReceived - x.QuantityIssued);

            var balanceAfterIssue = fromBalance - batchRequest.Quantity;
            var balanceAfterReceive = toBalance + batchRequest.Quantity;

            var toBinCardEvent = new BinCardInformation
            {
                MaterialBatchId = batch.Id,
                Description = fromWarehouse.Name,
                WayBill = "N/A",
                ArNumber = "N/A",
                QuantityReceived = 0,
                QuantityIssued = batchRequest.Quantity,
                BalanceQuantity = balanceAfterIssue,
                UoMId = batch.UoMId,
                ProductId = productionExtraPacking.ProductionScheduleProduct.ProductId,
                CreatedAt = DateTime.UtcNow,
                CreatedById = userId,
                WarehouseId = fromWarehouse.Id,
            };
            await context.BinCardInformation.AddAsync(toBinCardEvent);

            var fromBinCardEvent = new BinCardInformation
            {
                MaterialBatchId = batch.Id,
                Description = toWarehouse.Name,
                WayBill = "N/A",
                ArNumber = "N/A",
                QuantityReceived = batchRequest.Quantity,
                QuantityIssued = 0,
                BalanceQuantity = balanceAfterReceive,
                UoMId = batch.UoMId,
                ProductId = productionExtraPacking.ProductionScheduleProduct.ProductId,
                CreatedAt = DateTime.UtcNow,
                CreatedById = userId,
                WarehouseId = toWarehouse.Id,
            };
            await context.BinCardInformation.AddAsync(fromBinCardEvent);

            distributedBatches.Add(batch);
            remainingQuantity -= batchRequest.Quantity;
            if (remainingQuantity <= 0) break;
        }
        await context.SaveChangesAsync();

        if (toWarehouse.ArrivalLocation == null)
        {
            toWarehouse.ArrivalLocation = new WarehouseArrivalLocation
            {
                WarehouseId = toWarehouse.Id,
                Name = "Default Arrival Location",
                FloorName = "Ground Floor",
                Description = "Automatically created arrival location"
            };
            await context.WarehouseArrivalLocations.AddAsync(toWarehouse.ArrivalLocation);
        }

        toWarehouse.ArrivalLocation.DistributedStockTransferBatches.AddRange(distributedBatches);

        if (remainingQuantity > 0)
            return Error.Failure("StockTransfer.InsufficientStock", "Not enough batches to fulfill the transfer");

        productionExtraPacking.IssuedAt = DateTime.UtcNow;
        productionExtraPacking.IssuedById = userId;
        productionExtraPacking.Status = ProductionExtraPackingStatus.Approved;
        context.ProductionExtraPackings.Update(productionExtraPacking);
        await context.SaveChangesAsync();

        return Result.Success();
    }


    public async Task<Result<IEnumerable<ProductionScheduleReportDto>>> GetProductionScheduleSummaryReport(ProductionScheduleReportFilter filter)
    {
        var query = context.ProductionScheduleProducts
            .AsSplitQuery()
            .Include(p => p.Product)
            .Include(p => p.ProductionSchedule)
            .Where(p => !p.Cancelled)
            .AsQueryable();

        if (filter.StartDate.HasValue)
        {
            query = query.Where(p => p.ProductionSchedule.ScheduledStartTime.Date >= filter.StartDate.Value.Date);
        }

        if (filter.EndDate.HasValue)
        {
            query = query.Where(p => p.ProductionSchedule.ScheduledEndTime.Date <= filter.EndDate.Value.Date);
        }

        if (filter.ProductId != Guid.Empty)
        {
            query = query.Where(p => p.ProductId == filter.ProductId);
        }

        if (filter.MarketTypeId.HasValue)
        {
            query = query.Where(p => p.MarketTypeId == filter.MarketTypeId);
        }

        var productionSchedules = await query.ToListAsync();

        var groupedData = productionSchedules
            .Select(p => new ProductionScheduleReportDto
            {
                Product = mapper.Map<ProductListDto>(p.Product),
                UnitPrice = p.Product.Price,
                BatchSize = productionSchedules.Sum(productScheduleProduct =>
                    productScheduleProduct.BatchSize == BatchSize.Full ?
                    productScheduleProduct.Product.FullBatchSize :
                    productScheduleProduct.Product.FullBatchSize / 2),
                Batches = productionSchedules.Sum(productScheduleProduct => productScheduleProduct.BatchSize == BatchSize.Full ? 1m : 0.5m),
                MarketType = mapper.Map<CollectionItemDto>(p.MarketType),
                ActualQuantity = context.FinalPackings.FirstOrDefault(f =>
                    f.ProductionScheduleProductId == p.Id)?.TotalNumberOfBottles ?? 0m,
            })
            .ToList();

        return groupedData;
    }

    public async Task<Result<IEnumerable<ProductionScheduleDetailedReportDto>>> GetProductionScheduleDetailedReport(ProductionScheduleReportFilter filter)
    {
        var query = context.ProductionScheduleProducts
            .AsSplitQuery()
            .Include(p => p.Product)
            .Include(p => p.ProductionSchedule)
            .Include(p => p.MarketType)
            .Where(p => !p.Cancelled)
            .AsQueryable();

        if (filter.StartDate.HasValue)
        {
            query = query.Where(p => p.ProductionSchedule.ScheduledStartTime.Date >= filter.StartDate.Value.Date);
        }

        if (filter.EndDate.HasValue)
        {
            query = query.Where(p => p.ProductionSchedule.ScheduledEndTime.Date <= filter.EndDate.Value.Date);
        }

        if (filter.ProductId != Guid.Empty)
        {
            query = query.Where(p => p.ProductId == filter.ProductId);
        }

        var productionSchedules = await query.ToListAsync();

        var report = productionSchedules.Select(p =>
        {
            var unitPrice = p.Product.Price;
            var expectedQty = p.BatchSize == BatchSize.Full ? p.Product.FullBatchSize : p.Product.FullBatchSize / 2;
            var actualQty = context.FinalPackings
                .FirstOrDefault(f => f.ProductionScheduleProductId == p.Id)
                ?.TotalNumberOfBottles ?? 0m;

            return new ProductionScheduleDetailedReportDto
            {
                BatchNumber = p.BatchNumber,
                UnitPrice = unitPrice,
                PackageStyle = p.Product.PackageStyle,
                ExpectedQuantity = expectedQty,
                ActualQuantity = actualQty,
            };
        }).ToList();

        return report;
    }

    public Task<Result> ForecastProductionScheduleProduct()
    {
        throw new NotImplementedException();
    }
}