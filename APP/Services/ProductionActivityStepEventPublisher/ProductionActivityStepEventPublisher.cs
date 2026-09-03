using DOMAIN.Entities.Base;
using DOMAIN.Entities.ProductionSchedules;
using INFRASTRUCTURE.Context;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace APP.Services.ProductionActivityStepEventPublisher;

public class ProductionActivityStepEventPublisher(
    ApplicationDbContext context,
    IPublishEndpoint publishEndpoint
) : IProductionActivityStepEventPublisher
{
    public async Task PublishStatusChanged(
        Guid productionActivityStepId,
        ProductionStatus status,
        Guid? updatedByUserId
    )
    {
        var ids = await context
            .ProductionActivitySteps.IgnoreQueryFilters()
            .Where(s => s.Id == productionActivityStepId)
            .Select(s => new
            {
                s.ProductionActivityId,
                ProductionScheduleProductId = s.ProductionActivity.ProductionScheduleProductId,
                ProductionScheduleId = s
                    .ProductionActivity
                    .ProductionScheduleProduct
                    .ProductionScheduleId,
            })
            .FirstOrDefaultAsync();

        if (ids is null)
            return;

        await publishEndpoint.Publish(
            new ProductionActivityStepStatusChangedEvent
            {
                ProductionActivityStepId = productionActivityStepId,
                ProductionActivityId = ids.ProductionActivityId,
                ProductionScheduleProductId = ids.ProductionScheduleProductId,
                ProductionScheduleId = ids.ProductionScheduleId,
                Status = status,
                UpdatedByUserId = updatedByUserId,
            }
        );
    }
}
