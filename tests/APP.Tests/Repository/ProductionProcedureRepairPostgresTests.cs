using APP.Repository;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.ProductionSchedules;
using DOMAIN.Entities.Products;
using DOMAIN.Entities.Products.Production;
using DOMAIN.Entities.Routes;
using DOMAIN.Entities.Users;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Repository;

/// <summary>
/// Repairs a batch's procedure against a disposable local PostgreSQL database, because the repair runs in
/// a transaction with bulk deletes that the in-memory provider cannot run.
/// </summary>
public class ProductionProcedureRepairPostgresTests
{
    private const string Variable = "ORYX_PROCEDURE_REPAIR_TEST_CONNECTION";

    private sealed class CurrentUser : ICurrentUserService
    { public Guid? UserId => null; public Guid? DepartmentId => null; public string DepartmentType => string.Empty; }

    private static ApplicationDbContext Context(string connection) => new(
        new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(connection).Options, new CurrentUser());

    private static string Connection()
    {
        var connection = Environment.GetEnvironmentVariable(Variable) ?? throw new InvalidOperationException("Missing disposable fixture connection.");
        var parsed = new NpgsqlConnectionStringBuilder(connection);
        if (parsed.Host is not ("127.0.0.1" or "localhost" or "/tmp") || parsed.Database != "oryx_procedure_repair_test")
            throw new InvalidOperationException("Only the dedicated local disposable procedure repair fixture is permitted.");
        return connection;
    }

    [LocalProcedureRepairFact]
    public async Task Repairs_the_active_and_upcoming_steps_and_never_a_completed_one()
    {
        var connection = Connection();
        await using (var reset = Context(connection))
        {
            await reset.Database.EnsureDeletedAsync();
            await reset.Database.EnsureCreatedAsync();
        }

        var product = Guid.NewGuid();
        var (bmr, qcTest, granulation, packing, retired) = (Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var (qa, qc, production, oldQc) = (Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var ard = Guid.NewGuid();
        var activityId = Guid.NewGuid();
        var (bmrStep, testStep, retiredStep) = (Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        await using (var seed = Context(connection))
        {
            // The fixture is a slice of the graph; foreign keys are off only while it is written.
            await seed.Database.OpenConnectionAsync();
            await seed.Database.ExecuteSqlRawAsync("SET session_replication_role = replica;");
            await seed.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO "ProductAnalyticalRawData" ("Id", "AnalysisType", "Stage", "StpId", "FormId", "IsVerified", "CreatedAt")
                VALUES ({ard}, 0, 1, {Guid.NewGuid()}, {Guid.NewGuid()}, TRUE, now());
                """);
            seed.Users.AddRange(new[] { qa, qc, production, oldQc }.Select(id => new User { Id = id, UserName = $"u-{id:N}", FirstName = "Test", LastName = "User" }));
            seed.Operations.AddRange(new[] { bmr, qcTest, granulation, packing, retired }.Select(id => new Operation { Id = id, Name = $"Operation {id:N}" }));
            seed.Products.Add(new Product { Id = product, Name = "Blopen 100MG" });
            // The procedure as it is now: the QC test has its analytical raw data and a new last step.
            seed.Routes.AddRange(
                new Route { Id = Guid.NewGuid(), ProductId = product, OperationId = bmr, Order = 1, IsCritical = false,
                    ResponsibleUsers = [new RouteResponsibleUser { Id = Guid.NewGuid(), UserId = production, Action = OperationAction.BmrAndBprRequisitionOrIssue }] },
                new Route { Id = Guid.NewGuid(), ProductId = product, OperationId = qcTest, Order = 2, IsCritical = true,
                    ResponsibleUsers = [new RouteResponsibleUser { Id = Guid.NewGuid(), UserId = qc, Action = OperationAction.Atr, ProductAnalyticalRawDataId = ard }] },
                new Route { Id = Guid.NewGuid(), ProductId = product, OperationId = granulation, Order = 3,
                    ResponsibleUsers = [new RouteResponsibleUser { Id = Guid.NewGuid(), UserId = production, Action = OperationAction.StockRequisitionOrIssue }] },
                new Route { Id = Guid.NewGuid(), ProductId = product, OperationId = packing, Order = 4,
                    ResponsibleUsers = [new RouteResponsibleUser { Id = Guid.NewGuid(), UserId = production, Action = OperationAction.FinalPackingAndPartialReturn }] });
            var batch = new ProductionScheduleProduct { Id = Guid.NewGuid(), ProductionScheduleId = Guid.NewGuid(), ProductId = product, BatchNumber = "BT/0111/001" };
            seed.ProductionScheduleProducts.Add(batch);
            // The batch as it started: step 1 done by QA, step 2 running without analytical raw data, step 3 an old step.
            seed.ProductionActivities.Add(new ProductionActivity
            {
                Id = activityId, ProductionScheduleProductId = batch.Id, Code = "run", Status = ProductionStatus.InProgress, StartedAt = DateTime.UtcNow,
                Steps =
                [
                    new ProductionActivityStep { Id = bmrStep, OperationId = bmr, Order = 1, Status = ProductionStatus.Completed, IsCritical = true,
                        ResponsibleUsers = [new ProductionActivityStepUser { Id = Guid.NewGuid(), UserId = qa, Action = OperationAction.BmrAndBprRequisitionOrIssue }] },
                    new ProductionActivityStep { Id = testStep, OperationId = qcTest, Order = 2, Status = ProductionStatus.InProgress,
                        ResponsibleUsers =
                        [
                            new ProductionActivityStepUser { Id = Guid.NewGuid(), UserId = qc, Action = OperationAction.Atr },
                            new ProductionActivityStepUser { Id = Guid.NewGuid(), UserId = oldQc, Action = OperationAction.Atr },
                        ] },
                    new ProductionActivityStep { Id = retiredStep, OperationId = retired, Order = 3, Status = ProductionStatus.New },
                ],
            });
            await seed.SaveChangesAsync();
            await seed.Database.ExecuteSqlRawAsync("SET session_replication_role = DEFAULT;");
        }

        await using (var context = Context(connection))
        {
            var result = await ProductionProcedureRepair.RepairAsync(context, null!, activityId, production);
            Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Description : null);
            Assert.Equal(2, result.Value.StepsUpdated);
            Assert.Equal(1, result.Value.StepsAdded);
            Assert.Equal(0, result.Value.StepsRemoved);
        }

        await using (var check = Context(connection))
        {
            var steps = await check.ProductionActivitySteps.AsNoTracking().Include(step => step.ResponsibleUsers)
                .Where(step => step.ProductionActivityId == activityId).OrderBy(step => step.Order).ToListAsync();
            Assert.Equal([1, 2, 3, 4], steps.Select(step => step.Order));
            // The completed step is exactly as it was, though the procedure now names someone else.
            var done = steps[0];
            Assert.True(done.IsCritical);
            Assert.Equal(qa, Assert.Single(done.ResponsibleUsers).UserId);
            // The running step now has the procedure's analytical raw data, and the person no longer on it is removed.
            var test = steps[1];
            Assert.Equal(testStep, test.Id);
            Assert.True(test.IsCritical);
            var member = Assert.Single(test.ResponsibleUsers);
            Assert.Equal((qc, ard), (member.UserId, member.ProductAnalyticalRawDataId));
            Assert.Equal(ProductionStatus.InProgress, test.Status);
            // The step not started yet keeps its row and takes the procedure's operation; the new step is added.
            Assert.Equal((retiredStep, granulation), (steps[2].Id, steps[2].OperationId));
            Assert.Equal((packing, ProductionStatus.New), (steps[3].OperationId, steps[3].Status));
            Assert.Equal(production, Assert.Single(steps[3].ResponsibleUsers).UserId);
            Assert.Contains(await check.ProductionActivityLogs.AsNoTracking().Where(log => log.ProductionActivityId == activityId).Select(log => log.Message).ToListAsync(),
                message => message.StartsWith("Procedure repaired"));
        }
    }

    [LocalProcedureRepairFact]
    public async Task Removes_a_step_not_started_that_the_procedure_no_longer_has_but_keeps_a_started_one()
    {
        var connection = Connection();
        await using (var reset = Context(connection))
        {
            await reset.Database.EnsureDeletedAsync();
            await reset.Database.EnsureCreatedAsync();
        }
        var (product, operation, person, activityId) = (Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var (running, waiting) = (Guid.NewGuid(), Guid.NewGuid());
        await using (var seed = Context(connection))
        {
            await seed.Database.OpenConnectionAsync();
            await seed.Database.ExecuteSqlRawAsync("SET session_replication_role = replica;");
            seed.Users.Add(new User { Id = person, UserName = "u-person", FirstName = "Test", LastName = "User" });
            seed.Operations.Add(new Operation { Id = operation, Name = "Only step" });
            seed.Products.Add(new Product { Id = product, Name = "Blopen 100MG" });
            seed.Routes.Add(new Route { Id = Guid.NewGuid(), ProductId = product, OperationId = operation, Order = 1,
                ResponsibleUsers = [new RouteResponsibleUser { Id = Guid.NewGuid(), UserId = person, Action = OperationAction.Atr }] });
            var batch = new ProductionScheduleProduct { Id = Guid.NewGuid(), ProductionScheduleId = Guid.NewGuid(), ProductId = product };
            seed.ProductionScheduleProducts.Add(batch);
            seed.ProductionActivities.Add(new ProductionActivity { Id = activityId, ProductionScheduleProductId = batch.Id, Code = "run", Status = ProductionStatus.InProgress,
                Steps =
                [
                    new ProductionActivityStep { Id = Guid.NewGuid(), OperationId = operation, Order = 1, Status = ProductionStatus.InProgress },
                    new ProductionActivityStep { Id = running, OperationId = operation, Order = 2, Status = ProductionStatus.InProgress },
                    new ProductionActivityStep { Id = waiting, OperationId = operation, Order = 3, Status = ProductionStatus.New,
                        ResponsibleUsers = [new ProductionActivityStepUser { Id = Guid.NewGuid(), UserId = person, Action = OperationAction.Atr }] },
                ] });
            await seed.SaveChangesAsync();
            await seed.Database.ExecuteSqlRawAsync("SET session_replication_role = DEFAULT;");
        }

        await using (var context = Context(connection))
        {
            var result = await ProductionProcedureRepair.RepairAsync(context, null!, activityId, person);
            Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Description : null);
            Assert.Equal(1, result.Value.StepsRemoved);
            Assert.Single(result.Value.StepsKept);
        }
        await using var check = Context(connection);
        var orders = await check.ProductionActivitySteps.AsNoTracking().Where(step => step.ProductionActivityId == activityId).Select(step => step.Id).ToListAsync();
        Assert.Contains(running, orders);
        Assert.DoesNotContain(waiting, orders);
    }
}

public sealed class LocalProcedureRepairFactAttribute : FactAttribute
{
    public LocalProcedureRepairFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ORYX_PROCEDURE_REPAIR_TEST_CONNECTION")))
            Skip = "Set ORYX_PROCEDURE_REPAIR_TEST_CONNECTION to the dedicated disposable local PostgreSQL fixture.";
    }
}
