using APP.Services.ProductionActivityStepEventPublisher;
using DOMAIN.Entities.Base;

namespace APP.Tests.Repository;

internal class NoOpProductionActivityStepEventPublisher : IProductionActivityStepEventPublisher
{
    public Task PublishStatusChanged(
        Guid productionActivityStepId,
        ProductionStatus status,
        Guid? updatedByUserId
    ) => Task.CompletedTask;
}
