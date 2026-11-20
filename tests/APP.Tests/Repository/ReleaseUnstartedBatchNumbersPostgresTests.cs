using DOMAIN.Entities.Products.Production;
using Microsoft.EntityFrameworkCore;
using Xunit;
namespace APP.Tests.Repository;

/// <summary>Batches unstarted before the change kept their number (NL006 L006K005); the repair releases it.</summary>
public sealed class ReleaseUnstartedBatchNumbersPostgresTests
{
    [RepairPostgresFact]
    public async Task An_unstarted_batch_without_a_live_run_loses_its_number_and_a_running_one_keeps_it()
    {
        await using var fixture = new IssuedMaterialRepairFixture();
        await fixture.Seed(IssuedMaterialRepairKind.Bom, issueStock: false);
        Guid unstarted, running;
        await using (var db = fixture.Open())
        {
            var started = await db.ProductionScheduleProducts.IgnoreQueryFilters().SingleAsync(item => item.Id == fixture.BatchId);
            running = started.Id; started.BatchNumber = "L006K006";
            var batch = new DOMAIN.Entities.ProductionSchedules.ProductionScheduleProduct { Id = Guid.NewGuid(), ProductionScheduleId = started.ProductionScheduleId,
                ProductId = started.ProductId, BatchNumber = "L006K005" };
            unstarted = batch.Id;
            db.ProductionScheduleProducts.Add(batch);
            // The unstarted batch's run is archived; the fixture's batch was restarted and has a live run.
            var archived = new ProductionActivity { Id = Guid.NewGuid(), Code = "Unstarted run", ProductionScheduleProductId = unstarted, DeletedAt = DateTime.UtcNow.AddHours(-2), Steps = [] };
            db.ProductionActivities.Add(archived);
            var live = await db.ProductionActivities.IgnoreQueryFilters().FirstAsync(item => item.ProductionScheduleProductId == running);
            foreach (var (id, activity) in new[] { (unstarted, archived.Id), (running, live.Id) })
                db.ProductionUnstartRepairs.Add(new ProductionUnstartRepair { Id = Guid.NewGuid(), BatchId = id, ActivityId = activity, ActorId = fixture.Actor.UserId!.Value,
                    AppliedAt = DateTime.UtcNow.AddHours(-2), Reason = "Started at the wrong size" });
            await db.SaveChangesAsync();
        }
        await using (var db = fixture.Open())
        {
            var migration = new INFRASTRUCTURE.Migrations.ReleaseUnstartedBatchNumbers();
            var generator = Microsoft.EntityFrameworkCore.Infrastructure.AccessorExtensions.GetService<Microsoft.EntityFrameworkCore.Migrations.IMigrationsSqlGenerator>(db);
            foreach (var command in generator.Generate(migration.UpOperations, db.Model))
                await db.Database.ExecuteSqlRawAsync(command.CommandText);
        }
        await using (var db = fixture.Open())
        {
            Assert.Null((await db.ProductionScheduleProducts.IgnoreQueryFilters().SingleAsync(item => item.Id == unstarted)).BatchNumber);
            // The fixture's batch was restarted (it has a live run), so it keeps its number.
            Assert.Equal("L006K006", (await db.ProductionScheduleProducts.IgnoreQueryFilters().SingleAsync(item => item.Id == running)).BatchNumber);
        }
    }
}
