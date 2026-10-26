using DOMAIN.Entities.Base;
using DOMAIN.Entities.Products;
using DOMAIN.Entities.Products.Production;
using DOMAIN.Entities.ProductionSchedules;
using DOMAIN.Entities.Users;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

internal sealed class ProductionActivityBoardRow
{
    public Guid Id { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime StartedAt { get; init; }
    public ProductionStatus Status { get; init; }
    public ProductionScheduleProductDto Batch { get; init; }
    public ProductionActivityStep CurrentStep { get; init; }
}

/// <summary>Read only the open runs and the current-step fields displayed on the board.</summary>
internal static class ProductionActivityBoardQuery
{
    internal static IQueryable<ProductionActivityBoardRow> ForDepartment(
        ApplicationDbContext context,
        Guid? departmentId
    ) => context.ProductionActivities
        .AsNoTracking()
        .IgnoreAutoIncludes()
        .AsSplitQuery()
        .Where(activity =>
            !activity.CompletedAt.HasValue
            && activity.Status != ProductionStatus.Completed
            && activity.Status != ProductionStatus.Cancelled
            && !activity.ProductionScheduleProduct.Cancelled
            && (!departmentId.HasValue
                || activity.ProductionScheduleProduct.Product.DepartmentId == departmentId)
            && activity.Steps.Any(step => !step.CompletedAt.HasValue))
        .OrderBy(activity => activity.StartedAt)
        .ThenBy(activity => activity.Id)
        .Select(activity => new ProductionActivityBoardRow
        {
            Id = activity.Id,
            CreatedAt = activity.CreatedAt,
            StartedAt = activity.StartedAt,
            Status = activity.Status,
            Batch = new ProductionScheduleProductDto
            {
                Id = activity.ProductionScheduleProductId,
                BatchNumber = activity.ProductionScheduleProduct.BatchNumber
                    ?? context.BatchManufacturingRecords
                        .Where(record => record.ProductionScheduleProductId == activity.ProductionScheduleProductId)
                        .OrderBy(record => record.CreatedAt)
                        .ThenBy(record => record.Id)
                        .Select(record => record.BatchNumber)
                        .FirstOrDefault(),
                Quantity = activity.ProductionScheduleProduct.Quantity,
                BatchSize = activity.ProductionScheduleProduct.BatchSize,
                HasStarted = true,
                ProductionActivityId = activity.Id,
                ProductionSchedule = new CollectionItemDto
                {
                    Id = activity.ProductionScheduleProduct.ProductionScheduleId,
                    Code = activity.ProductionScheduleProduct.ProductionSchedule.Code,
                },
                Product = new ProductListDto
                {
                    Id = activity.ProductionScheduleProduct.ProductId,
                    Name = activity.ProductionScheduleProduct.Product.Name,
                    Code = activity.ProductionScheduleProduct.Product.Code,
                    BaseUoM = activity.ProductionScheduleProduct.Product.BaseUoM == null
                        ? null
                        : new UnitOfMeasureDto
                        {
                            Id = activity.ProductionScheduleProduct.Product.BaseUoM.Id,
                            Name = activity.ProductionScheduleProduct.Product.BaseUoM.Name,
                            Symbol = activity.ProductionScheduleProduct.Product.BaseUoM.Symbol,
                        },
                },
            },
            CurrentStep = activity.Steps
                .Where(step => !step.CompletedAt.HasValue)
                .OrderBy(step => step.Order)
                .ThenBy(step => step.Id)
                .Take(1)
                .Select(step => new ProductionActivityStep
                {
                    Id = step.Id,
                    ProductionActivityId = activity.Id,
                    ProductionActivity = new ProductionActivity { Id = activity.Id },
                    Order = step.Order,
                    Status = step.Status,
                    IsCritical = step.IsCritical,
                    StartedAt = step.StartedAt,
                    CompletedAt = step.CompletedAt,
                    OperationId = step.OperationId,
                    Operation = step.Operation == null ? null : new Operation
                    {
                        Id = step.Operation.Id,
                        Name = step.Operation.Name,
                        Description = step.Operation.Description,
                        Order = step.Operation.Order,
                    },
                    ResponsibleUsers = step.ResponsibleUsers
                        .OrderBy(person => person.Id)
                        .Select(person => new ProductionActivityStepUser
                        {
                            Id = person.Id,
                            Action = person.Action,
                            UserId = person.UserId,
                            User = person.User == null ? null : new User
                            {
                                Id = person.User.Id,
                                FirstName = person.User.FirstName,
                                LastName = person.User.LastName,
                                Avatar = person.User.Avatar,
                            },
                        }).ToList(),
                }).FirstOrDefault(),
        });
}
