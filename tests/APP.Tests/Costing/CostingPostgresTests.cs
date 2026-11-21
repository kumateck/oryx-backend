using APP.Services.Costing;
using APP.Tests.ApprovedVendorLists;
using DOMAIN.Entities.Costing;
using DOMAIN.Entities.Products;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Xunit;

namespace APP.Tests.Costing;

public sealed class CostingPostgresFactAttribute : FactAttribute
{
    public CostingPostgresFactAttribute() { if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ORYX_COSTING_TEST_CONNECTION"))) Skip="Requires a local PostgreSQL role allowed to create disposable databases."; }
}
public class CostingPostgresTests
{
    [CostingPostgresFact]
    public async Task Migration_enforces_one_open_revision_unique_pack_lines_and_optimistic_writes_and_executes_narrow_queries()
    {
        var adminString=Environment.GetEnvironmentVariable("ORYX_COSTING_TEST_CONNECTION")!;
        var name="oryx_costing_"+Guid.NewGuid().ToString("N");
        await using var admin=new NpgsqlConnection(adminString); await admin.OpenAsync();
        await new NpgsqlCommand($"CREATE DATABASE {name}",admin).ExecuteNonQueryAsync();
        try {
            var connection=new NpgsqlConnectionStringBuilder(adminString) { Database=name,Pooling=false }.ConnectionString;
            var options=new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(connection).Options;
            await using var db=new ApplicationDbContext(options,new AvlActor());
            await db.Database.ExecuteSqlRawAsync("""
                CREATE TABLE users ("Id" uuid PRIMARY KEY);
                CREATE TABLE roles ("Id" uuid PRIMARY KEY);
                CREATE TABLE "Approvals" ("Id" uuid PRIMARY KEY);
                CREATE TABLE "ProformaInvoiceProducts" ("Id" uuid PRIMARY KEY);
                CREATE TABLE "ProductionOrderProducts" ("Id" uuid PRIMARY KEY);
                CREATE TABLE "CustomerQuotationItems" ("Id" uuid PRIMARY KEY);
                CREATE TABLE "Products" ("Id" uuid PRIMARY KEY,"Division" integer NOT NULL,"DeletedAt" timestamptz);
                CREATE TABLE "__EFMigrationsHistory" ("MigrationId" varchar(150) PRIMARY KEY,"ProductVersion" varchar(32) NOT NULL);
                """);
            var migrations=db.Database.GetMigrations().ToList(); var target=migrations.Single(id=>id.EndsWith("_AddVersionedCostingPriceLists"));
            var index=migrations.IndexOf(target);
            var sql=db.GetService<IMigrator>().GenerateScript(migrations[index-1],target);
            await db.Database.ExecuteSqlRawAsync(sql);
            var id=Guid.NewGuid();var product=Guid.NewGuid();var packing=Guid.NewGuid();var currency=Guid.NewGuid();var date=DateTime.UtcNow.Date;
            var list=new CostingPriceList { Id=id,DocumentNumber="FIN/PL",RevisionReason="First",Division=(Division)0,RevisionNumber=0,
                EffectiveFrom=date,Status=CostingPriceListStatus.Draft,Version=1,
                Lines=[new() { Id=Guid.NewGuid(),ProductId=product,ProductPackingId=packing,ProductName="Snapshot",PackingStyle="24 bottles",Price=123m,CurrencyId=currency }] };
            db.CostingPriceLists.Add(list); await db.SaveChangesAsync();
            await using var second=new ApplicationDbContext(options,new AvlActor());
            second.CostingPriceLists.Add(new() { Id=Guid.NewGuid(),Division=(Division)0,RevisionNumber=1,EffectiveFrom=date,Status=CostingPriceListStatus.Draft });
            var duplicate=await Assert.ThrowsAsync<DbUpdateException>(()=>second.SaveChangesAsync());
            Assert.Equal("23505",Assert.IsType<PostgresException>(duplicate.InnerException).SqlState); second.ChangeTracker.Clear();
            var stale=await second.CostingPriceLists.SingleAsync(item=>item.Id==id);
            list.Version++;list.RevisionReason="Changed";await db.SaveChangesAsync();
            stale.Version++;stale.RevisionReason="Stale";
            await Assert.ThrowsAsync<DbUpdateConcurrencyException>(()=>second.SaveChangesAsync());second.ChangeTracker.Clear();
            second.CostingPriceListLines.Add(new() { Id=Guid.NewGuid(),CostingPriceListId=id,ProductId=product,ProductPackingId=packing });
            var duplicateLine=await Assert.ThrowsAsync<DbUpdateException>(()=>second.SaveChangesAsync());
            Assert.Equal("23505",Assert.IsType<PostgresException>(duplicateLine.InnerException).SqlState);
            list.Status=CostingPriceListStatus.Approved;list.ApprovedAt=date;await db.SaveChangesAsync();
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO \"Products\" (\"Id\",\"Division\") VALUES ({product},0)");
            Assert.Equal(123m,(await EffectiveCostingPrice.Resolve(db,product,packing,date)).Price);
            var lines=new CostingPriceListService(db,null!).LineQuery(id,"Snapshot");
            var query=lines.Take(30).ToQueryString();
            Assert.DoesNotContain("Warehouse",query);Assert.DoesNotContain("ProductPrices",query);
            Assert.Equal("24 bottles",Assert.Single(await lines.ToListAsync()).PackingStyle);
        } finally { await new NpgsqlCommand($"DROP DATABASE {name} WITH (FORCE)",admin).ExecuteNonQueryAsync(); }
    }
}
