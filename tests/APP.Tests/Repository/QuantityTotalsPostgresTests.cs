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
