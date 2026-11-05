using APP.Services.Production;
using APP.Utils;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.Materials.StockMigrations;
using DOMAIN.Entities.Products.Production;
using DOMAIN.Entities.Requisitions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Xunit;

namespace APP.Tests.Repository;

public sealed class ReservationRepairTests
{
    private static ReservationRepairService Service(INFRASTRUCTURE.Context.ApplicationDbContext db, IssuedMaterialRepairFixture fixture) => new(db, fixture.Actor);

    private static async Task Setup(IssuedMaterialRepairFixture fixture, decimal stock = 1000, IssuedMaterialRepairKind kind = IssuedMaterialRepairKind.Packaging)
    {
        await fixture.Seed(kind, issueStock: false);
        await using var db = fixture.Open();
        // Exercise the deployment migration's FKs and database append-only protection.
        await db.Database.ExecuteSqlRawAsync("DROP TABLE \"ReservationRepairs\"");
        var migrationSchema = new INFRASTRUCTURE.Migrations.AddReservationRepairs();
        foreach (var command in db.GetService<IMigrationsSqlGenerator>().Generate(migrationSchema.UpOperations, db.Model))
            await db.Database.ExecuteSqlRawAsync(command.CommandText);
        db.RoleClaims.Add(new() { Id = 4, RoleId = fixture.RoleId, ClaimType = AppConstants.Permission, ClaimValue = PermissionKeys.CanRepairStockReservations });
        db.PermissionTypes.Add(new() { Id = Guid.NewGuid(), RoleClaimId = 4, Key = PermissionKeys.CanRepairStockReservations, Type = "Action" });
        var replacement = new StockMigration { Id = Guid.NewGuid(), DepartmentId = fixture.Actor.DepartmentId.GetValueOrDefault(),
            FileName = "Synthetic approved replacement.xlsx", Mode = StockMigrationMode.OpeningBalance,
            Status = StockMigrationStatus.Approved, ApprovedAt = DateTime.UtcNow.AddDays(-1) };
        db.StockMigrations.Add(replacement);
        var requisition = new Requisition { Id = fixture.RequisitionId, Code = "PM/SYNTHETIC/WAITING", Approved = true,
            DepartmentId = fixture.Actor.DepartmentId.GetValueOrDefault(), ProductionScheduleProductId = fixture.BatchId,
            RequestedById = fixture.Actor.UserId.GetValueOrDefault(), Status = RequestStatus.Pending, RequisitionType = RequisitionType.Stock, Items = [] };
        db.Requisitions.Add(requisition);
        var reservations = await db.MaterialBatchReservedQuantities.Include(item => item.MaterialBatch).ToListAsync();
        foreach (var reservation in reservations)
        {
            var old = reservation.MaterialBatch;
            old.Status = BatchStatus.Consumed;
            var shelf = await db.ShelfMaterialBatches.SingleAsync(item => item.MaterialBatchId == old.Id);
            var oldQuantity = shelf.Quantity;
            shelf.Quantity = 0;
            replacement.WriteOffs.Add(new() { Id = Guid.NewGuid(), ShelfMaterialBatchId = shelf.Id, MaterialBatchId = old.Id,
                PreviousQuantity = oldQuantity, PreviousBatchStatus = BatchStatus.Available });
            var lot = new MaterialBatch { Id = Guid.NewGuid(), MaterialId = old.MaterialId, BatchNumber = old.BatchNumber + "/REPLACED",
                UoMId = fixture.UnitId, Status = BatchStatus.Available, TotalQuantity = stock, QuantityAssigned = stock,
                ExpiryDate = DateTime.UtcNow.AddYears(1) };
            db.MaterialBatches.Add(lot);
            db.ShelfMaterialBatches.Add(new() { Id = Guid.NewGuid(), MaterialBatchId = lot.Id,
                WarehouseLocationShelfId = shelf.WarehouseLocationShelfId, UoMId = fixture.UnitId, Quantity = stock });
            replacement.Lines.Add(new() { Id = Guid.NewGuid(), ImportedBatchId = lot.Id, MaterialId = lot.MaterialId, Quantity = stock });
            requisition.Items.Add(new() { Id = Guid.NewGuid(), MaterialId = old.MaterialId, UoMId = fixture.UnitId,
                Quantity = reservation.Quantity, Status = RequestStatus.Pending });
            db.BinCardInformation.Add(new() { MaterialBatchId = old.Id, WarehouseId = fixture.SourceId, UoMId = fixture.UnitId,
                QuantityIssued = oldQuantity, Description = "Synthetic replacement write-off" });
            db.BinCardInformation.Add(new() { MaterialBatchId = lot.Id, WarehouseId = fixture.SourceId, UoMId = fixture.UnitId,
                QuantityReceived = stock, BalanceQuantity = stock, Description = "Synthetic replacement receipt" });
        }
        await db.SaveChangesAsync();
    }

    private static async Task<ReservationRepairRequest> Request(IssuedMaterialRepairFixture fixture)
    {
        await using var db = fixture.Open();
        var preview = await Service(db, fixture).Preview(fixture.BatchId);
        Assert.True(preview.IsSuccess, preview.Error.Description);
        Assert.True(preview.Value.CanApply, string.Join(" ", preview.Value.Blockers));
        Assert.Equal(2, preview.Value.Rows.Count);
        return new() { RequestId = Guid.NewGuid(), PreviewToken = preview.Value.PreviewToken, Reason = "Rebind reservations after approved opening-stock replacement" };
    }

    [Fact]
    public void Endpoint_requires_its_own_permission_and_planned_batch_access()
    {
        var policies = typeof(API.Controllers.ReservationRepairController).GetCustomAttributes(typeof(AuthorizeAttribute), true)
            .Cast<AuthorizeAttribute>().Select(item => item.Policy).ToList();
        Assert.Contains(PermissionKeys.CanRepairStockReservations, policies);
        Assert.Contains(PermissionKeys.CanViewPlannedProducts, policies);
        Assert.DoesNotContain(PermissionKeys.CanRepairIssuedBom, policies);
    }

    [RepairPostgresFact]
    public async Task Rebind_preserves_ids_quantities_approvals_stock_and_records_immutable_history()
    {
        await using var fixture = new IssuedMaterialRepairFixture(); await Setup(fixture);
        var request = await Request(fixture);
        Dictionary<Guid, decimal> before;
        await using (var db = fixture.Open()) before = await db.MaterialBatchReservedQuantities.ToDictionaryAsync(item => item.Id, item => item.Quantity);
        await using (var db = fixture.Open()) Assert.True((await Service(db, fixture).Apply(fixture.BatchId, request)).IsSuccess);
        await using (var db = fixture.Open())
        {
            var after = await db.MaterialBatchReservedQuantities.Include(item => item.MaterialBatch).ToListAsync();
            Assert.All(after, item => { Assert.Equal(before[item.Id], item.Quantity); Assert.EndsWith("/REPLACED", item.MaterialBatch.BatchNumber); });
            Assert.Equal(2000, await db.ShelfMaterialBatches.SumAsync(item => item.Quantity));
            Assert.True((await db.Requisitions.SingleAsync()).Approved);
            Assert.Equal(RequestStatus.Pending, (await db.Requisitions.SingleAsync()).Status);
            Assert.Empty(await db.StockRequisitionIssues.ToListAsync());
            Assert.Empty(await db.MassMaterialBatchMovements.ToListAsync());
            Assert.Empty(await db.MaterialBatchEvents.ToListAsync());
            var audit = await db.ReservationRepairs.SingleAsync();
            Assert.Equal(fixture.Actor.UserId, audit.ActorId); Assert.Equal(request.Reason, audit.Reason);
            Assert.NotEqual(audit.BeforeJson, audit.AfterJson); Assert.Contains("before_row", audit.BeforeJson);
            Assert.Single(await db.ProductionActivityLogs.ToListAsync());
            var preview = await Service(db, fixture).Preview(fixture.BatchId);
            Assert.False(preview.Value.HasChanges); Assert.Single(preview.Value.History);
            Assert.True((await Service(db, fixture).Apply(fixture.BatchId, request)).Value.AlreadyApplied);
            request.Reason = "Conflicting reuse of the same request ID";
            Assert.Equal("ReservationRepair.RequestReused", (await Service(db, fixture).Apply(fixture.BatchId, request)).Error.Code);
            await Assert.ThrowsAsync<Npgsql.PostgresException>(() => db.Database.ExecuteSqlRawAsync("DELETE FROM \"ReservationRepairs\""));
            await Assert.ThrowsAsync<Npgsql.PostgresException>(() => db.Database.ExecuteSqlRawAsync("TRUNCATE \"ReservationRepairs\""));
        }
    }

    [RepairPostgresFact]
    public async Task Dedicated_permission_is_required_live_even_with_existing_bom_repair_grants()
    {
        await using var fixture = new IssuedMaterialRepairFixture(); await Setup(fixture); var request = await Request(fixture);
        await using var db = fixture.Open();
        await db.Database.ExecuteSqlRawAsync("DELETE FROM \"PermissionTypes\" WHERE \"Key\"='CanRepairStockReservations'");
        Assert.Equal("ReservationRepair.Permission", (await Service(db, fixture).Preview(fixture.BatchId)).Error.Code);
        Assert.Equal("ReservationRepair.Permission", (await Service(db, fixture).Apply(fixture.BatchId, request)).Error.Code);
        Assert.Empty(await db.ReservationRepairs.ToListAsync());
    }

    [RepairPostgresFact]
    public async Task Wrong_department_and_disabled_actor_cannot_repair()
    {
        await using var fixture = new IssuedMaterialRepairFixture(); await Setup(fixture); var request = await Request(fixture);
        await using var db = fixture.Open();
        var product = await db.Products.SingleAsync(); product.DepartmentId = null; await db.SaveChangesAsync();
        Assert.Equal("ReservationRepair.Batch", (await Service(db, fixture).Apply(fixture.BatchId, request)).Error.Code);
        var user = await db.Users.SingleAsync(); user.IsDisabled = true; await db.SaveChangesAsync();
        Assert.Equal("ReservationRepair.Permission", (await Service(db, fixture).Preview(fixture.BatchId)).Error.Code);
    }

    [RepairPostgresFact]
    public async Task Changed_stock_and_new_waiting_work_invalidate_a_reviewed_preview()
    {
        await using var fixture = new IssuedMaterialRepairFixture(); await Setup(fixture); var request = await Request(fixture);
        await using var db = fixture.Open();
        await AddWaitingBatch(db, fixture);
        Assert.Equal("ReservationRepair.StalePreview", (await Service(db, fixture).Apply(fixture.BatchId, request)).Error.Code);
        Assert.Empty(await db.ReservationRepairs.ToListAsync());
    }

    private static async Task AddWaitingBatch(INFRASTRUCTURE.Context.ApplicationDbContext db, IssuedMaterialRepairFixture fixture)
    {
        var source = await db.ProductionScheduleProducts.IgnoreAutoIncludes().SingleAsync(item => item.Id == fixture.BatchId);
        var batchId = Guid.NewGuid();
        db.ProductionScheduleProducts.Add(new() { Id = batchId, ProductId = fixture.ProductId, ProductionScheduleId = source.ProductionScheduleId,
            BatchNumber = "OTHER-WAITING-BATCH", Quantity = source.Quantity });
        db.ProductionActivities.Add(new() { Id = Guid.NewGuid(), ProductionScheduleProductId = batchId, Status = ProductionStatus.InProgress });
        var requisition = new Requisition { Id = Guid.NewGuid(), Code = "PM/SYNTHETIC/OTHER", Approved = true,
            DepartmentId = fixture.Actor.DepartmentId.GetValueOrDefault(), ProductionScheduleProductId = batchId,
            RequestedById = fixture.Actor.UserId.GetValueOrDefault(), Status = RequestStatus.Pending, RequisitionType = RequisitionType.Stock, Items = [] };
        foreach (var reservation in await db.MaterialBatchReservedQuantities.Include(item => item.MaterialBatch).Where(item => item.ProductionScheduleProductId == fixture.BatchId).ToListAsync())
        {
            requisition.Items.Add(new() { Id = Guid.NewGuid(), MaterialId = reservation.MaterialBatch.MaterialId, UoMId = fixture.UnitId,
                Quantity = reservation.Quantity, Status = RequestStatus.Pending });
            db.MaterialBatchReservedQuantities.Add(new() { Id = Guid.NewGuid(), MaterialBatchId = reservation.MaterialBatchId,
                WarehouseId = reservation.WarehouseId, WarehouseLocationShelfId = reservation.WarehouseLocationShelfId,
                ProductionScheduleProductId = batchId, Quantity = reservation.Quantity, UoMId = fixture.UnitId });
        }
        db.Requisitions.Add(requisition); await db.SaveChangesAsync();
    }

    [RepairPostgresFact]
    public async Task Repair_is_batch_scoped_but_respects_every_waiting_batchs_capacity()
    {
        await using var fixture = new IssuedMaterialRepairFixture(); await Setup(fixture, stock: 100);
        await using var db = fixture.Open(); await AddWaitingBatch(db, fixture);
        var blocked = await Service(db, fixture).Preview(fixture.BatchId);
        Assert.False(blocked.Value.CanApply); Assert.Contains(blocked.Value.Blockers, item => item.Contains("insufficient uncommitted"));
        await db.Database.ExecuteSqlRawAsync("UPDATE \"MaterialBatches\" SET \"TotalQuantity\"=1000 WHERE \"Status\"=3; UPDATE \"ShelfMaterialBatches\" SET \"Quantity\"=1000 WHERE \"Quantity\">0; UPDATE \"BinCardInformation\" SET \"QuantityReceived\"=1000 WHERE \"QuantityReceived\"=100");
        var request = await Request(fixture);
        Assert.True((await Service(db, fixture).Apply(fixture.BatchId, request)).IsSuccess);
        Assert.Equal(2, await db.MaterialBatchReservedQuantities.Where(item => item.ProductionScheduleProductId != fixture.BatchId
            && item.MaterialBatch.Status == BatchStatus.Consumed).CountAsync());
    }

    [RepairPostgresFact]
    public async Task Issue_evidence_approval_quantity_consumption_and_ledger_problems_block_the_whole_batch()
    {
        await using var fixture = new IssuedMaterialRepairFixture(); await Setup(fixture);
        await using var db = fixture.Open();
        var requisition = await db.Requisitions.SingleAsync(); requisition.Approved = false; await db.SaveChangesAsync();
        Assert.False((await Service(db, fixture).Preview(fixture.BatchId)).Value.CanApply);
        requisition.Approved = true; await db.SaveChangesAsync();
        await db.Database.ExecuteSqlRawAsync("UPDATE \"RequisitionItems\" SET \"QuantityReceived\"=1");
        Assert.False((await Service(db, fixture).Preview(fixture.BatchId)).Value.CanApply);
        await db.Database.ExecuteSqlRawAsync("UPDATE \"RequisitionItems\" SET \"QuantityReceived\"=0; UPDATE \"ProductionActivitySteps\" SET \"StartedAt\"=now() WHERE \"Order\"=2");
        Assert.False((await Service(db, fixture).Preview(fixture.BatchId)).Value.CanApply);
        await db.Database.ExecuteSqlRawAsync("UPDATE \"ProductionActivitySteps\" SET \"StartedAt\"=NULL; UPDATE \"ShelfMaterialBatches\" SET \"Quantity\"=999 WHERE \"Quantity\">0");
        var preview = await Service(db, fixture).Preview(fixture.BatchId);
        Assert.Contains(preview.Value.Blockers, item => item.Contains("bin-card"));
    }

    [RepairPostgresFact]
    public async Task Frozen_expired_and_ambiguous_replacements_are_not_guessed()
    {
        await using var fixture = new IssuedMaterialRepairFixture(); await Setup(fixture);
        await using var db = fixture.Open();
        await db.Database.ExecuteSqlRawAsync("UPDATE \"MaterialBatches\" SET \"Status\"=6 WHERE \"Status\"=3");
        Assert.False((await Service(db, fixture).Preview(fixture.BatchId)).Value.CanApply);
        await db.Database.ExecuteSqlRawAsync("UPDATE \"MaterialBatches\" SET \"Status\"=3, \"ExpiryDate\"=now()-interval '1 day', \"RetestDate\"=now()-interval '1 day' WHERE \"Status\"=6");
        Assert.False((await Service(db, fixture).Preview(fixture.BatchId)).Value.CanApply);
        await db.Database.ExecuteSqlRawAsync("UPDATE \"MaterialBatches\" SET \"ExpiryDate\"=NULL, \"RetestDate\"=NULL WHERE \"Status\"=3");
        var stock = await db.ShelfMaterialBatches.IgnoreAutoIncludes().FirstAsync(item => item.Quantity > 0);
        var shelf = await db.WarehouseLocationShelves.SingleAsync();
        var extra = new DOMAIN.Entities.Warehouses.WarehouseLocationShelf { Id = Guid.NewGuid(), Name = "Ambiguous shelf", Code = "OTHER", WarehouseLocationRackId = shelf.WarehouseLocationRackId };
        db.WarehouseLocationShelves.Add(extra);
        db.ShelfMaterialBatches.Add(new() { Id = Guid.NewGuid(), MaterialBatchId = stock.MaterialBatchId, WarehouseLocationShelfId = extra.Id, UoMId = fixture.UnitId, Quantity = 1 });
        await db.SaveChangesAsync();
        Assert.Contains((await Service(db, fixture).Preview(fixture.BatchId)).Value.Blockers, item => item.Contains("found 2"));
    }

    [RepairPostgresFact]
    public async Task Deleted_earlier_replacement_evidence_is_retained_but_missing_lot_evidence_blocks()
    {
        await using var fixture = new IssuedMaterialRepairFixture(); await Setup(fixture);
        await using var db = fixture.Open();
        var earlier = new StockMigration { Id = Guid.NewGuid(), DepartmentId = fixture.Actor.DepartmentId.GetValueOrDefault(),
            FileName = "Earlier deleted replacement.xlsx", Mode = StockMigrationMode.OpeningBalance,
            Status = StockMigrationStatus.Approved, ApprovedAt = DateTime.UtcNow.AddDays(-2), DeletedAt = DateTime.UtcNow.AddDays(-1) };
        db.StockMigrations.Add(earlier);
        foreach (var off in await db.StockMigrationWriteOffs.ToListAsync()) off.StockMigrationId = earlier.Id;
        await db.SaveChangesAsync();
        Assert.True((await Service(db, fixture).Preview(fixture.BatchId)).Value.CanApply);
        await db.Database.ExecuteSqlRawAsync("UPDATE \"MaterialBatches\" SET \"DeletedAt\"=now() WHERE \"Status\"=7");
        var blocked = await Service(db, fixture).Preview(fixture.BatchId);
        Assert.False(blocked.Value.CanApply);
        Assert.Contains(blocked.Value.Blockers, item => item.Contains("untraceable batch evidence"));
    }

    [RepairPostgresFact]
    public async Task Raw_retest_can_replace_expired_lots_but_expired_retest_never_overrides_future_expiry()
    {
        await using var fixture = new IssuedMaterialRepairFixture(); await Setup(fixture, kind: IssuedMaterialRepairKind.Bom);
        await using var db = fixture.Open();
        await db.Database.ExecuteSqlRawAsync("UPDATE \"MaterialBatches\" SET \"ExpiryDate\"=now()-interval '1 day', \"RetestDate\"=now()+interval '1 year' WHERE \"Status\"=3");
        Assert.True((await Service(db, fixture).Preview(fixture.BatchId)).Value.CanApply);
        await db.Database.ExecuteSqlRawAsync("UPDATE \"MaterialBatches\" SET \"ExpiryDate\"=now()+interval '1 year', \"RetestDate\"=now()-interval '1 day' WHERE \"Status\"=3");
        var request = await Request(fixture);
        Assert.True((await Service(db, fixture).Apply(fixture.BatchId, request)).IsSuccess);
    }

    [RepairPostgresFact]
    public async Task Packaging_replacement_uses_the_current_backend_retest_eligibility()
    {
        await using var fixture = new IssuedMaterialRepairFixture(); await Setup(fixture);
        await using var db = fixture.Open();
        await db.Database.ExecuteSqlRawAsync("UPDATE \"MaterialBatches\" SET \"ExpiryDate\"=now()-interval '1 day', \"RetestDate\"=now()+interval '1 year' WHERE \"Status\"=3");
        var request = await Request(fixture);
        Assert.True((await Service(db, fixture).Apply(fixture.BatchId, request)).IsSuccess);
    }

    [RepairPostgresFact]
    public async Task Existing_issue_receipts_and_changed_stock_refuse_the_reviewed_write()
    {
        await using var fixture = new IssuedMaterialRepairFixture(); await Setup(fixture); var request = await Request(fixture);
        await using var db = fixture.Open();
        await db.Database.ExecuteSqlRawAsync("UPDATE \"ShelfMaterialBatches\" SET \"Quantity\"=\"Quantity\"-1 WHERE \"Quantity\">0");
        Assert.Equal("ReservationRepair.StalePreview", (await Service(db, fixture).Apply(fixture.BatchId, request)).Error.Code);
        db.StockRequisitionIssues.Add(new() { Id = Guid.NewGuid(), RequisitionId = fixture.RequisitionId,
            IssuedById = fixture.Actor.UserId.GetValueOrDefault(), IssuedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();
        var preview = await Service(db, fixture).Preview(fixture.BatchId);
        Assert.False(preview.Value.CanApply);
        Assert.Contains(preview.Value.Blockers, item => item.Contains("issue evidence"));
        Assert.Empty(await db.ReservationRepairs.ToListAsync());
    }

    [RepairPostgresFact]
    public async Task Failed_audit_rolls_back_every_reservation_and_activity_log()
    {
        await using var fixture = new IssuedMaterialRepairFixture(); await Setup(fixture); var request = await Request(fixture);
        await using var db = fixture.Open();
        await db.Database.ExecuteSqlRawAsync("CREATE FUNCTION reject_synthetic_repair() RETURNS trigger LANGUAGE plpgsql AS $$ BEGIN RAISE EXCEPTION 'Synthetic failure'; END $$; CREATE TRIGGER synthetic_failure BEFORE INSERT ON \"ReservationRepairs\" FOR EACH ROW EXECUTE FUNCTION reject_synthetic_repair()");
        await Assert.ThrowsAsync<DbUpdateException>(() => Service(db, fixture).Apply(fixture.BatchId, request));
        await db.SaveChangesAsync();
        Assert.Equal(2, await db.MaterialBatchReservedQuantities.Where(item => item.MaterialBatch.Status == BatchStatus.Consumed).CountAsync());
        Assert.Empty(await db.ReservationRepairs.ToListAsync()); Assert.Empty(await db.ProductionActivityLogs.ToListAsync());
    }

    [RepairPostgresFact]
    public async Task Concurrent_same_request_returns_one_receipt_and_never_duplicates_the_write()
    {
        await using var fixture = new IssuedMaterialRepairFixture(); await Setup(fixture); var request = await Request(fixture);
        await using var first = fixture.Open(); await using var second = fixture.Open();
        var results = await Task.WhenAll(Service(first, fixture).Apply(fixture.BatchId, request), Service(second, fixture).Apply(fixture.BatchId, request));
        Assert.All(results, result => Assert.True(result.IsSuccess, result.Error.Description));
        Assert.Single(results, result => result.Value.AlreadyApplied);
        await using var db = fixture.Open(); Assert.Single(await db.ReservationRepairs.ToListAsync()); Assert.Single(await db.ProductionActivityLogs.ToListAsync());
    }
}
