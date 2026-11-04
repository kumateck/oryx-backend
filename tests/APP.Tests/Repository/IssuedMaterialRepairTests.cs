using APP.Services.Production;
using APP.Utils;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.Products.Production;
using DOMAIN.Entities.Requisitions;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;

namespace APP.Tests.Repository;

public sealed class IssuedMaterialRepairTests
{
    [RepairPostgresFact]
    public async Task Reproduces_real_issue_then_repairs_packaging_once_and_reconciles_later_corrections()
    {
        await using var fixture = new IssuedMaterialRepairFixture();
        await fixture.Seed();
        await using (var db = fixture.Open())
        {
            Assert.Equal(117.5m, await db.MaterialBatchReservedQuantities.SumAsync(item => item.Quantity));
            Assert.Equal(400_000_000 - 117.5m, await db.ShelfMaterialBatches.SumAsync(item => item.Quantity));
        }
        await fixture.CorrectMaster();
        var request = await fixture.Request();
        await using (var db = fixture.Open())
        {
            var result = await fixture.Service(db).Apply(fixture.BatchId, fixture.Kind, request);
            Assert.True(result.IsSuccess, result.Error.Description);
            Assert.True(result.Value.Applied);
        }
        await using (var db = fixture.Open())
        {
            Assert.Equal(117_500_000, await db.MaterialBatchReservedQuantities.SumAsync(item => item.Quantity));
            Assert.Equal(282_500_000, await db.ShelfMaterialBatches.SumAsync(item => item.Quantity));
            Assert.Equal(282_500_000, await db.MaterialBatches.SumAsync(item => item.QuantityAssigned));
            Assert.Equal(0, await db.MaterialBatches.SumAsync(item => item.ConsumedQuantity));
            Assert.Equal(117.5m, await db.RequisitionItems.SumAsync(item => item.Quantity));
            Assert.Single(await db.IssuedMaterialRepairs.ToListAsync());
            var repeat = await fixture.Service(db).Apply(fixture.BatchId, fixture.Kind, request);
            Assert.True(repeat.IsSuccess); Assert.True(repeat.Value.AlreadyApplied);
            var dto = new RequisitionDto { Id = fixture.RequisitionId, Items = await db.RequisitionItems.Select(item => new RequisitionItemDto { Id = item.Id, Quantity = item.Quantity }).ToListAsync() };
            await IssuedMaterialRepairRead.Apply(db, dto);
            Assert.Equal(117_500_000, dto.Items.Sum(item => item.Quantity));
            Assert.Equal(117.5m, dto.Items.Sum(item => item.ApprovedQuantity));
        }
        // A second click with a new ID is also a no-op when the target has already been reached.
        await using (var db = fixture.Open())
        {
            request.RequestId = Guid.NewGuid();
            var repeat = await fixture.Service(db).Apply(fixture.BatchId, fixture.Kind, request);
            Assert.True(repeat.IsSuccess); Assert.True(repeat.Value.AlreadyApplied);
            Assert.Single(await db.IssuedMaterialRepairs.ToListAsync());
        }
        // Correct the source again: only the new difference is transferred, in either direction.
        await fixture.CorrectMaster(2);
        var second = await fixture.Request();
        await using (var db = fixture.Open()) Assert.True((await fixture.Service(db).Apply(fixture.BatchId, fixture.Kind, second)).IsSuccess);
        await fixture.CorrectMaster(0.5m);
        var decrease = await fixture.Request();
        await using (var db = fixture.Open()) Assert.True((await fixture.Service(db).Apply(fixture.BatchId, fixture.Kind, decrease)).IsSuccess);
        await using (var db = fixture.Open())
        {
            Assert.Equal(117_500_000, await db.MaterialBatchReservedQuantities.SumAsync(item => item.Quantity));
            Assert.Equal(282_500_000, await db.ShelfMaterialBatches.SumAsync(item => item.Quantity));
            Assert.Equal(117_500_000, await db.BinCardInformation.Where(item => item.RequisitionId == fixture.RequisitionId).SumAsync(item => item.QuantityIssued - item.QuantityReceived));
            Assert.Equal(3, await db.IssuedMaterialRepairs.CountAsync());
            Assert.All(await db.IssuedMaterialRepairAllocations.ToListAsync(), item => Assert.Equal(item.ShelfBefore - item.Adjustment, item.ShelfAfter));
            var repair = await db.IssuedMaterialRepairs.FirstAsync(); repair.Reason = "overwrite";
            await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());
        }
    }

    [RepairPostgresFact]
    public async Task Bom_uses_current_master_and_rechecks_permissions_department_and_preview()
    {
        await using var fixture = new IssuedMaterialRepairFixture(); await fixture.Seed(IssuedMaterialRepairKind.Bom);
        await fixture.CorrectMaster(); var request = await fixture.Request();
        await using (var db = fixture.Open())
        {
            var claim = await db.RoleClaims.SingleAsync(item => item.ClaimValue == PermissionKeys.CanRepairIssuedBom);
            db.RoleClaims.Remove(claim); await db.SaveChangesAsync();
        }
        await using (var db = fixture.Open())
        {
            var denied = await fixture.Service(db).Apply(fixture.BatchId, fixture.Kind, request);
            Assert.True(denied.IsFailure); Assert.Equal("Repair.Permission", denied.Error.Code);
            Assert.Empty(await db.IssuedMaterialRepairs.ToListAsync());
        }
        await using (var db = fixture.Open())
        {
            db.RoleClaims.Add(new Microsoft.AspNetCore.Identity.IdentityRoleClaim<Guid> { Id = 2, RoleId = fixture.RoleId, ClaimType = AppConstants.Permission, ClaimValue = PermissionKeys.CanRepairIssuedBom });
            await db.SaveChangesAsync();
        }
        await fixture.CorrectMaster(1.1m);
        await using (var db = fixture.Open())
        {
            var stale = await fixture.Service(db).Apply(fixture.BatchId, fixture.Kind, request);
            Assert.True(stale.IsFailure); Assert.Equal("Repair.StalePreview", stale.Error.Code);
        }
        request = await fixture.Request();
        await using (var db = fixture.Open()) Assert.True((await fixture.Service(db).Apply(fixture.BatchId, fixture.Kind, request)).IsSuccess);
        await using (var db = fixture.Open()) Assert.Equal(129_250_000, await db.MaterialBatchReservedQuantities.SumAsync(item => item.Quantity));
    }

    [RepairPostgresFact]
    public async Task Insufficient_or_expired_stock_blocks_every_line_without_partial_posting()
    {
        await using var fixture = new IssuedMaterialRepairFixture(); await fixture.Seed(stock: 50_000_000); await fixture.CorrectMaster();
        await using var db = fixture.Open();
        var preview = await fixture.Service(db).Preview(fixture.BatchId, fixture.Kind);
        Assert.True(preview.IsSuccess); Assert.False(preview.Value.CanApply);
        Assert.Contains(preview.Value.Blockers, message => message.Contains("insufficient"));
        var result = await fixture.Service(db).Apply(fixture.BatchId, fixture.Kind, new() { RequestId = Guid.NewGuid(), Reason = "Test shortage", PreviewToken = preview.Value.PreviewToken });
        Assert.True(result.IsFailure); Assert.Empty(await db.IssuedMaterialRepairs.ToListAsync());
        Assert.Equal(117.5m, await db.MaterialBatchReservedQuantities.SumAsync(item => item.Quantity));
        Assert.Equal(100_000_000 - 117.5m, await db.ShelfMaterialBatches.SumAsync(item => item.Quantity));
        var lot = await db.MaterialBatches.FirstAsync(); lot.ExpiryDate = DateTime.UtcNow.AddDays(-1); await db.SaveChangesAsync();
        var expired = await fixture.Service(db).Preview(fixture.BatchId, fixture.Kind);
        Assert.False(expired.Value.CanApply); Assert.Contains(expired.Value.Rows, row => row.AvailableQuantity == 0);
    }

    [RepairPostgresFact]
    public async Task Consumed_or_ambiguous_reservations_and_changed_membership_are_refused()
    {
        await using var fixture = new IssuedMaterialRepairFixture(); await fixture.Seed(); await fixture.CorrectMaster();
        await using (var db = fixture.Open())
        {
            var reservation = await db.MaterialBatchReservedQuantities.FirstAsync(); db.MaterialBatchReservedQuantities.Remove(reservation); await db.SaveChangesAsync();
        }
        await using (var db = fixture.Open())
        {
            var preview = await fixture.Service(db).Preview(fixture.BatchId, fixture.Kind);
            Assert.False(preview.Value.CanApply); Assert.Contains(preview.Value.Blockers, message => message.Contains("consumed"));
            Assert.Empty(await db.IssuedMaterialRepairs.ToListAsync());
        }
    }

    [RepairPostgresFact]
    public async Task Department_scope_reused_request_ids_and_source_membership_fail_closed()
    {
        await using var fixture = new IssuedMaterialRepairFixture(); await fixture.Seed(); await fixture.CorrectMaster();
        var request = await fixture.Request();
        await using (var db = fixture.Open()) Assert.True((await fixture.Service(db).Apply(fixture.BatchId, fixture.Kind, request)).IsSuccess);
        await using (var db = fixture.Open())
        {
            request.Reason = "Different request using the same ID";
            var reused = await fixture.Service(db).Apply(fixture.BatchId, fixture.Kind, request);
            Assert.True(reused.IsFailure); Assert.Equal("Repair.RequestReused", reused.Error.Code);
            var original = await db.IssuedMaterialRepairs.SingleAsync();
            Assert.NotEqual(request.Reason, original.Reason);
            var refused = await Assert.ThrowsAsync<PostgresException>(() => db.Database.ExecuteSqlRawAsync("DELETE FROM \"IssuedMaterialRepairs\""));
            Assert.Contains("append-only", refused.MessageText);
            var product = await db.Products.SingleAsync(item => item.Id == fixture.ProductId);
            db.Departments.Add(new() { Id = Guid.NewGuid(), Name = "Other synthetic production department", Type = SHARED.DepartmentType.Production });
            await db.SaveChangesAsync();
            product.DepartmentId = await db.Departments.Where(item => item.Id != fixture.Actor.DepartmentId).Select(item => item.Id).FirstAsync();
            await db.SaveChangesAsync();
        }
        await using (var db = fixture.Open())
        {
            var denied = await fixture.Service(db).Preview(fixture.BatchId, fixture.Kind);
            Assert.True(denied.IsFailure); Assert.Equal("Repair.Batch", denied.Error.Code);
        }
    }

    [RepairPostgresFact]
    public async Task Stock_changed_after_preview_or_reserved_for_another_batch_cannot_be_double_allocated()
    {
        await using var fixture = new IssuedMaterialRepairFixture(); await fixture.Seed(); await fixture.CorrectMaster(); var request = await fixture.Request();
        await using (var db = fixture.Open())
        {
            var original = await db.MaterialBatchReservedQuantities.FirstAsync();
            var batch = await db.ProductionScheduleProducts.SingleAsync(item => item.Id == fixture.BatchId);
            var other = new DOMAIN.Entities.ProductionSchedules.ProductionScheduleProduct { Id = Guid.NewGuid(), ProductId = fixture.ProductId,
                ProductionScheduleId = batch.ProductionScheduleId, BatchNumber = "OTHER-SYNTHETIC" };
            db.ProductionScheduleProducts.Add(other);
            db.MaterialBatchReservedQuantities.Add(new() { Id = Guid.NewGuid(), MaterialBatchId = original.MaterialBatchId,
                WarehouseId = original.WarehouseId, ProductionScheduleProductId = other.Id, UoMId = original.UoMId, Quantity = 195_000_000 });
            await db.SaveChangesAsync();
        }
        await using (var db = fixture.Open())
        {
            var stale = await fixture.Service(db).Apply(fixture.BatchId, fixture.Kind, request);
            Assert.True(stale.IsFailure); Assert.Equal("Repair.StalePreview", stale.Error.Code);
            var preview = await fixture.Service(db).Preview(fixture.BatchId, fixture.Kind);
            Assert.False(preview.Value.CanApply); Assert.Contains(preview.Value.Blockers, message => message.Contains("insufficient"));
            Assert.Empty(await db.IssuedMaterialRepairs.ToListAsync());
            Assert.Equal(400_000_000 - 117.5m, await db.ShelfMaterialBatches.SumAsync(item => item.Quantity));
        }
    }

    [RepairPostgresFact]
    public async Task Returns_can_restore_an_exhausted_source_shelf_without_restoring_deleted_history()
    {
        await using var fixture = new IssuedMaterialRepairFixture(); await fixture.Seed();
        await fixture.CorrectMaster(0.5m);
        await using (var db = fixture.Open())
        {
            foreach (var shelf in await db.ShelfMaterialBatches.ToListAsync())
            {
                // Move the remaining warehouse stock to another verified shelf. The original
                // source allocation can then be exhausted/deleted while issued stock is still held.
                var physical = await db.WarehouseLocationShelves.SingleAsync(item => item.Id == shelf.WarehouseLocationShelfId);
                var other = new DOMAIN.Entities.Warehouses.WarehouseLocationShelf { Id = Guid.NewGuid(), Name = "Shelf B", WarehouseLocationRackId = physical.WarehouseLocationRackId };
                db.WarehouseLocationShelves.Add(other);
                db.ShelfMaterialBatches.Add(new() { Id = Guid.NewGuid(), MaterialBatchId = shelf.MaterialBatchId, WarehouseLocationShelfId = other.Id, UoMId = shelf.UoMId, Quantity = shelf.Quantity });
                shelf.Quantity = 0;
                db.ShelfMaterialBatches.Remove(shelf);
            }
            await db.SaveChangesAsync();
        }
        var request = await fixture.Request();
        await using (var db = fixture.Open()) Assert.True((await fixture.Service(db).Apply(fixture.BatchId, fixture.Kind, request)).IsSuccess);
        await using (var db = fixture.Open())
        {
            Assert.Equal(58.75m, await db.MaterialBatchReservedQuantities.SumAsync(item => item.Quantity));
            Assert.Equal(400_000_000 - 58.75m, await db.ShelfMaterialBatches.SumAsync(item => item.Quantity));
            Assert.Equal(2, await db.ShelfMaterialBatches.IgnoreQueryFilters().CountAsync(item => item.DeletedAt.HasValue));
            Assert.Equal(4, await db.ShelfMaterialBatches.CountAsync());
        }
    }

    [RepairPostgresFact]
    public async Task Concurrent_repairs_commit_one_stock_adjustment_and_database_failure_rolls_back_all_effects()
    {
        await using var fixture = new IssuedMaterialRepairFixture(); await fixture.Seed(); await fixture.CorrectMaster(); var request = await fixture.Request();
        await using (var db = fixture.Open())
            await db.Database.ExecuteSqlRawAsync("ALTER TABLE \"IssuedMaterialRepairAllocations\" ADD CONSTRAINT reject_repair_test CHECK (false) NOT VALID");
        await using (var db = fixture.Open())
        {
            await Assert.ThrowsAsync<DbUpdateException>(() => fixture.Service(db).Apply(fixture.BatchId, fixture.Kind, request));
            Assert.Empty(db.ChangeTracker.Entries());
            await db.SaveChangesAsync(); // A later scoped audit save cannot flush the failed plan.
        }
        await using (var db = fixture.Open())
        {
            Assert.Empty(await db.IssuedMaterialRepairs.ToListAsync());
            Assert.Equal(117.5m, await db.MaterialBatchReservedQuantities.SumAsync(item => item.Quantity));
            Assert.Equal(2, await db.MassMaterialBatchMovements.CountAsync());
            await db.Database.ExecuteSqlRawAsync("ALTER TABLE \"IssuedMaterialRepairAllocations\" DROP CONSTRAINT reject_repair_test");
        }
        async Task<bool> Run()
        {
            await using var db = fixture.Open();
            try { var result = await fixture.Service(db).Apply(fixture.BatchId, fixture.Kind, request); return result.IsSuccess && result.Value.Applied; }
            catch (PostgresException error) when (error.SqlState is "40001" or "40P01") { return false; }
            catch (DbUpdateException error) when (error.InnerException is PostgresException { SqlState: "40001" or "23505" }) { return false; }
        }
        Assert.Single(await Task.WhenAll(Run(), Run()), applied => applied);
        await using (var db = fixture.Open())
        {
            Assert.Single(await db.IssuedMaterialRepairs.ToListAsync());
            Assert.Equal(117_500_000, await db.MaterialBatchReservedQuantities.SumAsync(item => item.Quantity));
            Assert.Equal(282_500_000, await db.ShelfMaterialBatches.SumAsync(item => item.Quantity));
        }
    }

    [RepairPostgresFact]
    public async Task Consumption_loaded_before_repair_must_reload_then_consumes_corrected_reservations_once()
    {
        await using var fixture = new IssuedMaterialRepairFixture(); await fixture.Seed(IssuedMaterialRepairKind.Bom); await fixture.CorrectMaster();
        var request = await fixture.Request();
        await using var stale = fixture.Open();
        var step = await stale.ProductionActivitySteps.Include(item => item.ProductionActivity)
            .SingleAsync(item => item.Operation.Name == "Product Preparation");
        await using (var db = fixture.Open()) Assert.True((await fixture.Service(db).Apply(fixture.BatchId, fixture.Kind, request)).IsSuccess);
        var mapper = new AutoMapper.MapperConfiguration(config => config.CreateMap<MaterialBatchReservedQuantity, DOMAIN.Entities.Materials.Batch.MaterialBatchReservedQuantityDto>()
            .ConvertUsing(source => new DOMAIN.Entities.Materials.Batch.MaterialBatchReservedQuantityDto { Id = source.Id, Quantity = source.Quantity,
                MaterialBatch = new DOMAIN.Entities.Materials.Batch.MaterialBatchListDto { Id = source.MaterialBatchId } })).CreateMapper();
        APP.Repository.ProductionScheduleRepository Repository(INFRASTRUCTURE.Context.ApplicationDbContext db) =>
            new(db, mapper, null!, new APP.Repository.MaterialRepository(db, mapper), null!, null!, new NoOpProductionActivityStepEventPublisher());
        var refused = await Repository(stale).UpdateStatusOfProductionActivityStep(step.Id, DOMAIN.Entities.Base.ProductionStatus.InProgress, fixture.Actor.UserId.GetValueOrDefault());
        Assert.True(refused.IsFailure); Assert.Equal("ProductActivity.StockChanged", refused.Error.Code);
        Assert.Empty(stale.ChangeTracker.Entries());
        await using (var db = fixture.Open())
        {
            Assert.Equal(0, await db.MaterialBatches.SumAsync(item => item.ConsumedQuantity));
            Assert.Equal(117_500_000, await db.MaterialBatchReservedQuantities.SumAsync(item => item.Quantity));
            var started = await Repository(db).UpdateStatusOfProductionActivityStep(step.Id, DOMAIN.Entities.Base.ProductionStatus.InProgress, fixture.Actor.UserId.GetValueOrDefault());
            Assert.True(started.IsSuccess, started.Error.Description);
        }
        await using (var db = fixture.Open())
        {
            Assert.Equal(117_500_000, await db.MaterialBatches.SumAsync(item => item.ConsumedQuantity));
            Assert.Empty(await db.MaterialBatchReservedQuantities.ToListAsync());
            var repeated = await Repository(db).UpdateStatusOfProductionActivityStep(step.Id, DOMAIN.Entities.Base.ProductionStatus.InProgress, fixture.Actor.UserId.GetValueOrDefault());
            Assert.True(repeated.IsSuccess);
            Assert.Equal(117_500_000, await db.MaterialBatches.SumAsync(item => item.ConsumedQuantity));
        }
    }
}
