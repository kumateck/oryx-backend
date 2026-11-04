using DOMAIN.Entities.Products.Production;
using SHARED;

namespace APP.Repository;

public partial class ProductionScheduleRepository
{
    public Task<Result<ScheduleRepairPreview>> PreviewScheduleRepair(Guid scheduleId, Guid productId, Guid actorId, ScheduleRepairKind kind) =>
        ProductionScheduleRepairs.Preview(context, userManager, scheduleId, productId, actorId, kind);

    public Task<Result<ScheduleRepairResult>> ApplyScheduleRepair(Guid scheduleId, Guid productId, Guid actorId,
        ScheduleRepairKind kind, ApplyScheduleRepair request) =>
        ProductionScheduleRepairs.Apply(context, approvalRepository, userManager, scheduleId, productId, actorId, kind, request);
}
