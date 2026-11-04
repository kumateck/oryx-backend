using APP.Repository;
using APP.Utils;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Approvals;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.Permissions;
using DOMAIN.Entities.ProductionSchedules;
using DOMAIN.Entities.Products;
using DOMAIN.Entities.Products.Production;
using DOMAIN.Entities.Requisitions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using Xunit;

namespace APP.Tests.Repository;

public sealed class ProductionScheduleRepairTests
{
    private static async Task<Guid> Prepare(IssuedMaterialRepairFixture fixture, IssuedMaterialRepairKind kind, decimal stock = 200_000_000)
    {
        await fixture.Seed(kind, stock, issueStock: false);
        await using var db = fixture.Open();
        await db.Database.ExecuteSqlRawAsync("DROP TABLE \"ProductionScheduleRepairs\"");
        var migration = new INFRASTRUCTURE.Migrations.AddScheduleProductRepairs();
        foreach (var command in db.GetService<IMigrationsSqlGenerator>().Generate(migration.UpOperations, db.Model))
            await db.Database.ExecuteSqlRawAsync(command.CommandText);
        var batch = await db.ProductionScheduleProducts.SingleAsync();
        var keys = new[] { PermissionKeys.CanViewProductSchedules, PermissionKeys.CanRepairBom, PermissionKeys.CanRepairPackaging, PermissionKeys.CanRepairProcedure };
        for (var index = 0; index < keys.Length; index++)
        {
            db.RoleClaims.Add(new IdentityRoleClaim<Guid> { Id = 800 + index, RoleId = fixture.RoleId, ClaimType = AppConstants.Permission, ClaimValue = keys[index] });
            db.PermissionTypes.Add(new PermissionType { Id = Guid.NewGuid(), Key = keys[index], RoleClaimId = 800 + index, Type = "Action" });
        }
        var style = new ProductPacking { ProductId = fixture.ProductId, Name = "Fixture style" };
        db.ProductPackings.Add(style); batch.ProductPackingId = style.Id;
        var second = new ProductionScheduleProduct { ProductId = fixture.ProductId, ProductionScheduleId = batch.ProductionScheduleId,
            ProductPackingId = style.Id, BatchSize = batch.BatchSize, Quantity = batch.Quantity, BatchNumber = "SECOND" };
        db.ProductionScheduleProducts.Add(second);
        var firstRun = await db.ProductionActivities.Include(run => run.Steps).SingleAsync();
        db.ProductionActivities.Add(new ProductionActivity { Code = "SECOND", ProductionScheduleProductId = second.Id, Status = ProductionStatus.InProgress,
            Steps = firstRun.Steps.Select(step => new ProductionActivityStep { Order = step.Order, OperationId = step.OperationId, Status = step.Status }).ToList() });
        foreach (var held in await db.MaterialBatchReservedQuantities.ToListAsync()) db.MaterialBatchReservedQuantities.Add(new MaterialBatchReservedQuantity
        { ProductionScheduleProductId = second.Id, MaterialBatchId = held.MaterialBatchId, WarehouseId = held.WarehouseId,
            WarehouseLocationShelfId = held.WarehouseLocationShelfId, UoMId = held.UoMId, Quantity = held.Quantity });
        db.ProductionScheduleProducts.Add(new ProductionScheduleProduct { ProductId = fixture.ProductId,
            ProductionScheduleId = batch.ProductionScheduleId, ProductPackingId = style.Id, Quantity = batch.Quantity, BatchNumber = "UNSTARTED" });
        await db.SaveChangesAsync();
        return batch.ProductionScheduleId;
    }

    private static async Task<ApplyScheduleRepair> Request(IssuedMaterialRepairFixture fixture, Guid schedule, ScheduleRepairKind kind)
    {
        await using var db = fixture.Open();
        var preview = await ProductionScheduleRepairs.Preview(db, null!, schedule, fixture.ProductId, fixture.Actor.UserId.GetValueOrDefault(), kind);
        Assert.True(preview.IsSuccess, preview.Error.Description);
        Assert.True(preview.Value.CanApply, string.Join(" ", preview.Value.Batches.Select(row => row.Message)));
        Assert.Equal(2, preview.Value.Batches.Count(row => row.Status == "ready"));
        Assert.Single(preview.Value.Batches.Where(row => row.Status == "skipped"));
        return new ApplyScheduleRepair { RequestId = Guid.NewGuid(), PreviewHash = preview.Value.PreviewHash, Reason = "Correct repeated batch configuration" };
    }

    [RepairPostgresFact]
    public Task Bom_repairs_every_running_batch_and_preserves_physical_shelf_stock() => MaterialRepair(IssuedMaterialRepairKind.Bom);

    [RepairPostgresFact]
    public Task Packaging_repairs_every_running_batch_and_preserves_physical_shelf_stock() => MaterialRepair(IssuedMaterialRepairKind.Packaging);

    private static async Task MaterialRepair(IssuedMaterialRepairKind kind)
    {
        await using var fixture = new IssuedMaterialRepairFixture(); var schedule = await Prepare(fixture, kind);
        await fixture.CorrectMaster();
        var repairKind = kind == IssuedMaterialRepairKind.Bom ? ScheduleRepairKind.Bom : ScheduleRepairKind.Packaging;
        var request = await Request(fixture, schedule, repairKind);
        await using (var db = fixture.Open())
        {
            var result = await ProductionScheduleRepairs.Apply(db, null!, null!, schedule, fixture.ProductId, fixture.Actor.UserId.GetValueOrDefault(), repairKind, request);
            Assert.True(result.IsSuccess, result.Error.Description); Assert.Equal(2, result.Value.Repaired); Assert.Equal(1, result.Value.Skipped);
        }
        await using (var db = fixture.Open())
        {
            Assert.Equal(235_000_000, await db.MaterialBatchReservedQuantities.SumAsync(item => item.Quantity));
            Assert.Equal(400_000_000, await db.ShelfMaterialBatches.SumAsync(item => item.Quantity));
            Assert.Equal(2, await db.ProductionMaterialRepairs.CountAsync());
            Assert.Equal(2, await db.ProductionMaterialConfigurations.CountAsync());
            Assert.Empty(await db.StockRequisitionIssues.ToListAsync()); Assert.Empty(await db.MassMaterialBatchMovements.ToListAsync());
            var repeat = await ProductionScheduleRepairs.Apply(db, null!, null!, schedule, fixture.ProductId, fixture.Actor.UserId.GetValueOrDefault(), repairKind, request);
            Assert.True(repeat.IsSuccess); Assert.True(repeat.Value.AlreadyApplied);
            Assert.Single(await db.ProductionScheduleRepairs.ToListAsync());
            var audit = await db.ProductionScheduleRepairs.SingleAsync(); Assert.Contains("Correct repeated", audit.Reason);
            audit.Reason = "overwrite"; await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());
            var refusal = await Assert.ThrowsAsync<PostgresException>(() => db.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"ProductionScheduleRepairs\""));
            Assert.Equal("23514", refusal.SqlState);
        }
    }

    [RepairPostgresFact]
    public async Task All_changed_unissued_requisitions_need_a_new_approval_cycle_with_old_decisions_preserved()
    {
        await using var fixture = new IssuedMaterialRepairFixture(); var schedule = await Prepare(fixture, IssuedMaterialRepairKind.Bom);
        await using (var db = fixture.Open())
        {
            var approval = new Approval { ItemType = "StockRequisition", ApprovalStages = [] };
            db.Approvals.Add(approval);
            db.ApprovalStages.Add(new() { ApprovalId = approval.Id, UserId = fixture.Actor.UserId, Required = true, Order = 1 });
            foreach (var run in await db.ProductionActivities.ToListAsync())
            {
                var reservations = await db.MaterialBatchReservedQuantities.Include(item => item.MaterialBatch)
                    .Where(item => item.ProductionScheduleProductId == run.ProductionScheduleProductId).ToListAsync();
                db.Requisitions.Add(new Requisition { Code = $"RM/{run.Id}", DepartmentId = fixture.Actor.DepartmentId.GetValueOrDefault(),
                    RequestedById = fixture.Actor.UserId.GetValueOrDefault(), ProductionScheduleProductId = run.ProductionScheduleProductId,
                    RequisitionType = RequisitionType.Stock, Approved = true, Status = RequestStatus.Pending,
                    Items = reservations.Select(item => new RequisitionItem { MaterialId = item.MaterialBatch.MaterialId,
                        UoMId = item.UoMId, Quantity = item.Quantity, Status = RequestStatus.Pending }).ToList(),
                    Approvals = [new() { ApprovalId = approval.Id, UserId = fixture.Actor.UserId, Required = true, Order = 1,
                        Status = ApprovalStatus.Approved, ApprovedById = fixture.Actor.UserId, ApprovalTime = DateTime.UtcNow }] });
            }
            await db.SaveChangesAsync();
        }
        await fixture.CorrectMaster(2); var request = await Request(fixture, schedule, ScheduleRepairKind.Bom);
        await using (var db = fixture.Open())
        {
            var approvals = new ApprovalRepository(db, null!, null!, null!, NullLogger<ApprovalRepository>.Instance, null!, new NoOpProductionActivityStepEventPublisher());
            var result = await ProductionScheduleRepairs.Apply(db, approvals, null!, schedule, fixture.ProductId, fixture.Actor.UserId.GetValueOrDefault(), ScheduleRepairKind.Bom, request);
            Assert.True(result.IsSuccess, result.Error.Description); Assert.Equal(2, result.Value.RequiringApproval);
        }
        await using (var db = fixture.Open())
        {
            Assert.All(await db.Requisitions.ToListAsync(), item => { Assert.False(item.Approved); Assert.Equal(1, item.ContentRevision); Assert.Equal(RequestStatus.New, item.Status); });
            Assert.Equal(2, await db.RequisitionApprovals.IgnoreQueryFilters().CountAsync(item => item.ContentRevision == 0 && item.Status == ApprovalStatus.Approved));
            Assert.Equal(2, await db.RequisitionApprovals.CountAsync(item => item.ContentRevision == 1 && item.Status == ApprovalStatus.Pending));
            Assert.Empty(await db.StockRequisitionIssues.ToListAsync());
        }
    }

    [RepairPostgresFact]
    public async Task A_later_batch_shortage_rolls_back_every_earlier_batch()
    {
        await using var fixture = new IssuedMaterialRepairFixture(); var schedule = await Prepare(fixture, IssuedMaterialRepairKind.Bom, 100_000_000);
        await fixture.CorrectMaster(); var request = await Request(fixture, schedule, ScheduleRepairKind.Bom);
        await using (var db = fixture.Open()) Assert.True((await ProductionScheduleRepairs.Apply(db, null!, null!, schedule, fixture.ProductId,
            fixture.Actor.UserId.GetValueOrDefault(), ScheduleRepairKind.Bom, request)).IsFailure);
        await using (var db = fixture.Open())
        {
            Assert.Equal(235m, await db.MaterialBatchReservedQuantities.SumAsync(item => item.Quantity));
            Assert.Empty(await db.ProductionMaterialRepairs.ToListAsync()); Assert.Empty(await db.ProductionMaterialConfigurations.ToListAsync());
            Assert.Empty(await db.ProductionScheduleRepairs.ToListAsync()); Assert.Equal(200_000_000, await db.ShelfMaterialBatches.SumAsync(item => item.Quantity));
        }
    }

    [RepairPostgresFact]
    public async Task Scope_membership_master_changes_and_current_permissions_are_rechecked()
    {
        await using var fixture = new IssuedMaterialRepairFixture(); var schedule = await Prepare(fixture, IssuedMaterialRepairKind.Bom);
        await fixture.CorrectMaster(); var request = await Request(fixture, schedule, ScheduleRepairKind.Bom);
        await fixture.CorrectMaster(1.01m);
        await using (var db = fixture.Open())
        {
            var stale = await ProductionScheduleRepairs.Apply(db, null!, null!, schedule, fixture.ProductId, fixture.Actor.UserId.GetValueOrDefault(), ScheduleRepairKind.Bom, request);
            Assert.True(stale.IsFailure); Assert.Equal("Repair.StalePreview", stale.Error.Code);
            Assert.True((await ProductionScheduleRepairs.Preview(db, null!, Guid.NewGuid(), fixture.ProductId, fixture.Actor.UserId.GetValueOrDefault(), ScheduleRepairKind.Bom)).IsFailure);
            var claim = await db.RoleClaims.SingleAsync(item => item.ClaimValue == PermissionKeys.CanRepairBom); db.RoleClaims.Remove(claim); await db.SaveChangesAsync();
        }
        await using (var db = fixture.Open())
        {
            var denied = await ProductionScheduleRepairs.Apply(db, null!, null!, schedule, fixture.ProductId, fixture.Actor.UserId.GetValueOrDefault(), ScheduleRepairKind.Bom, request);
            Assert.True(denied.IsFailure); Assert.Equal("Repair.Permission", denied.Error.Code);
            Assert.Empty(await db.ProductionScheduleRepairs.ToListAsync());
        }
    }

    [RepairPostgresFact]
    public async Task Procedure_repairs_all_runs_preserves_completed_steps_and_replays_once()
    {
        await using var fixture = new IssuedMaterialRepairFixture(); var schedule = await Prepare(fixture, IssuedMaterialRepairKind.Bom);
        Guid completed;
        await using (var db = fixture.Open())
        {
            var runs = await db.ProductionActivities.Include(run => run.Steps).OrderBy(run => run.Id).ToListAsync();
            var first = runs[0].Steps.OrderBy(step => step.Order).First();
            completed = first.Id; first.Status = ProductionStatus.Completed; first.CompletedAt = DateTime.UtcNow;
            foreach (var step in runs[0].Steps)
                db.Routes.Add(new DOMAIN.Entities.Routes.Route { ProductId = fixture.ProductId, OperationId = step.OperationId, Order = step.Order, IsCritical = true,
                    ResponsibleUsers = [new() { UserId = fixture.Actor.UserId.GetValueOrDefault(), Action = DOMAIN.Entities.Routes.OperationAction.BmrAndBprRequisitionOrIssue }] });
            db.Routes.Add(new DOMAIN.Entities.Routes.Route { ProductId = fixture.ProductId, Order = 3, Operation = new Operation { Name = "Corrected final step" },
                ResponsibleUsers = [new() { UserId = fixture.Actor.UserId.GetValueOrDefault(), Action = DOMAIN.Entities.Routes.OperationAction.BmrAndBprRequisitionOrIssue }] });
            await db.SaveChangesAsync();
        }
        var request = await Request(fixture, schedule, ScheduleRepairKind.Procedure);
        await using (var db = fixture.Open())
        {
            var result = await ProductionScheduleRepairs.Apply(db, null!, null!, schedule, fixture.ProductId, fixture.Actor.UserId.GetValueOrDefault(), ScheduleRepairKind.Procedure, request);
            Assert.True(result.IsSuccess, result.Error.Description); Assert.Equal(2, result.Value.Repaired);
        }
        await using (var db = fixture.Open())
        {
            Assert.Equal(6, await db.ProductionActivitySteps.CountAsync());
            var preserved = await db.ProductionActivitySteps.SingleAsync(step => step.Id == completed);
            Assert.Equal(ProductionStatus.Completed, preserved.Status); Assert.False(preserved.IsCritical);
            Assert.All(await db.ProductionActivitySteps.Where(step => step.Id != completed && step.Order < 3).ToListAsync(), step => Assert.True(step.IsCritical));
            var logs = await db.ProductionActivityLogs.CountAsync();
            var replay = await ProductionScheduleRepairs.Apply(db, null!, null!, schedule, fixture.ProductId, fixture.Actor.UserId.GetValueOrDefault(), ScheduleRepairKind.Procedure, request);
            Assert.True(replay.IsSuccess); Assert.True(replay.Value.AlreadyApplied);
            Assert.Equal(logs, await db.ProductionActivityLogs.CountAsync());
            var audit = await db.ProductionScheduleRepairs.SingleAsync(); Assert.NotEqual(audit.BeforeJson, audit.AfterJson);
        }
    }
}
