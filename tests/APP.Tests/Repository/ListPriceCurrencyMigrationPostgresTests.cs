using INFRASTRUCTURE.Context;
using INFRASTRUCTURE.Reporting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Repository;

public sealed class LocalListPriceMigrationTheoryAttribute : TheoryAttribute
{
    public LocalListPriceMigrationTheoryAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ORYX_LIST_PRICE_MIGRATION_TEST_CONNECTION")))
            Skip = "Requires the separately provisioned local disposable list-price migration database.";
    }
}

public sealed class ListPriceCurrencyMigrationPostgresTests
{
    private const string Previous = "20261030190441_AddStockMigrations";
    private const string Target = "20261031061119_AddListPriceCurrency";
    private static readonly Guid BaseCurrency = Guid.Parse("00000000-0000-0000-0000-000000000002");

    private sealed class Actor : ICurrentUserService
    {
        public Guid? UserId => null;
        public Guid? DepartmentId => null;
        public string DepartmentType => string.Empty;
    }

    [LocalListPriceMigrationTheory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public async Task CurrencyBackfillPreservesPricesAndHistoryWithDeferredTriggers(bool hasBaseCurrency, bool idempotentSql)
    {
        var connectionString = FixtureConnection();
        await using var database = new NpgsqlConnection(connectionString);
        await database.OpenAsync();
        await Execute(database, "DROP SCHEMA public CASCADE; CREATE SCHEMA public;");
        await Execute(database, """
            CREATE TABLE "Currencies" (
              "Id" uuid PRIMARY KEY, "IsBaseCurrency" boolean NOT NULL,
              "CreatedAt" timestamptz NOT NULL, "DeletedAt" timestamptz);
            CREATE TABLE "Products" ("Id" uuid PRIMARY KEY);
            CREATE TABLE "ProductPrices" (
              "Id" integer NOT NULL, "ProductId" uuid NOT NULL REFERENCES "Products"("Id"),
              "Price" numeric NOT NULL, PRIMARY KEY ("ProductId", "Id"));
            CREATE TABLE "__EFMigrationsHistory" (
              "MigrationId" varchar(150) PRIMARY KEY, "ProductVersion" varchar(32) NOT NULL);
            INSERT INTO "Products" VALUES ('10000000-0000-0000-0000-000000000001');
            INSERT INTO "ProductPrices" VALUES
              (1, '10000000-0000-0000-0000-000000000001', 9.875),
              (2, '10000000-0000-0000-0000-000000000001', 0.000001);
            INSERT INTO "Currencies" VALUES
              ('00000000-0000-0000-0000-000000000001', true, '2020-01-01T00:00:00Z', '2020-02-01T00:00:00Z'),
              ('00000000-0000-0000-0000-000000000002', false, '2021-01-01T00:00:00Z', NULL),
              ('00000000-0000-0000-0000-000000000003', false, '2022-01-01T00:00:00Z', NULL);
            """);
        if (hasBaseCurrency)
            await Execute(database, "UPDATE \"Currencies\" SET \"IsBaseCurrency\"=true WHERE \"DeletedAt\" IS NULL;");

        // Use the production reporting implementation, including its initially deferred capture
        // trigger. A trigger-free scratch database does not reproduce the DigitalOcean failure.
        await Execute(database, ReportHistorySchema.InstallSql);
        Assert.Equal(1L, await Scalar<long>(database, """
            SELECT count(*) FROM pg_trigger
            WHERE tgrelid='"ProductPrices"'::regclass AND tgname='oryx_report_capture'
              AND tgdeferrable AND tginitdeferred AND tgenabled='O'
            """));
        Assert.Equal(2L, await PriceHistoryCount(database));

        await using var context = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(connectionString).Options, new Actor());
        var installed = context.Database.GetMigrations()
            .Where(id => string.CompareOrdinal(id, Previous) <= 0).ToArray();
        Assert.Contains(Previous, installed);
        await using (var history = new NpgsqlCommand("""
            INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
            SELECT id, '9.0.3' FROM unnest(@ids) id
            """, database))
        {
            history.Parameters.AddWithValue("ids", installed);
            await history.ExecuteNonQueryAsync();
        }

        var migrator = context.GetService<IMigrator>();
        // DigitalOcean executes the generated idempotent SQL; the physical customer's local
        // migrator applies the same migration directly. Both must retain capture and retry safely.
        var script = idempotentSql
            ? migrator.GenerateScript("0", Target, MigrationsSqlGenerationOptions.Idempotent)
            : string.Empty;
        async Task Apply()
        {
            if (idempotentSql) await Execute(database, script);
            else await migrator.MigrateAsync(Target);
        }

        await Apply();
        Assert.Equal(2L, await Scalar<long>(database, "SELECT count(*) FROM \"ProductPrices\""));
        Assert.Equal(9.875001m, await Scalar<decimal>(database, "SELECT sum(\"Price\") FROM \"ProductPrices\""));
        await using (var prices = new NpgsqlCommand("SELECT \"CurrencyId\" FROM \"ProductPrices\" ORDER BY \"Id\"", database))
        await using (var reader = await prices.ExecuteReaderAsync())
        {
            var count = 0;
            while (await reader.ReadAsync())
            {
                if (hasBaseCurrency) Assert.Equal(BaseCurrency, reader.GetGuid(0));
                else Assert.True(reader.IsDBNull(0));
                count++;
            }
            Assert.Equal(2, count);
        }
        Assert.True(await Scalar<bool>(database, """
            SELECT indisvalid FROM pg_index WHERE indexrelid='"IX_ProductPrices_CurrencyId"'::regclass
            """));
        Assert.True(await Scalar<bool>(database, """
            SELECT convalidated FROM pg_constraint WHERE conrelid='"ProductPrices"'::regclass
              AND conname='FK_ProductPrices_Currencies_CurrencyId'
            """));
        Assert.Equal(4L, await PriceHistoryCount(database));
        Assert.Equal(2L, await Scalar<long>(database, """
            SELECT count(*) FROM "ReportHistoryVersions"
            WHERE "TableName"='ProductPrices' AND "Payload" ? 'CurrencyId'
            """));

        await Apply();
        Assert.Equal(4L, await PriceHistoryCount(database));
        Assert.Equal(installed.Length + 1L, await Scalar<long>(database, "SELECT count(*) FROM \"__EFMigrationsHistory\""));
        var invalidCurrency = await Assert.ThrowsAsync<PostgresException>(() => Execute(database, """
            UPDATE "ProductPrices" SET "CurrencyId"='ffffffff-ffff-ffff-ffff-ffffffffffff' WHERE "Id"=1
            """));
        Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, invalidCurrency.SqlState);
        Assert.Equal(4L, await PriceHistoryCount(database));

        await migrator.MigrateAsync(Previous);
        Assert.Equal(9.875001m, await Scalar<decimal>(database, "SELECT sum(\"Price\") FROM \"ProductPrices\""));
        Assert.Equal(4L, await PriceHistoryCount(database));
        Assert.Equal(0L, await Scalar<long>(database, """
            SELECT count(*) FROM information_schema.columns
            WHERE table_schema='public' AND table_name='ProductPrices' AND column_name='CurrencyId'
            """));
        Assert.Equal(installed.LongLength, await Scalar<long>(database, "SELECT count(*) FROM \"__EFMigrationsHistory\""));
    }

    private static string FixtureConnection()
    {
        var value = Environment.GetEnvironmentVariable("ORYX_LIST_PRICE_MIGRATION_TEST_CONNECTION")
            ?? throw new InvalidOperationException("Missing disposable migration fixture connection.");
        var parsed = new NpgsqlConnectionStringBuilder(value);
        if (parsed.Host is not ("127.0.0.1" or "localhost") || parsed.Database != "oryx_list_price_migration_test")
            throw new InvalidOperationException("Refusing migration tests outside the local disposable fixture database.");
        return value;
    }

    private static Task<long> PriceHistoryCount(NpgsqlConnection connection) => Scalar<long>(connection,
        "SELECT count(*) FROM \"ReportHistoryVersions\" WHERE \"TableName\"='ProductPrices'");

    private static async Task Execute(NpgsqlConnection connection, string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<T> Scalar<T>(NpgsqlConnection connection, string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        return await command.ExecuteScalarAsync() is T value ? value
            : throw new InvalidOperationException("Unexpected migration fixture result.");
    }
}
