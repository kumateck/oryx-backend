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

/// <summary>Stock entered by hand in the warehouse: picked materials and shelves, full dates and a retest date.</summary>
public sealed class StockTopUpManualEntryPostgresTests
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
    public async Task Manual_and_legacy_exact_entry_use_months_and_keep_original_date_evidence()
    {
        var database = "oryx_topup_manual_" + Guid.NewGuid().ToString("N");
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
            var department = new Department { Id = Guid.NewGuid(), Name = "Manual top-up", Type = DepartmentType.Production };
            db.Departments.Add(department);
            db.Users.Add(new User { Id = actor.UserId!.Value, UserName = "manual-topup" });
            var unit = new UnitOfMeasure { Id = Guid.NewGuid(), Symbol = "mg", Name = "Milligram", Category = UnitOfMeasureCategory.Weight };
            db.UnitOfMeasures.Add(unit);
            var material = new Material { Id = Guid.NewGuid(), Code = "RMA016", Name = "Amaranth (W/S)", Kind = MaterialKind.Raw };
            db.Materials.Add(material);
            db.MaterialDepartments.Add(new MaterialDepartment { MaterialId = material.Id, DepartmentId = department.Id, UoMId = unit.Id });
            var shelf = new WarehouseLocationShelf { Id = Guid.NewGuid(), Name = "B2", Code = "RM/B2", WarehouseLocationRack = new WarehouseLocationRack
            { Name = "Rack", WarehouseLocation = new WarehouseLocation { Name = "Room", Warehouse = new Warehouse { Name = "Raw store", Type = WarehouseType.RawMaterialStorage, DepartmentId = department.Id } } } };
            db.WarehouseLocationShelves.Add(shelf);
            db.MaterialBatches.Add(new MaterialBatch { Id = Guid.NewGuid(), MaterialId = material.Id, UoMId = unit.Id, BatchNumber = "LOT-OLD", Status = BatchStatus.Available, TotalQuantity = 1, QuantityAssigned = 1 });
            await db.SaveChangesAsync();
            var repository = new StockMigrationRepository(db);

            var made = Day(2026, 8, 3);
            var expires = Day(2029, 7, 14);
            var retest = Day(2027, 8, 2);
            var request = new CreateStockTopUpRequest { DepartmentId = department.Id, Reason = "Received in August; missed by the migration", ExactDates = true, MassUnit = "kg", Lines = [
                new() { MaterialId = material.Id, ShelfId = shelf.Id, BatchNumber = "LOT-NEW", UnitSymbol = "mg", QuantityInSheet = 25,
                    ArNumber = "AR/RM/0912", ManufacturingDate = made, ExpiryDate = expires, RetestDate = retest },
                new() { MaterialId = material.Id, ShelfId = shelf.Id, BatchNumber = "lot-old", UnitSymbol = "mg", QuantityInSheet = 1, ArNumber = "AR/RM/0913" },
            ] };
            var draft = await repository.CreateTopUp(request, actor.UserId.Value);
            Assert.True(draft.IsSuccess, draft.Error?.Description);
            var lines = await db.StockMigrationLines.AsNoTracking().Where(l => l.StockMigrationId == draft.Value).OrderBy(l => l.RowNumber).ToListAsync();
            Assert.All(lines, line => Assert.False(line.ExactDates));
            Assert.Equal((Day(2026, 8, 1), Day(2029, 7, 1), Day(2027, 8, 31)), (lines[0].ManufacturingDate!.Value, lines[0].ExpiryDate!.Value, lines[0].RetestDate!.Value));
            Assert.Equal("2029-07-14", lines[0].SheetExpiryDate);
            Assert.Equal(StockMigrationLineStatus.Ok, lines[0].Status);
            Assert.Equal(25_000_000, lines[0].Quantity);
            // The same batch number already in stock is flagged, not refused.
            Assert.Equal(StockMigrationLineStatus.Warning, lines[1].Status);
            Assert.Contains("already in stock", lines[1].Warnings);

            // A second draft with the same batch is flagged against the first, which is still waiting.
            var again = await repository.CreateTopUp(new CreateStockTopUpRequest { DepartmentId = department.Id, Reason = "Entered twice", ExactDates = true,
                Lines = [new() { MaterialId = material.Id, ShelfId = shelf.Id, BatchNumber = "LOT-NEW", UnitSymbol = "mg", QuantityInSheet = 25 }] }, actor.UserId.Value);
            Assert.Contains("another top-up draft", (await db.StockMigrationLines.AsNoTracking().SingleAsync(l => l.StockMigrationId == again.Value)).Warnings);
            Assert.True((await repository.Discard(again.Value)).IsSuccess);

            // Retest dates outside the lot's life are blocked; a passed retest date is a warning.
            var invalid = await repository.CreateTopUp(new CreateStockTopUpRequest { DepartmentId = department.Id, Reason = "Bad dates", ExactDates = true, Lines = [
                new() { MaterialId = material.Id, ShelfId = shelf.Id, BatchNumber = "LOT-A", UnitSymbol = "mg", QuantityInSheet = 1, ExpiryDate = Day(2028, 1, 31), RetestDate = Day(2028, 2, 1) },
                new() { MaterialId = material.Id, ShelfId = shelf.Id, BatchNumber = "LOT-B", UnitSymbol = "mg", QuantityInSheet = 1, ManufacturingDate = Day(2026, 5, 1), RetestDate = Day(2026, 4, 30) },
                new() { MaterialId = material.Id, ShelfId = shelf.Id, BatchNumber = "LOT-C", UnitSymbol = "mg", QuantityInSheet = 1, RetestDate = DateTime.UtcNow.Date.AddMonths(-1) },
            ] }, actor.UserId.Value);
            var checkedLines = await db.StockMigrationLines.AsNoTracking().Where(l => l.StockMigrationId == invalid.Value).OrderBy(l => l.RowNumber).ToListAsync();
            Assert.Contains("retest date is after the expiry", checkedLines[0].Blockers);
            Assert.Contains("retest date is before the manufacturing", checkedLines[1].Blockers);
            Assert.Equal(StockMigrationLineStatus.Warning, checkedLines[2].Status);
            Assert.Contains("due for retest", checkedLines[2].Warnings);
            Assert.True((await repository.Discard(invalid.Value)).IsSuccess);

            // A correction that does not send the retest date keeps it, keeps full dates, and can clear it on purpose.
            var correction = request.Lines[0];
            correction.RetestDate = null;
            correction.ExpiryDate = Day(2029, 7, 20);
            Assert.True((await repository.UpdateLine(draft.Value, lines[0].Id, correction)).IsSuccess);
            var corrected = await db.StockMigrationLines.AsNoTracking().SingleAsync(l => l.Id == lines[0].Id);
            Assert.Equal((Day(2027, 8, 31), Day(2029, 7, 1)), (corrected.RetestDate!.Value, corrected.ExpiryDate!.Value));
            correction.ClearRetestDate = true;
            Assert.True((await repository.UpdateLine(draft.Value, lines[0].Id, correction)).IsSuccess);
            Assert.Null((await db.StockMigrationLines.AsNoTracking().SingleAsync(l => l.Id == lines[0].Id)).RetestDate);
            correction.ClearRetestDate = false;
            correction.RetestDate = retest;
            Assert.True((await repository.UpdateLine(draft.Value, lines[0].Id, correction)).IsSuccess);

            // Approval posts month-end expiry/retest and month-start manufacturing onto the new lot.
            var approval = await repository.Approve(draft.Value, actor.UserId.Value);
            Assert.True(approval.IsSuccess, approval.Error?.Description);
            var lot = await db.MaterialBatches.AsNoTracking().SingleAsync(b => b.BatchNumber == "LOT-NEW");
            Assert.Equal((Day(2029, 7, 31), Day(2027, 8, 31), Day(2026, 8, 1)), (lot.ExpiryDate!.Value, lot.RetestDate!.Value, lot.ManufacturingDate!.Value));
            Assert.Equal(25_000_000, await db.ShelfMaterialBatches.Where(s => s.MaterialBatchId == lot.Id).SumAsync(s => s.Quantity));
            Assert.Equal(2, await db.MaterialBatches.CountAsync(b => b.BatchNumber.ToUpper() == "LOT-OLD"));

            // Sheet top-ups keep their month rule: expiry is the end of the month.
            var sheet = await repository.CreateTopUp(new CreateStockTopUpRequest { DepartmentId = department.Id, Reason = "Month dates", Lines = [
                new() { MaterialId = material.Id, ShelfId = shelf.Id, BatchNumber = "LOT-MONTH", UnitSymbol = "mg", QuantityInSheet = 1, ExpiryDate = Day(2029, 2, 10) }] }, actor.UserId.Value);
            Assert.True((await repository.Approve(sheet.Value, actor.UserId.Value)).IsSuccess);
            Assert.Equal(Day(2029, 2, 28), (await db.MaterialBatches.AsNoTracking().SingleAsync(b => b.BatchNumber == "LOT-MONTH")).ExpiryDate);
        }
        finally
        {
            NpgsqlConnection.ClearAllPools();
            await using var drop = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{database}\" WITH (FORCE)", admin);
            await drop.ExecuteNonQueryAsync();
        }
    }
}
