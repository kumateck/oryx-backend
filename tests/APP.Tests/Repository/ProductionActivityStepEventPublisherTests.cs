using APP.Services.ProductionActivityStepEventPublisher;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Products;
using DOMAIN.Entities.Products.Production;
using DOMAIN.Entities.ProductionSchedules;
using INFRASTRUCTURE.Context;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Repository;

file class FakeCurrentUserService : ICurrentUserService
{
    public Guid? UserId => null;
    public Guid? DepartmentId => null;
    public string DepartmentType => string.Empty;
}

file class CapturingPublishEndpoint : IPublishEndpoint
{
    public object? LastPublished { get; private set; }

    public Task Publish<T>(T message, CancellationToken cancellationToken = default)
        where T : class
    {
        LastPublished = message;
        return Task.CompletedTask;
    }

    public Task Publish<T>(
        T message,
        IPipe<PublishContext<T>> publishPipe,
        CancellationToken cancellationToken = default
    )
        where T : class => throw new NotImplementedException();

    public Task Publish<T>(
        T message,
        IPipe<PublishContext> publishPipe,
        CancellationToken cancellationToken = default
    )
        where T : class => throw new NotImplementedException();

    public Task Publish(object message, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    public Task Publish(
        object message,
        Type messageType,
        CancellationToken cancellationToken = default
    ) => throw new NotImplementedException();

    public Task Publish(
        object message,
        IPipe<PublishContext> publishPipe,
        CancellationToken cancellationToken = default
    ) => throw new NotImplementedException();

    public Task Publish(
        object message,
        Type messageType,
        IPipe<PublishContext> publishPipe,
        CancellationToken cancellationToken = default
    ) => throw new NotImplementedException();

    public Task Publish<T>(object values, CancellationToken cancellationToken = default)
        where T : class => throw new NotImplementedException();

    public Task Publish<T>(
        object values,
        IPipe<PublishContext<T>> publishPipe,
        CancellationToken cancellationToken = default
    )
        where T : class => throw new NotImplementedException();

    public Task Publish<T>(
        object values,
        IPipe<PublishContext> publishPipe,
        CancellationToken cancellationToken = default
    )
        where T : class => throw new NotImplementedException();

    public ConnectHandle ConnectPublishObserver(IPublishObserver observer) =>
        throw new NotImplementedException();
}

public class ProductionActivityStepEventPublisherTests
{
    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options, new FakeCurrentUserService());
    }

    [Fact]
    public async Task PublishStatusChanged_ResolvesIdsAndPublishesEvent()
    {
        await using var context = CreateContext();

        var productionSchedule = new ProductionSchedule { Id = Guid.NewGuid() };
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Test Product",
            Code = "P-TEST",
        };
        var productionScheduleProduct = new ProductionScheduleProduct
        {
            Id = Guid.NewGuid(),
            ProductionScheduleId = productionSchedule.Id,
            ProductId = product.Id,
        };
        var productionActivity = new ProductionActivity
        {
            Id = Guid.NewGuid(),
            ProductionScheduleProductId = productionScheduleProduct.Id,
        };
        var operation = new Operation { Id = Guid.NewGuid(), Name = "Weighing" };
        var activityStep = new ProductionActivityStep
        {
            Id = Guid.NewGuid(),
            ProductionActivityId = productionActivity.Id,
            OperationId = operation.Id,
            Status = ProductionStatus.Completed,
        };

        context.ProductionSchedules.Add(productionSchedule);
        context.Products.Add(product);
        context.ProductionScheduleProducts.Add(productionScheduleProduct);
        context.ProductionActivities.Add(productionActivity);
        context.Operations.Add(operation);
        context.ProductionActivitySteps.Add(activityStep);
        await context.SaveChangesAsync();

        var publishEndpoint = new CapturingPublishEndpoint();
        var publisher = new ProductionActivityStepEventPublisher(context, publishEndpoint);
        var userId = Guid.NewGuid();

        await publisher.PublishStatusChanged(activityStep.Id, ProductionStatus.Completed, userId);

        var published = Assert.IsType<ProductionActivityStepStatusChangedEvent>(
            publishEndpoint.LastPublished
        );
        Assert.Equal(activityStep.Id, published.ProductionActivityStepId);
        Assert.Equal(productionActivity.Id, published.ProductionActivityId);
        Assert.Equal(productionScheduleProduct.Id, published.ProductionScheduleProductId);
        Assert.Equal(productionSchedule.Id, published.ProductionScheduleId);
        Assert.Equal(ProductionStatus.Completed, published.Status);
        Assert.Equal(userId, published.UpdatedByUserId);
    }

    [Fact]
    public async Task PublishStatusChanged_UnknownStep_DoesNotPublish()
    {
        await using var context = CreateContext();
        var publishEndpoint = new CapturingPublishEndpoint();
        var publisher = new ProductionActivityStepEventPublisher(context, publishEndpoint);

        await publisher.PublishStatusChanged(Guid.NewGuid(), ProductionStatus.Completed, null);

        Assert.Null(publishEndpoint.LastPublished);
    }
}
