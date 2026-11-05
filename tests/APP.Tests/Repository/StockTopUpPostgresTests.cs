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

public sealed class StockTopUpPostgresTests
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

    [PostgresFact]
    public async Task Draft_approval_is_additive_traceable_atomic_and_cannot_be_repeated()
    {
        var database = "oryx_topup_test_" + Guid.NewGuid().ToString("N");
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
            var department = new Department { Id = Guid.NewGuid(), Name = "Top-up test", Type = DepartmentType.Production };
            db.Departments.Add(department);
            db.Users.Add(new User { Id = actor.UserId!.Value, UserName = "topup-test" });
            var unit = new UnitOfMeasure { Id = Guid.NewGuid(), Symbol = "mg", Name = "Milligram", Category = UnitOfMeasureCategory.Weight };
            db.UnitOfMeasures.Add(unit);
            var material = new Material { Id = Guid.NewGuid(), Code = "TEST-RM", Name = "Test material", Kind = MaterialKind.Raw };
            db.Materials.Add(material);
            db.MaterialDepartments.Add(new MaterialDepartment { MaterialId = material.Id, DepartmentId = department.Id, UoMId = unit.Id });
            var shelf = new WarehouseLocationShelf { Id = Guid.NewGuid(), Name = "A", Code = "TEST/A", WarehouseLocationRack = new WarehouseLocationRack
            { Name = "Rack", WarehouseLocation = new WarehouseLocation { Name = "Room", Warehouse = new Warehouse { Name = "Test raw store", Type = WarehouseType.RawMaterialStorage, DepartmentId = department.Id } } } };
            db.WarehouseLocationShelves.Add(shelf);
            var existing = new MaterialBatch { Id = Guid.NewGuid(), MaterialId = material.Id, UoMId = unit.Id, BatchNumber = "OLD", Status = BatchStatus.Available, TotalQuantity = 100, QuantityAssigned = 100 };
            db.MaterialBatches.Add(existing);
            var stored = new ShelfMaterialBatch { Id = Guid.NewGuid(), MaterialBatchId = existing.Id, WarehouseLocationShelfId = shelf.Id, UoMId = unit.Id, Quantity = 100 };
            db.ShelfMaterialBatches.Add(stored);
            await db.SaveChangesAsync();
            var repository = new StockMigrationRepository(db);
            var request = new CreateStockTopUpRequest { DepartmentId = department.Id, Reason = "Discovered after migration", Lines = [
                new() { MaterialCode = material.Code, ShelfId = shelf.Id, BatchNumber = "NEW", UnitSymbol = "mg", QuantityInSheet = 10, Grn = "GRN-1", ArNumber = "AR-1" },
                new() { MaterialCode = material.Code, ShelfId = shelf.Id, BatchNumber = "NEW", UnitSymbol = "mg", QuantityInSheet = 20, Grn = "GRN-2", ArNumber = "AR-2" },
            ] };
            var draft = await repository.CreateTopUp(request, actor.UserId.Value);
            Assert.True(draft.IsSuccess, draft.Error?.Description);
            Assert.Equal(100, await db.ShelfMaterialBatches.SumAsync(s => s.Quantity));
            Assert.Empty(await db.BinCardInformation.ToListAsync());
            db.BinCardInformation.Add(new DOMAIN.Entities.BinCards.BinCardInformation
            {
                MaterialBatchId = existing.Id, WarehouseId = shelf.WarehouseLocationRack.WarehouseLocation.Warehouse.Id,
                UoMId = unit.Id, QuantityReceived = 100, BalanceQuantity = 100, Description = "Existing receipt",
            });
            await db.SaveChangesAsync();
            var approval = await repository.Approve(draft.Value, actor.UserId.Value);
            Assert.True(approval.IsSuccess, approval.Error?.Description);
            Assert.Equal(130, await db.ShelfMaterialBatches.SumAsync(s => s.Quantity));
            Assert.Equal(100, stored.Quantity);
            Assert.Equal(BatchStatus.Available, existing.Status);
            Assert.Empty(await db.StockMigrationWriteOffs.ToListAsync());
            var lines = await db.StockMigrationLines.Where(l => l.StockMigrationId == draft.Value).ToListAsync();
            Assert.Equal(2, lines.Select(l => l.ImportedBatchId).Distinct().Count());
            Assert.All(lines, line => Assert.NotNull(line.ImportedBatchId));
            var receipts = await db.BinCardInformation.Where(b => b.MaterialBatchId != existing.Id).OrderBy(b => b.ArNumber).ToListAsync();
            Assert.Equal(new[] { "AR-1", "AR-2" }, receipts.Select(b => b.ArNumber));
            Assert.Equal(new decimal[] { 110, 130 }, receipts.Select(b => b.BalanceQuantity));
            Assert.All(receipts, b => Assert.Contains(draft.Value.ToString(), b.Description));
            var repeat = await repository.Approve(draft.Value, actor.UserId.Value);
            Assert.True(repeat.IsFailure);
            Assert.Equal(130, await db.ShelfMaterialBatches.SumAsync(s => s.Quantity));
            var edit = await repository.UpdateLine(draft.Value, lines[0].Id, request.Lines[0]);
            Assert.True(edit.IsFailure);
            stored.Quantity = 50;
            await db.SaveChangesAsync();
            var comparison = await repository.Comparison(draft.Value);
            Assert.Equal(100, Assert.Single(comparison.Value).Current);
            Assert.Equal(30, comparison.Value[0].Added);
            Assert.Equal(130, comparison.Value[0].New);

            // Invalid rows cannot post even the valid part of a list.
            request.Lines = [new() { MaterialCode = "MISSING", ShelfId = shelf.Id, BatchNumber = "", UnitSymbol = "mg", QuantityInSheet = 5 }];
            var blocked = await repository.CreateTopUp(request, actor.UserId.Value);
            Assert.True(blocked.IsSuccess);
            Assert.True((await repository.Approve(blocked.Value, actor.UserId.Value)).IsFailure);
            Assert.Equal(80, await db.ShelfMaterialBatches.SumAsync(s => s.Quantity));
            var blockedLine = await db.StockMigrationLines.SingleAsync(l => l.StockMigrationId == blocked.Value);
            request.Lines[0].BatchNumber = "CONCURRENT";
            request.Lines[0].MaterialCode = material.Code;
            Assert.True((await repository.UpdateLine(blocked.Value, blockedLine.Id, request.Lines[0])).IsSuccess);

            // Two independent approvers race for the same draft; only one can post it.
            async Task<Result> ApproveSeparately()
            {
                await using var other = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(connection.ConnectionString).Options, actor);
                return await new StockMigrationRepository(other).Approve(blocked.Value, actor.UserId.Value);
            }
            var raced = await Task.WhenAll(ApproveSeparately(), ApproveSeparately());
            Assert.Single(raced.Where(result => result.IsSuccess));
            Assert.Single(raced.Where(result => result.IsFailure));
            Assert.Equal(85, await db.ShelfMaterialBatches.SumAsync(s => s.Quantity));

            // A database refusal after the approval claim rolls back every posting and the claim.
            request.Lines[0].BatchNumber = "ROLLBACK";
            var failing = await repository.CreateTopUp(request, actor.UserId.Value);
            await db.Database.ExecuteSqlRawAsync("ALTER TABLE \"StockMigrationBalances\" ADD CONSTRAINT reject_test_topup CHECK (false) NOT VALID");
            await using (var failingDb = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(connection.ConnectionString).Options, actor))
                await Assert.ThrowsAsync<DbUpdateException>(() => new StockMigrationRepository(failingDb).Approve(failing.Value, actor.UserId.Value));
            Assert.Equal(85, await db.ShelfMaterialBatches.SumAsync(s => s.Quantity));
            Assert.False(await db.MaterialBatches.AnyAsync(b => b.BatchNumber == "ROLLBACK"));
            Assert.Equal(StockMigrationStatus.Draft, await db.StockMigrations.AsNoTracking().Where(m => m.Id == failing.Value).Select(m => m.Status).SingleAsync());
            await db.Database.ExecuteSqlRawAsync("ALTER TABLE \"StockMigrationBalances\" DROP CONSTRAINT reject_test_topup");

            // Missing references are optional and never merge separate source rows into one lot.
            request.Lines = [
                new() { MaterialCode = material.Code, ShelfId = shelf.Id, UnitSymbol = "mg", QuantityInSheet = 2 },
                new() { MaterialCode = material.Code, ShelfId = shelf.Id, BatchNumber = " ", UnitSymbol = "mg", QuantityInSheet = 3 },
            ];
            var unnumbered = await repository.CreateTopUp(request, actor.UserId.Value);
            Assert.True(unnumbered.IsSuccess);
            Assert.True((await repository.Approve(unnumbered.Value, actor.UserId.Value)).IsSuccess);
            var unnumberedLines = await db.StockMigrationLines.Where(l => l.StockMigrationId == unnumbered.Value).ToListAsync();
            Assert.Equal(2, unnumberedLines.Select(l => l.ImportedBatchId).Distinct().Count());
            Assert.Equal(90, await db.ShelfMaterialBatches.SumAsync(s => s.Quantity));
            var batchIds = unnumberedLines.Select(l => l.ImportedBatchId).ToList();
            Assert.All(await db.MaterialBatches.Where(b => batchIds.Contains(b.Id)).ToListAsync(), b => Assert.Equal("", b.BatchNumber));
        }
        finally
        {
            NpgsqlConnection.ClearAllPools();
            await using var drop = new NpgsqlCommand($"DROP DATABASE \"{database}\" WITH (FORCE)", admin);
            await drop.ExecuteNonQueryAsync();
        }
    }
}
