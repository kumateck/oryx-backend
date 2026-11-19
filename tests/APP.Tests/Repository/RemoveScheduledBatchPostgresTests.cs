using APP.Repository;
using APP.Services.Production;
using DOMAIN.Entities.ProductionSchedules;
using DOMAIN.Entities.Products.Production;
using DOMAIN.Entities.Requisitions;
using Microsoft.EntityFrameworkCore;
using Xunit;
namespace APP.Tests.Repository;

/// <summary>
/// Removing a batch from a schedule deletes its row. On the client server a batch not yet started but with a requisition
/// raised for it hit the requisition foreign key and returned 500 (FK_Requisitions_ProductionScheduleProducts…).
/// </summary>
public sealed class RemoveScheduledBatchPostgresTests
{
    private static async Task<(IssuedMaterialRepairFixture Fixture, Guid Batch)> UnstartedBatch()
    {
        var fixture = new IssuedMaterialRepairFixture();
        await fixture.Seed(IssuedMaterialRepairKind.Bom, issueStock: false);
        await using var db = fixture.Open();
        var started = await db.ProductionScheduleProducts.IgnoreQueryFilters().SingleAsync(item => item.Id == fixture.BatchId);
        var batch = new ProductionScheduleProduct { Id = Guid.NewGuid(), ProductionScheduleId = started.ProductionScheduleId, ProductId = started.ProductId, BatchSize = BatchSize.Full };
        db.ProductionScheduleProducts.Add(batch);
        await db.SaveChangesAsync();
        return (fixture, batch.Id);
    }

    private static ProductionScheduleRepository Repository(INFRASTRUCTURE.Context.ApplicationDbContext db) =>
        new(db, null!, null!, null!, null!, null!, new NoOpProductionActivityStepEventPublisher());

    [RepairPostgresFact]
    public async Task A_batch_with_a_requisition_is_refused_with_its_reason_not_a_server_error()
    {
        var (fixture, batch) = await UnstartedBatch();
        await using (fixture)
        {
            await using (var setup = fixture.Open())
            {
                setup.Requisitions.Add(new Requisition { Id = Guid.NewGuid(), Code = "PR-SHORTAGE-1", RequisitionType = RequisitionType.Purchase,
                    ProductionScheduleProductId = batch, DepartmentId = fixture.Actor.DepartmentId!.Value, RequestedById = fixture.Actor.UserId!.Value, Items = [] });
                await setup.SaveChangesAsync();
            }
            await using var db = fixture.Open();

            var result = await Repository(db).RemoveProductFromSchedule(batch);

            Assert.Equal("ProductionScheduleProduct.InUse", result.Error.Code);
            Assert.Contains("requisitions", result.Error.Description);
            Assert.True(await db.ProductionScheduleProducts.IgnoreQueryFilters().AnyAsync(item => item.Id == batch));
        }
    }

    [RepairPostgresFact]
    public async Task An_unstarted_batch_keeps_its_earlier_run_instead_of_having_it_deleted_with_it()
    {
        var (fixture, batch) = await UnstartedBatch();
        await using (fixture)
        {
            await using (var setup = fixture.Open())
            {
                // A run that was started and then unstarted is soft-deleted; the row still cascades on delete.
                setup.ProductionActivities.Add(new ProductionActivity { Id = Guid.NewGuid(), Code = "Unstarted run", ProductionScheduleProductId = batch, DeletedAt = DateTime.UtcNow, Steps = [] });
                await setup.SaveChangesAsync();
            }
            await using var db = fixture.Open();

            var result = await Repository(db).RemoveProductFromSchedule(batch);

            Assert.Equal("ProductionScheduleProduct.InUse", result.Error.Code);
            Assert.True(await db.ProductionActivities.IgnoreQueryFilters().AnyAsync(item => item.ProductionScheduleProductId == batch));
        }
    }

    [RepairPostgresFact]
    public async Task A_batch_nothing_refers_to_is_removed()
    {
        var (fixture, batch) = await UnstartedBatch();
        await using (fixture)
        {
            await using var db = fixture.Open();
            Assert.True((await Repository(db).RemoveProductFromSchedule(batch)).IsSuccess);
            Assert.False(await db.ProductionScheduleProducts.IgnoreQueryFilters().AnyAsync(item => item.Id == batch));
        }
    }
}
