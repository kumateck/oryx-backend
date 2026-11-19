using Microsoft.Extensions.DependencyInjection;
using APP.Utils;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Repository;

// GET production-schedule/material-stock 500'd with OverflowException in PgNumeric.Builder.ToDecimal:
// Postgres sums exact density snapshots at full precision and the result no longer fits System.Decimal.
public sealed class QuantityTotalsPostgresTests
{
    private sealed class Actor : ICurrentUserService
    {
        public Guid? UserId => null;
        public Guid? DepartmentId => null;
        public string DepartmentType => "";
    }

    private static readonly Guid MaterialId = Guid.Parse("00000000-0000-0000-0000-000000000002");
    private const decimal Expected = 1333.3333333333333333333333333m;

    [RepairPostgresFact]
    public async Task Production_schedule_stock_sums_read_high_precision_totals_without_overflow()
    {
        await using var connection = new NpgsqlConnection(Environment.GetEnvironmentVariable("ORYX_ISSUE_REPAIR_TEST_CONNECTION"));
        await connection.OpenAsync();
        await using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(connection).Options, new Actor());
        await db.Database.ExecuteSqlRawAsync("""
            CREATE TEMP TABLE "MaterialBatches" ("Id" uuid PRIMARY KEY, "MaterialId" uuid NOT NULL);
            CREATE TEMP TABLE "ShelfMaterialBatches" ("Id" uuid PRIMARY KEY, "MaterialBatchId" uuid NOT NULL,
                "Quantity" numeric NOT NULL, "DeletedAt" timestamptz);
            CREATE TEMP TABLE "MaterialBatchReservedQuantities" ("Id" uuid PRIMARY KEY, "MaterialBatchId" uuid NOT NULL,
                "Quantity" numeric NOT NULL, "DeletedAt" timestamptz, "ReleasedAt" timestamptz);
            CREATE TEMP TABLE "ProductionExtraPackings" ("Id" uuid PRIMARY KEY, "MaterialId" uuid NOT NULL,
                "Quantity" numeric NOT NULL, "DeletedAt" timestamptz);
            INSERT INTO "MaterialBatches" VALUES ('00000000-0000-0000-0000-000000000001','00000000-0000-0000-0000-000000000002');
            INSERT INTO "ShelfMaterialBatches" VALUES
                ('00000000-0000-0000-0000-000000000011','00000000-0000-0000-0000-000000000001',666.66666666666666666666666667,NULL),
                ('00000000-0000-0000-0000-000000000012','00000000-0000-0000-0000-000000000001',666.66666666666666666666666667,NULL);
            INSERT INTO "MaterialBatchReservedQuantities" VALUES
                ('00000000-0000-0000-0000-000000000021','00000000-0000-0000-0000-000000000001',666.66666666666666666666666667,NULL,NULL),
                ('00000000-0000-0000-0000-000000000022','00000000-0000-0000-0000-000000000001',666.66666666666666666666666667,NULL,NULL);
            INSERT INTO "ProductionExtraPackings" VALUES
                ('00000000-0000-0000-0000-000000000031','00000000-0000-0000-0000-000000000002',666.66666666666666666666666667,NULL),
                ('00000000-0000-0000-0000-000000000032','00000000-0000-0000-0000-000000000002',666.66666666666666666666666667,NULL);
            """);
        var shelves = db.ShelfMaterialBatches.IgnoreQueryFilters().IgnoreAutoIncludes().Where(row => row.DeletedAt == null);
        var reservations = db.MaterialBatchReservedQuantities.IgnoreQueryFilters().IgnoreAutoIncludes().Where(row => row.DeletedAt == null);
        var extras = db.ProductionExtraPackings.IgnoreQueryFilters().IgnoreAutoIncludes().Where(row => row.DeletedAt == null);

        // The shape the endpoint used before the fix fails exactly like production.
        await Assert.ThrowsAsync<OverflowException>(() => shelves.GroupBy(row => row.MaterialBatch.MaterialId)
            .Select(group => new { MaterialId = group.Key, Total = group.Sum(row => row.Quantity) })
            .ToDictionaryAsync(row => row.MaterialId, row => row.Total));

        Assert.Equal(Expected, (await shelves.ToTotalsByAsync(row => row.MaterialBatch.MaterialId, row => row.Quantity, true))[MaterialId]);
        Assert.Equal(Expected, (await reservations.ToTotalsByAsync(row => row.MaterialBatch.MaterialId, row => row.Quantity, true))[MaterialId]);
        Assert.Equal(Expected, (await extras.ToTotalsByAsync(row => row.MaterialId, row => row.Quantity, true))[MaterialId]);
        Assert.Empty(await shelves.Where(row => row.Quantity < 0).ToTotalsByAsync(row => row.MaterialBatch.MaterialId, row => row.Quantity, true));
    }
}

public sealed class LiveHoldTotalsPostgresTests
{
    // GET production-schedule/stock-transfer/{id} 500'd after live holds became UnissuedReservations: the reserved-stock
    // resolver summed them through ReservationTotals' Postgres text aggregate.
    [RepairPostgresFact]
    public async Task Reserved_stock_of_live_holds_sums_on_postgres()
    {
        await using var f = new IssuedMaterialRepairFixture();
        await f.Seed(DOMAIN.Entities.Products.Production.IssuedMaterialRepairKind.Bom, issueStock: false);
        await using var db = f.Open();
        var materialId = await db.MaterialDepartments.Select(link => link.MaterialId).FirstAsync();
        var live = APP.Repository.ProductionMaterialAllocator.UnissuedReservations(db).Where(row => row.MaterialBatch.MaterialId == materialId);
        var total = ReservationTotals.Sum(live, true);
        Assert.True(total >= 0);
        Assert.True(await ReservationTotals.SumAsync(live, true) >= 0);
    }
}

public sealed class StockTransferReadPostgresTests
{
    [RepairPostgresFact]
    public async Task Transfer_detail_and_batches_read_on_postgres_for_a_litre_request_from_a_milligram_store()
    {
        await using var f = new IssuedMaterialRepairFixture();
        await f.Seed(DOMAIN.Entities.Products.Production.IssuedMaterialRepairKind.Bom, issueStock: false);
        await using var db = f.Open();
        var link = await db.MaterialDepartments.FirstAsync();
        var material = await db.Materials.FirstAsync(m => m.Id == link.MaterialId);
        material.DensityGramsPerMilliliter = 1.036m;
        var litre = new DOMAIN.Entities.Base.UnitOfMeasure { Id = Guid.NewGuid(), Name = "Litre", Symbol = "L" };
        var receiver = new DOMAIN.Entities.Departments.Department { Id = Guid.NewGuid(), Name = "Syrup", Type = SHARED.DepartmentType.Production };
        var transfer = new DOMAIN.Entities.ProductionSchedules.StockTransfers.StockTransferSource { Id = Guid.NewGuid(),
            StockTransfer = new() { Id = Guid.NewGuid(), Code = "ST-PG", MaterialId = material.Id, UoM = litre, RequiredQuantity = 20_000 },
            FromDepartmentId = f.Actor.DepartmentId!.Value, ToDepartment = receiver, Quantity = 20_000,
            Status = DOMAIN.Entities.ProductionSchedules.StockTransfers.StockTransferStatus.Approved };
        db.AddRange(litre, transfer); await db.SaveChangesAsync(); db.ChangeTracker.Clear();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<ApplicationDbContext>(db);
        services.AddSingleton<Microsoft.AspNetCore.Http.IHttpContextAccessor, APP.Tests.QcWorksheets.NullHttpContextAccessor>();
        services.AddAutoMapper(cfg => { }, typeof(APP.Mapper.OryxMapper));
        await using var provider = services.BuildServiceProvider();
        var mapper = provider.GetRequiredService<AutoMapper.IMapper>();
        var repository = new APP.Repository.ProductionScheduleRepository(db, mapper, null!, new APP.Repository.MaterialRepository(db, mapper), null!, null!,
            new NoOpProductionActivityStepEventPublisher());

        var read = await repository.GetStockTransferSource(transfer.Id);
        Assert.True(read.IsSuccess, read.IsFailure ? read.Error.Description : null);
        Assert.Equal("mg", read.Value.SupplierUoM.Symbol);
        var batches = await repository.BatchesToSupplyForStockTransfer(transfer.Id);
        Assert.True(batches.IsSuccess || batches.Error.Code == "StockTransfer.InsufficientStock", batches.IsFailure ? batches.Error.Description : null);
    }
}
