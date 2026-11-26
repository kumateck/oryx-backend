using APP.Repository;
using APP.Utils;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.BinCards;
using DOMAIN.Entities.Departments;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.ProductionSchedules.StockTransfers;
using DOMAIN.Entities.Warehouses;
using INFRASTRUCTURE.Context;
using INFRASTRUCTURE.Migrations;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SHARED;
using Xunit;

namespace APP.Tests.Repository;

/// <summary>
/// The Vanilla Powder case (RMV006, lot R-03506220): Tablet's store issued 5.43 kg per requisition while the 100 kg and
/// 30 kg transfers it received from Beta were never posted, so its saved balances ran to −51.54 kg.
/// </summary>
public sealed class BinCardLedgerTests
{
    private const decimal Kg = 1_000_000m; // the lot is kept in mg

    private static async Task<(ScheduleStockFixture F, WarehouseRepository Repository)> Vanilla()
    {
        var f = new ScheduleStockFixture(); await f.Seed(); var db = f.Harness.Db;
        f.Lot.Material.Name = "Vanilla Powder"; f.Lot.Material.Code = "RMV006"; f.Lot.BatchNumber = "R-03506220";
        f.Lot.ExpiryDate = DateTime.UtcNow.AddYears(1);
        var kg = new UnitOfMeasure { Id = Guid.NewGuid(), Name = "Kilogram", Symbol = "kg" };
        db.UnitOfMeasures.Add(kg);
        (await db.MaterialDepartments.SingleAsync()).UoMId = kg.Id; // the department reports in kg
        f.Shelf.Quantity = 149.05m * Kg;
        var start = DateTime.UtcNow.Date.AddDays(-20);
        var rows = new (int Day, decimal Received, decimal Issued, decimal Saved, string Text, Guid Warehouse)[]
        {
            (0, 35.34m, 0, 35.34m, "Stock migration: opening stock", f.Storage.Id),
            (4, 0, 5.43m, 29.91m, "RM/N/26/037", f.Storage.Id),
            (5, 0, 5.43m, 24.48m, "RM/N/26/038", f.Storage.Id),
            (6, 0, 5.43m, 19.05m, "RM/N/26/039", f.Storage.Id),
            (7, 0, 999m, -999m, "Floor issue, not store stock", f.Production.Id),
            // Posted late by the backfill: dated when the transfer was received, saved balance never recomputed.
            (2, 100m, 0, 0, "Stock transfer RM/B/25/011: received from BETA Raw Warehouse", f.Storage.Id),
            (9, 30m, 0, 0, "Stock transfer RM/B/25/022: received from BETA Raw Warehouse", f.Storage.Id),
        };
        var cards = rows.Select(row => new BinCardInformation { MaterialBatchId = f.Lot.Id, WarehouseId = row.Warehouse, UoMId = f.Lot.UoMId,
            QuantityReceived = row.Received * Kg, QuantityIssued = row.Issued * Kg, BalanceQuantity = row.Saved * Kg,
            Description = row.Text, RequisitionCode = row.Text.StartsWith("RM/N") ? row.Text : null }).ToList();
        var elsewhere = new Warehouse { Id = Guid.NewGuid(), Name = "BETA Raw Warehouse", DepartmentId = f.OtherDepartment.Id, Type = WarehouseType.RawMaterialStorage };
        db.Warehouses.Add(elsewhere);
        cards.Add(new() { MaterialBatchId = f.Lot.Id, WarehouseId = elsewhere.Id, UoMId = f.Lot.UoMId, QuantityReceived = 391 * Kg, Description = "Beta receipt" });
        db.BinCardInformation.AddRange(cards);
        await db.SaveChangesAsync();
        for (var i = 0; i < rows.Length; i++) cards[i].CreatedAt = start.AddDays(rows[i].Day);
        cards[^1].CreatedAt = start;
        await db.SaveChangesAsync();
        return (f, new WarehouseRepository(db, f.Mapper, new MaterialRepository(db, f.Mapper), null!));
    }

    [Fact]
    public async Task Bin_card_replays_store_balances_in_date_order_and_reconciles_to_shelves()
    {
        var (f, repository) = await Vanilla(); using var _ = f;
        var result = await repository.GetBinCardInformation(1, 20, null, null, f.Lot.MaterialId, f.Department.Id);
        Assert.True(result.IsSuccess, result.Error.Description);
        var card = result.Value;
        Assert.Equal("kg", card.Unit.Symbol);
        // Newest first; the floor row and Beta's rows are not on Tablet's store card.
        Assert.Equal(new[] { 149.05m, 119.05m, 124.48m, 129.91m, 135.34m, 35.34m }, card.Data.Select(row => row.BalanceQuantity));
        Assert.Equal(new[] { 30m, 0, 0, 0, 100m, 35.34m }, card.Data.Select(row => row.QuantityReceived));
        Assert.All(card.Data, row => Assert.Equal("kg", row.UoM.Symbol));
        Assert.DoesNotContain(card.Data, row => row.BalanceQuantity < 0);
        Assert.Equal(165.34m, card.Received); Assert.Equal(16.29m, card.Issued); Assert.Equal(149.05m, card.Closing);
        Assert.Equal(149.05m, card.ShelfStock); Assert.Equal(0, card.Difference);
        Assert.Equal(card.Closing, card.Received - card.Issued);
    }

    [Fact]
    public async Task Search_and_paging_keep_the_balances_of_the_whole_card()
    {
        var (f, repository) = await Vanilla(); using var _ = f;
        var issues = (await repository.GetBinCardInformation(1, 20, "RM/N/26", null, f.Lot.MaterialId, f.Department.Id)).Value;
        Assert.Equal(new[] { 119.05m, 124.48m, 129.91m }, issues.Data.Select(row => row.BalanceQuantity));
        Assert.Equal(149.05m, issues.Closing);
        var second = (await repository.GetBinCardInformation(2, 4, null, null, f.Lot.MaterialId, f.Department.Id)).Value;
        Assert.Equal(6, second.TotalRecordCount); Assert.Equal(new[] { 135.34m, 35.34m }, second.Data.Select(row => row.BalanceQuantity));
    }

    [Fact]
    public async Task A_missing_receipt_shows_as_a_difference_from_shelf_stock_and_nothing_is_adjusted()
    {
        var (f, repository) = await Vanilla(); using var _ = f; var db = f.Harness.Db;
        db.BinCardInformation.Remove(await db.BinCardInformation.SingleAsync(row => row.Description.Contains("RM/B/25/011")));
        await db.SaveChangesAsync();
        var card = (await repository.GetBinCardInformation(1, 20, null, null, f.Lot.MaterialId, f.Department.Id)).Value;
        Assert.Equal(49.05m, card.Closing); Assert.Equal(-100m, card.Difference); Assert.Equal(149.05m * Kg, f.Shelf.Quantity);
    }

    [Fact]
    public async Task Stock_trace_reads_the_same_ledger_as_the_bin_card()
    {
        var (f, repository) = await Vanilla(); using var _ = f; var db = f.Harness.Db;
        await SharedDensityTransferTests.Grant(db, f.Actor.Id, PermissionKeys.CanViewRawMaterialStockTrace);
        var trace = await new MaterialRepository(db, f.Mapper).GetMaterialStockTrace(f.Lot.MaterialId, MaterialKind.Raw, f.Department.Id,
            DateTime.UtcNow.Date.AddDays(-30), f.Actor.Id);
        Assert.True(trace.IsSuccess, trace.Error.Description);
        var card = (await repository.GetBinCardInformation(1, 20, null, null, f.Lot.MaterialId, f.Department.Id)).Value;
        Assert.Equal(card.Closing, trace.Value.LedgerClosing); Assert.Equal(card.Closing, trace.Value.LedgerRows[^1].Balance);
        Assert.Equal(card.Data.Reverse().Select(row => row.BalanceQuantity), trace.Value.LedgerRows.Select(row => row.Balance));
        Assert.Equal(0, trace.Value.LedgerGap);
    }

    [Fact]
    public async Task Conflicting_department_units_are_refused_rather_than_summed()
    {
        var (f, repository) = await Vanilla(); using var _ = f; var db = f.Harness.Db;
        db.MaterialDepartments.Add(new() { Id = Guid.NewGuid(), MaterialId = f.Lot.MaterialId, DepartmentId = f.Department.Id, UoMId = f.Lot.UoMId });
        await db.SaveChangesAsync();
        Assert.Equal("StockLedger.Unit", (await repository.GetBinCardInformation(1, 20, null, null, f.Lot.MaterialId, f.Department.Id)).Error.Code);
    }

    [BinCardPostgresFact]
    public async Task Backfill_posts_both_sides_of_unposted_transfers_once_and_recomputes_saved_balances()
    {
        var root = new NpgsqlConnectionStringBuilder(Environment.GetEnvironmentVariable("ORYX_ISSUE_REPAIR_TEST_CONNECTION"));
        Assert.Contains(root.Host, new[] { "localhost", "127.0.0.1", "::1", "/tmp" });
        var name = "oryx_bincard_backfill_" + Guid.NewGuid().ToString("N");
        await using (var admin = new NpgsqlConnection(root.ConnectionString))
        {
            await admin.OpenAsync();
            await using (var create = new NpgsqlCommand($"CREATE DATABASE \"{name}\"", admin)) await create.ExecuteNonQueryAsync();
        }
        try
        {
            var builder = new NpgsqlConnectionStringBuilder(root.ConnectionString) { Database = name };
            var actor = new RepairActor();
            await using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(builder.ConnectionString).Options, actor);
            await db.Database.EnsureCreatedAsync();
            db.Users.Add(new DOMAIN.Entities.Users.User { Id = actor.UserId!.Value, UserName = "bincard-backfill-test" });
            await db.SaveChangesAsync();
            var mg = new UnitOfMeasure { Id = Guid.NewGuid(), Name = "Milligram", Symbol = "mg" };
            var beta = new Department { Id = Guid.NewGuid(), Name = "Beta Lactam", Type = DepartmentType.Production };
            var tablet = new Department { Id = Guid.NewGuid(), Name = "Oral Solid-Tablet", Type = DepartmentType.Production };
            var source = new Warehouse { Id = Guid.NewGuid(), Name = "BETA Raw Warehouse", Department = beta, Type = WarehouseType.RawMaterialStorage };
            var target = new Warehouse { Id = Guid.NewGuid(), Name = "TABLET Raw Warehouse", Department = tablet, Type = WarehouseType.RawMaterialStorage };
            var material = new Material { Id = Guid.NewGuid(), Code = "RMV006", Name = "Vanilla Powder", Kind = MaterialKind.Raw };
            var lot = new MaterialBatch { Id = Guid.NewGuid(), Material = material, UoM = mg, BatchNumber = "R-03506220", Status = BatchStatus.Available, TotalQuantity = 391 * Kg };
            var t0 = new DateTime(2026, 11, 2, 14, 39, 0, DateTimeKind.Utc);
            db.AddRange(mg, beta, tablet, source, target, material, lot);
            db.BinCardInformation.AddRange(
                new BinCardInformation { MaterialBatch = lot, WarehouseId = source.Id, UoMId = mg.Id, QuantityReceived = 391 * Kg, BalanceQuantity = 391 * Kg, ArNumber = "QCD/RM/26/495", Supplier = "Supplier A", CreatedAt = t0 },
                new BinCardInformation { MaterialBatch = lot, WarehouseId = target.Id, UoMId = mg.Id, QuantityIssued = 5.43m * Kg, BalanceQuantity = -5.43m * Kg, RequisitionCode = "RM/N/26/037", CreatedAt = t0.AddDays(15) });
            StockTransferSource Source(string code) => new() { Id = Guid.NewGuid(), StockTransfer = new StockTransfer { Id = Guid.NewGuid(), Code = code, MaterialId = material.Id },
                FromDepartmentId = beta.Id, ToDepartmentId = tablet.Id, Quantity = 1, IssuedAt = t0.AddDays(11) };
            var received = Source("RM/B/25/011"); var pending = Source("RM/B/25/099");
            db.StockTransferSources.AddRange(received, pending);
            var done = new HoldingMaterialTransfer { Id = Guid.NewGuid(), StockTransferId = received.StockTransfer.Id, StockTransferSourceId = received.Id,
                Status = HoldingMaterialTransferStatus.Transferred, Batches = [new() { Id = Guid.NewGuid(), MaterialBatchId = lot.Id, Quantity = 100 * Kg,
                    SourceWarehouseId = source.Id, DestinationWarehouseId = target.Id }] };
            var waiting = new HoldingMaterialTransfer { Id = Guid.NewGuid(), StockTransferId = pending.StockTransfer.Id, StockTransferSourceId = pending.Id,
                Status = HoldingMaterialTransferStatus.Pending, Batches = [new() { Id = Guid.NewGuid(), MaterialBatchId = lot.Id, Quantity = 30 * Kg,
                    SourceWarehouseId = source.Id, DestinationWarehouseId = target.Id }] };
            db.HoldingMaterialTransfers.AddRange(done, waiting);
            // An old receipt kept no receipt time; the warehouse movement records when it arrived.
            db.MassMaterialBatchMovements.Add(new() { BatchId = lot.Id, FromWarehouseId = source.Id, ToWarehouseId = target.Id, Quantity = 100 * Kg, MovedAt = t0.AddDays(11).AddHours(1), MovedById = actor.UserId.Value });
            await db.SaveChangesAsync();
            await db.Database.ExecuteSqlRawAsync("UPDATE \"HoldingMaterialTransfers\" SET \"CreatedAt\" = {0}", t0.AddDays(11));
            await db.Database.ExecuteSqlRawAsync("UPDATE \"BinCardInformation\" SET \"CreatedAt\" = {0} WHERE \"RequisitionCode\" IS NOT NULL", t0.AddDays(15));
            await db.Database.ExecuteSqlRawAsync("UPDATE \"BinCardInformation\" SET \"CreatedAt\" = {0} WHERE \"RequisitionCode\" IS NULL", t0);

            async Task Run() { await using var tx = await db.Database.BeginTransactionAsync(); await db.Database.ExecuteSqlRawAsync(PostUnpostedStockTransfersToBinCards.Sql); await tx.CommitAsync(); }
            await Run(); await Run();

            var cards = await db.BinCardInformation.AsNoTracking().OrderBy(row => row.CreatedAt).ToListAsync();
            Assert.Equal(5, cards.Count); // opening, Tablet issue, and three backfilled rows; the second run posted nothing
            var issueOut = cards.Where(row => row.WarehouseId == source.Id && row.QuantityIssued > 0).ToList();
            Assert.Equal(new[] { 100 * Kg, 30 * Kg }, issueOut.Select(row => row.QuantityIssued).Order().Reverse());
            Assert.Contains(issueOut, row => row.Description == "Stock transfer RM/B/25/011: issued to TABLET Raw Warehouse" && row.CreatedAt == t0.AddDays(11));
            var inTablet = Assert.Single(cards, row => row.WarehouseId == target.Id && row.QuantityReceived > 0); // the pending one is not received
            Assert.Equal("Stock transfer RM/B/25/011: received from BETA Raw Warehouse", inTablet.Description);
            Assert.Equal(t0.AddDays(11).AddHours(1), inTablet.CreatedAt); Assert.Equal("QCD/RM/26/495", inTablet.ArNumber);
            Assert.Equal("Supplier A", inTablet.Supplier); Assert.Equal(mg.Id, inTablet.UoMId);
            // Saved balances recomputed in date order: Tablet 100 kg received before the 5.43 kg issue.
            Assert.Equal(100 * Kg, inTablet.BalanceQuantity);
            Assert.Equal(94.57m * Kg, cards.Single(row => row.RequisitionCode == "RM/N/26/037").BalanceQuantity);
            Assert.Equal(261 * Kg, cards.Where(row => row.WarehouseId == source.Id).OrderBy(row => row.CreatedAt).Last().BalanceQuantity);
            Assert.Equal(3, await db.Database.SqlQueryRaw<int>("SELECT COUNT(*)::int AS \"Value\" FROM \"_BinCardTransferBackfill\"").SingleAsync());
        }
        finally
        {
            NpgsqlConnection.ClearAllPools();
            await using var admin = new NpgsqlConnection(root.ConnectionString);
            await admin.OpenAsync();
            await using var drop = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{name}\" WITH (FORCE)", admin);
            await drop.ExecuteNonQueryAsync();
        }
    }
}

internal sealed class BinCardPostgresFactAttribute : FactAttribute
{
    public BinCardPostgresFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ORYX_ISSUE_REPAIR_TEST_CONNECTION")))
            Skip = "Set ORYX_ISSUE_REPAIR_TEST_CONNECTION to an isolated local PostgreSQL server (CREATE DATABASE required).";
    }
}
