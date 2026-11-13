using APP.Utils;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Repository;

public sealed class ReservationTotalsPostgresTests
{
    private sealed class PostgresFactAttribute : FactAttribute
    {
        public PostgresFactAttribute()
        {
            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ORYX_MATERIAL_TOTALS_TEST_CONNECTION")))
                Skip = "Set ORYX_MATERIAL_TOTALS_TEST_CONNECTION to an isolated PostgreSQL test server.";
        }
    }

    private sealed class Actor : ICurrentUserService
    {
        public Guid? UserId => null;
        public Guid? DepartmentId => null;
        public string DepartmentType => "";
    }

    [PostgresFact]
    public async Task High_precision_reservations_sum_without_decoder_overflow_or_changing_stored_values()
    {
        await using var connection = new NpgsqlConnection(Environment.GetEnvironmentVariable("ORYX_MATERIAL_TOTALS_TEST_CONNECTION"));
        await connection.OpenAsync();
        await using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(connection).Options, new Actor());
        await db.Database.ExecuteSqlRawAsync("""
            CREATE TEMP TABLE "MaterialBatches" ("Id" uuid PRIMARY KEY, "MaterialId" uuid NOT NULL);
            CREATE TEMP TABLE "MaterialBatchReservedQuantities" (
                "Id" uuid PRIMARY KEY, "MaterialBatchId" uuid NOT NULL, "Quantity" numeric NOT NULL,
                "DeletedAt" timestamptz, "ReleasedAt" timestamptz);
            INSERT INTO "MaterialBatches" VALUES
                ('00000000-0000-0000-0000-000000000001','00000000-0000-0000-0000-000000000002');
            INSERT INTO "MaterialBatchReservedQuantities" ("Id", "MaterialBatchId", "Quantity") VALUES
                ('00000000-0000-0000-0000-000000000003','00000000-0000-0000-0000-000000000001',694.44444444444444444444444444),
                ('00000000-0000-0000-0000-000000000004','00000000-0000-0000-0000-000000000001',694.44444444444444444444444444);
            """);
        var materialId = Guid.Parse("00000000-0000-0000-0000-000000000002");
        var active = db.MaterialBatchReservedQuantities.IgnoreQueryFilters()
            .Where(row => row.MaterialBatch.MaterialId == materialId && row.DeletedAt == null && row.ReleasedAt == null);
        await Assert.ThrowsAsync<OverflowException>(() => active.SumAsync(row => row.Quantity));

        const decimal expected = 1388.8888888888888888888888889m;
        Assert.Equal(expected, ReservationTotals.Sum(active, true));
        Assert.Equal(expected, await ReservationTotals.SumAsync(active, true));
        Assert.Equal(expected, (await ReservationTotals.ByMaterialAsync(active, true))[materialId]);
        Assert.Equal(0m, await ReservationTotals.SumAsync(active.Where(row => row.Quantity == 0), true));

        var original = await db.Database.SqlQueryRaw<string>(
            """SELECT "Quantity"::text AS "Value" FROM "MaterialBatchReservedQuantities" ORDER BY "Id" """).ToListAsync();
        Assert.All(original, value => Assert.Equal("694.44444444444444444444444444", value));
    }
}
