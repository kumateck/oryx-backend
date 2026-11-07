using APP.Repository;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Departments;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.Materials.StockMigrations;
using DOMAIN.Entities.Users;
using DOMAIN.Entities.Warehouses;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SHARED;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Repository;

/// <summary>
/// A top-up row whose batch is already on the department's shelves adds to that lot when the AR number and
/// dates match, and becomes a separate lot when they differ.
/// </summary>
public sealed class StockTopUpExistingBatchPostgresTests
{
    private sealed class PostgresFactAttribute : FactAttribute
    {
        public PostgresFactAttribute()
        {
            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ORYX_STOCK_TOPUP_TEST_CONNECTION")))
                Skip = "Set ORYX_STOCK_TOPUP_TEST_CONNECTION to an isolated PostgreSQL test server (requires CREATE DATABASE).";
        }
    }

    private sealed class Actor : ICurrentUserService
    {
        public Guid? UserId { get; } = Guid.NewGuid();
        public Guid? DepartmentId => null;
        public string DepartmentType => string.Empty;
    }

    private static DateTime Day(int year, int month, int day) => new(year, month, day, 0, 0, 0, DateTimeKind.Utc);

    [PostgresFact]
    public async Task Matching_batch_adds_to_the_existing_lot_and_a_different_one_stays_separate()
    {
        var database = "oryx_topup_existing_" + Guid.NewGuid().ToString("N");
        var connection = new NpgsqlConnectionStringBuilder(Environment.GetEnvironmentVariable("ORYX_STOCK_TOPUP_TEST_CONNECTION"));
        await using var admin = new NpgsqlConnection(connection.ConnectionString);
        await admin.OpenAsync();
        await using (var create = new NpgsqlCommand($"CREATE DATABASE \"{database}\"", admin)) await create.ExecuteNonQueryAsync();
        connection.Database = database;
        try
        {
            var actor = new Actor();
            await using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(connection.ConnectionString).Options, actor);
            await db.Database.EnsureCreatedAsync();
            var department = new Department { Id = Guid.NewGuid(), Name = "Existing batch top-up", Type = DepartmentType.Production };
            db.Departments.Add(department);
            db.Users.Add(new User { Id = actor.UserId!.Value, UserName = "existing-topup" });
            var unit = new UnitOfMeasure { Id = Guid.NewGuid(), Symbol = "mg", Name = "Milligram", Category = UnitOfMeasureCategory.Weight };
            db.UnitOfMeasures.Add(unit);
            var material = new Material { Id = Guid.NewGuid(), Code = "RMD002", Name = "Dextromethorphan HBr", Kind = MaterialKind.Raw };
            db.Materials.Add(material);
            db.MaterialDepartments.Add(new MaterialDepartment { MaterialId = material.Id, DepartmentId = department.Id, UoMId = unit.Id });
            var shelf = new WarehouseLocationShelf { Id = Guid.NewGuid(), Name = "A1", Code = "RM/A1", WarehouseLocationRack = new WarehouseLocationRack
            { Name = "Rack", WarehouseLocation = new WarehouseLocation { Name = "Room", Warehouse = new Warehouse { Name = "Raw store", Type = WarehouseType.RawMaterialStorage, DepartmentId = department.Id } } } };
            db.WarehouseLocationShelves.Add(shelf);
            await db.SaveChangesAsync();
            var repository = new StockMigrationRepository(db);
            var made = Day(2026, 1, 5);
            var expires = Day(2028, 1, 31);
            UpdateStockMigrationLineRequest Row(string batch, string ar, decimal quantity, DateTime? madeOn = null) => new()
            {
                MaterialId = material.Id, ShelfId = shelf.Id, BatchNumber = batch, UnitSymbol = "mg", QuantityInSheet = quantity,
                ArNumber = ar, ManufacturingDate = madeOn ?? made, ExpiryDate = expires,
            };
            async Task<Guid> Draft(params UpdateStockMigrationLineRequest[] rows)
            {
                var draft = await repository.CreateTopUp(new CreateStockTopUpRequest { DepartmentId = department.Id, Reason = "Found after migration", ExactDates = true, Lines = [.. rows] }, actor.UserId.Value);
                Assert.True(draft.IsSuccess, draft.Error?.Description);
                return draft.Value;
            }

            // The first top-up creates the lot DXL034/21 with AR NQC/RM/22/153-01.
            var first = await Draft(Row("DXL034/21", "NQC/RM/22/153-01", 100));
            Assert.True((await repository.Approve(first, actor.UserId.Value)).IsSuccess);
            var lot = await db.MaterialBatches.AsNoTracking().SingleAsync(b => b.BatchNumber == "DXL034/21");

            // Same batch, AR and dates (in different case) adds to it; a different AR or manufacturing date does not.
            var second = await Draft(
                Row("dxl034/21", " nqc/rm/22/153-01 ", 40),
                Row("DXL034/21", "NQC/RM/22/999-01", 5),
                Row("DXL034/21", "NQC/RM/22/153-01", 3, Day(2026, 2, 5)),
                Row("NEW-LOT", "AR-NEW", 7));
            var lines = await db.StockMigrationLines.AsNoTracking().Where(l => l.StockMigrationId == second).OrderBy(l => l.RowNumber).ToListAsync();
            Assert.Contains("Approval adds this quantity to that batch", lines[0].Warnings);
            Assert.Contains("different AR number", lines[1].Warnings);
            Assert.Contains("separate lot", lines[1].Warnings);
            Assert.Contains("different manufacturing date", lines[2].Warnings);
            Assert.DoesNotContain("already in stock", lines[3].Warnings ?? string.Empty);

            // The preview counts the matched row as stock on the existing lot, not a new lot.
            var preview = Assert.Single((await repository.Comparison(second)).Value);
            Assert.Equal((100m, 155m, 1, 4), (preview.Current, preview.New, preview.CurrentBatches, preview.NewBatches));

            Assert.True((await repository.Approve(second, actor.UserId.Value)).IsSuccess);
            var refilled = await db.MaterialBatches.AsNoTracking().SingleAsync(b => b.Id == lot.Id);
            Assert.Equal(140, refilled.TotalQuantity);
            Assert.Equal(140, Assert.Single(await db.ShelfMaterialBatches.AsNoTracking().Where(s => s.MaterialBatchId == lot.Id).ToListAsync()).Quantity);
            Assert.Equal(3, await db.MaterialBatches.CountAsync(b => b.BatchNumber == "DXL034/21"));
            var approvedLines = await db.StockMigrationLines.AsNoTracking().Where(l => l.StockMigrationId == second).OrderBy(l => l.RowNumber).ToListAsync();
            Assert.Equal(lot.Id, approvedLines[0].ImportedBatchId);
            Assert.All(approvedLines.Skip(1), l => Assert.NotEqual(lot.Id, l.ImportedBatchId));
            var receipts = await db.BinCardInformation.AsNoTracking().Where(b => b.MaterialBatchId == lot.Id).OrderBy(b => b.BalanceQuantity).ToListAsync();
            Assert.Equal(new decimal[] { 100, 40 }, receipts.Select(r => r.QuantityReceived));
            var after = Assert.Single((await repository.Comparison(second)).Value);
            Assert.Equal((100m, 155m, 1, 4), (after.Current, after.New, after.CurrentBatches, after.NewBatches));

            // A lot that was used up is refilled and becomes available again.
            await db.ShelfMaterialBatches.Where(s => s.MaterialBatchId == lot.Id).ExecuteUpdateAsync(s => s.SetProperty(x => x.Quantity, 0));
            await db.MaterialBatches.Where(b => b.Id == lot.Id).ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, BatchStatus.Consumed));
            db.ChangeTracker.Clear();
            var third = await Draft(Row("DXL034/21", "NQC/RM/22/153-01", 9));
            Assert.True((await repository.Approve(third, actor.UserId.Value)).IsSuccess);
            var revived = await db.MaterialBatches.AsNoTracking().SingleAsync(b => b.Id == lot.Id);
            Assert.Equal((BatchStatus.Available, 149m), (revived.Status, revived.TotalQuantity));
            Assert.Equal(9, await db.ShelfMaterialBatches.AsNoTracking().Where(s => s.MaterialBatchId == lot.Id).SumAsync(s => s.Quantity));

            // A rejected lot is never refilled.
            await db.MaterialBatches.Where(b => b.Id == lot.Id).ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, BatchStatus.Rejected));
            db.ChangeTracker.Clear();
            var fourth = await Draft(Row("DXL034/21", "NQC/RM/22/153-01", 2));
            Assert.Contains("approval adds it as a separate lot", (await db.StockMigrationLines.AsNoTracking().SingleAsync(l => l.StockMigrationId == fourth)).Warnings);
            Assert.True((await repository.Approve(fourth, actor.UserId.Value)).IsSuccess);
            Assert.Equal(149, (await db.MaterialBatches.AsNoTracking().SingleAsync(b => b.Id == lot.Id)).TotalQuantity);
        }
        finally
        {
            NpgsqlConnection.ClearAllPools();
            await using var drop = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{database}\" WITH (FORCE)", admin);
            await drop.ExecuteNonQueryAsync();
        }
    }
}
