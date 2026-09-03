using DOMAIN.Entities.Base;

namespace APP.Services.ProductionActivityStepEventPublisher;

public interface IProductionActivityStepEventPublisher
{
    Task PublishStatusChanged(
        Guid productionActivityStepId,
        ProductionStatus status,
        Guid? updatedByUserId
    );
}
