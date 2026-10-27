using System.Data;
using DOMAIN.Entities.Items;
using DOMAIN.Entities.Employees;
using DOMAIN.Entities.Warehouses;
using DOMAIN.Entities.Materials.Batch;
using INFRASTRUCTURE.Context;
using INFRASTRUCTURE.Reporting;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Reporting;

public sealed class LocalReportHistoryFactAttribute : FactAttribute
{
    public LocalReportHistoryFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ORYX_REPORT_HISTORY_TEST_CONNECTION")))
            Skip = "Set the explicitly local disposable report-history PostgreSQL fixture connection.";
    }
}

public class ReportHistoryPostgresTests
{
    private sealed class User : ICurrentUserService
    { public Guid? UserId => null; public Guid? DepartmentId => null; public string DepartmentType => "NonProduction"; }
    private static string Connection()
    {
        var value = Environment.GetEnvironmentVariable("ORYX_REPORT_HISTORY_TEST_CONNECTION") ?? throw new InvalidOperationException("Missing local fixture.");
        var parsed = new NpgsqlConnectionStringBuilder(value);
        if (parsed.Host is not ("127.0.0.1" or "localhost") || !parsed.Database.StartsWith("oryx_report_history_test", StringComparison.Ordinal))
            throw new InvalidOperationException("These tests refuse non-loopback or non-fixture databases.");
        return value;
    }
    private static async Task Execute(NpgsqlConnection connection, string sql)
    { await using var command = new NpgsqlCommand(sql, connection); await command.ExecuteNonQueryAsync(); }
    private static async Task<DateTime> Clock(NpgsqlConnection connection)
    { await using var command = new NpgsqlCommand("SELECT clock_timestamp()", connection); return (await command.ExecuteScalarAsync()) is DateTime value ? value : throw new InvalidOperationException("Missing database clock."); }
    private static ApplicationDbContext Context(string connection) => new(new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(connection).Options, new User());

    [Fact]
    public void Dates_are_UTC_bounded_and_never_claim_a_future_day()
    {
        var now = new DateTime(2026, 10, 27, 12, 0, 0, DateTimeKind.Utc);
        Assert.Equal(now, ReportHistoryScope.Cutoff(new DateTimeOffset(now.Date.AddDays(1).AddTicks(-1)), now));
        Assert.Throws<ArgumentException>(() => ReportHistoryScope.Cutoff(new DateTimeOffset(now.AddDays(1)), now));
        Assert.Equal(now.AddDays(-1), ReportHistoryScope.Cutoff(new DateTimeOffset(now.AddDays(-1)), now));
    }

    [LocalReportHistoryFact]
    public async Task Temporal_views_replay_committed_stock_and_names_then_restore_current_queries()
    {
        var connection = Connection();
        await using var database = new NpgsqlConnection(connection); await database.OpenAsync();
        await Execute(database, "DROP SCHEMA public CASCADE; CREATE SCHEMA public;");
        await Execute(database, """
            CREATE TABLE public."Departments" ("Id" uuid PRIMARY KEY,"Name" text,"DeletedAt" timestamptz);
            CREATE TABLE public."Items" ("Id" uuid PRIMARY KEY,"Name" text,"Code" text,"AvailableQuantity" integer,"DeletedAt" timestamptz);
            CREATE TABLE public."Employees" ("Id" uuid PRIMARY KEY,"DepartmentId" uuid REFERENCES public."Departments"("Id"),"Level" integer,"Type" integer,"Gender" integer,"DeletedAt" timestamptz);
            CREATE TABLE public."users" ("Id" uuid PRIMARY KEY,"PasswordHash" text,"SecurityStamp" text,"FirstName" text);
            INSERT INTO public."Departments" VALUES('00000000-0000-0000-0000-000000000001','Original department',NULL);
            INSERT INTO public."Employees" VALUES('00000000-0000-0000-0000-000000000004','00000000-0000-0000-0000-000000000001',0,1,0,NULL);
            INSERT INTO public."Items" VALUES('00000000-0000-0000-0000-000000000002','Item','ITEM-1',10,NULL);
            INSERT INTO public."users" VALUES('00000000-0000-0000-0000-000000000003','fixture-hash','fixture-stamp','Original');
            """);
        await using (var setup = await database.BeginTransactionAsync())
        { await Execute(database, ReportHistorySchema.InstallSql); await setup.CommitAsync(); }
        var first = await Clock(database);
        await Execute(database, """
            BEGIN;
            UPDATE public."Items" SET "AvailableQuantity"=25;
            UPDATE public."Employees" SET "Level"=2;
            UPDATE public."Departments" SET "Name"='Renamed department';
            COMMIT;
            """);
        await using var context = Context(connection);
        Assert.Equal(25, await context.Items.Select(item => item.AvailableQuantity).SingleAsync());
        await using (await ReportHistoryScope.Open(context, first))
        {
            Assert.Equal(EmployeeLevel.JuniorStaff, await context.Employees.Select(employee=>employee.Level).SingleAsync());
            Assert.Equal("Original department", await context.Employees.Select(employee=>employee.Department.Name).SingleAsync());
            Assert.Equal(10, await context.Items.Select(item => item.AvailableQuantity).SingleAsync());
            var grades = await new APP.Repository.ReportRepository(context,null!,null!,Microsoft.Extensions.Logging.Abstractions.NullLogger<APP.Repository.ReportRepository>.Instance).GetPermanentStaffGradeReport(null);
            Assert.Equal(1, grades.Value.Totals.JuniorStaffMale);
            Assert.Equal(0, grades.Value.Totals.SeniorMgtMale);
            Assert.Equal("Original department", await context.Departments.Select(item => item.Name).SingleAsync());
            context.Items.Add(new Item { Id = Guid.NewGuid() });
            await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync());
            context.ChangeTracker.Clear();
            await Assert.ThrowsAsync<ReportHistoryUnavailableException>(() => context.Database.ExecuteSqlRawAsync("UPDATE \"Items\" SET \"AvailableQuantity\"=99"));
        }
        Assert.Equal(25, await context.Items.Select(item => item.AvailableQuantity).SingleAsync());
        Assert.Equal("Renamed department", await context.Departments.Select(item => item.Name).SingleAsync());
        Assert.Equal(EmployeeLevel.SeniorManagement, await context.Employees.Select(employee=>employee.Level).SingleAsync());
        Assert.Null(context.ReportAsOfUtc);
        await Execute(database, "BEGIN; UPDATE public.\"Items\" SET \"AvailableQuantity\"=99; ROLLBACK;");
        await using (await ReportHistoryScope.Open(context, await Clock(database))) Assert.Equal(25, await context.Items.Select(item=>item.AvailableQuantity).SingleAsync());
        await using var secrets = new NpgsqlCommand("SELECT count(*) FROM public.\"ReportHistoryVersions\" WHERE \"TableName\"='users' AND (\"Payload\" ? 'PasswordHash' OR \"Payload\" ? 'SecurityStamp')", database);
        Assert.Equal(0L, await secrets.ExecuteScalarAsync());
        await Assert.ThrowsAsync<PostgresException>(() => Execute(database, "UPDATE public.\"ReportHistoryVersions\" SET \"Payload\"='{}'"));
        await Assert.ThrowsAsync<PostgresException>(() => Execute(database, "TRUNCATE public.\"ReportHistoryVersions\""));
        await Assert.ThrowsAsync<PostgresException>(() => Execute(database, "TRUNCATE public.\"Items\""));
        await Assert.ThrowsAsync<ReportHistoryUnavailableException>(async () => { await using var ignored = await ReportHistoryScope.Open(context, first.AddYears(-1)); });
        var beforeDelete = await Clock(database);
        await Execute(database, "DELETE FROM public.\"Items\"");
        await using (await ReportHistoryScope.Open(context, beforeDelete)) Assert.Equal(25, await context.Items.Select(item => item.AvailableQuantity).SingleAsync());
        await using (await ReportHistoryScope.Open(context, await Clock(database))) Assert.Empty(await context.Items.Select(item => item.AvailableQuantity).ToListAsync());
        await Execute(database, ReportHistorySchema.RemoveSql);
    }
    [LocalReportHistoryFact]
    public async Task Full_model_report_dependencies_are_captured_and_queries_remain_read_only()
    {
        var connection = Connection();
        await using var database = new NpgsqlConnection(connection); await database.OpenAsync();
        await Execute(database, "DROP SCHEMA public CASCADE; CREATE SCHEMA public;");
        await using var context = Context(connection);
        await context.Database.EnsureCreatedAsync();
        var unit = new DOMAIN.Entities.Base.UnitOfMeasure { Id=Guid.NewGuid(),Name="Pieces",Symbol="pcs" };
        var item = new Item { Id=Guid.NewGuid(),Name="Fixture item",Code="FIX-1",UnitOfMeasure=unit,Store=Store.ItStore,AvailableQuantity=10,MinimumLevel=2,MaximumLevel=100,ReorderLevel=5,IsActive=true };
        context.Items.Add(item);
        var material = new DOMAIN.Entities.Materials.Material { Id=Guid.NewGuid(),Name="Raw fixture",Code="RAW-FIX",Kind=DOMAIN.Entities.Materials.MaterialKind.Raw };
        var department = new DOMAIN.Entities.Departments.Department {Id=Guid.NewGuid(),Name="Warehouse owner"};
        var shelf = new WarehouseLocationShelf {Id=Guid.NewGuid(),Name="Shelf",WarehouseLocationRack=new WarehouseLocationRack {Id=Guid.NewGuid(),Name="Rack",WarehouseLocation=new WarehouseLocation {Id=Guid.NewGuid(),Name="Location",Warehouse=new Warehouse {Id=Guid.NewGuid(),Name="Raw warehouse",Type=WarehouseType.RawMaterialStorage,Department=department}}}};
        context.ShelfMaterialBatches.Add(new ShelfMaterialBatch {Id=Guid.NewGuid(),WarehouseLocationShelf=shelf,Quantity=10,UoM=unit,MaterialBatch=new MaterialBatch {Id=Guid.NewGuid(),Material=material,UoM=unit,BatchNumber="HISTORY-1",TotalQuantity=10}});
        context.Products.Add(new DOMAIN.Entities.Products.Product { Id=Guid.NewGuid(),Name="Product fixture",Code="PROD-FIX",Prices=[new DOMAIN.Entities.Products.ProductPrices {Price=10,PriceUoM="box",Date=DateTime.UtcNow}] });
        await context.SaveChangesAsync(); context.ChangeTracker.Clear();
        await using (var setup = await database.BeginTransactionAsync())
        { await Execute(database, ReportHistorySchema.InstallSql); await setup.CommitAsync(); }
        var captured = await Clock(database);
        await Execute(database, "BEGIN; UPDATE public.\"Items\" SET \"AvailableQuantity\"=25; UPDATE public.\"ShelfMaterialBatches\" SET \"Quantity\"=25; UPDATE public.\"ProductPrices\" SET \"Price\"=20; COMMIT;");
        await using (await ReportHistoryScope.Open(context, captured))
        {
            // Exercise collection/owned joins even with no batch rows: missing history must fail closed.
            Assert.Single(await context.Materials.AsSplitQuery().ToListAsync());
            Assert.Equal(10, Assert.Single(await context.Products.IgnoreQueryFilters().AsSplitQuery().ToListAsync()).Price);
            var reports = new APP.Repository.ReportRepository(context, null!, null!, Microsoft.Extensions.Logging.Abstractions.NullLogger<APP.Repository.ReportRepository>.Instance);
            Assert.True((await reports.GetPermanentStaffGradeReport(null)).IsSuccess);
            Assert.Equal(10, Assert.Single((await reports.GetMaterialsStockSummary()).Value).TotalQuantity);
            Assert.Equal(10, Assert.Single((await reports.GetMaterialsStockBatchDetail()).Value).QuantityAvailable);
            Assert.True((await reports.GetInventoryValuationDetail()).IsSuccess);
            Assert.True((await reports.GetProductStockSummaryReport()).IsSuccess);
            Assert.True((await reports.GetProductStockDetailedReport()).IsSuccess);
            var stock = await reports.GetStockSummaryPerStoreType();
            Assert.Equal(10, Assert.Single(stock.Value).TotalQuantity);
            var master = await reports.GetItemsPerStoreType(Store.ItStore,null,null,null);
            var row = Assert.Single(master.Value);
            Assert.Equal("FIX-1",row.Code); Assert.Equal(10,row.AvailableQuantity); Assert.Equal("pcs",row.UnitOfMeasure.Symbol);
            Assert.Equal(2,row.MinimumLevel); Assert.Equal(100,row.MaximumLevel); Assert.Equal(5,row.ReorderLevel); Assert.True(row.IsActive);
            Assert.True((await reports.GetVendorItemMapping(null,null,null,null,null)).IsSuccess);
            Assert.True((await reports.GetVendorItemMappingPerStoreTypeSummary(null,null,null,null)).IsSuccess);
            // Explicit qualification must not bypass the connection-local historical views.
            await Assert.ThrowsAsync<ReportHistoryUnavailableException>(() => context.Database.SqlQueryRaw<int>("SELECT count(*)::integer AS \"Value\" FROM \"public\".\"Items\"").ToListAsync());
            // New unversioned report joins must fail instead of mixing historical and current facts.
            await Assert.ThrowsAsync<ReportHistoryUnavailableException>(() => context.Roles.Select(role=>role.Name).ToListAsync());
        }
        var current = new APP.Repository.ReportRepository(context,null!,null!,Microsoft.Extensions.Logging.Abstractions.NullLogger<APP.Repository.ReportRepository>.Instance);
        Assert.Equal(25,Assert.Single((await current.GetMaterialsStockSummary()).Value).TotalQuantity);
        Assert.Equal(20,Assert.Single(await context.Products.IgnoreQueryFilters().AsSplitQuery().ToListAsync()).Price);
        await Execute(database, ReportHistorySchema.RemoveSql);
    }

}
