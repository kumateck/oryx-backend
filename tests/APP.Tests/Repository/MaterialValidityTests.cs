using APP.Mapper;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using APP.Repository;
using APP.Tests.QcWorksheets;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.Warehouses;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace APP.Tests.Repository;

public class MaterialValidityTests
{
    private static ServiceProvider Mapping(QcWorksheetTestContext harness)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(harness.Db);
        services.AddSingleton<IHttpContextAccessor, NullHttpContextAccessor>();
        services.AddAutoMapper(cfg => { }, typeof(OryxMapper));
        return services.BuildServiceProvider();
    }

    [Theory]
    [InlineData(30, 60, 30, false)]
    [InlineData(30, -5, 30, false)]
    [InlineData(-30, 60, 60, false)]
    [InlineData(-30, -5, -5, true)]
    [InlineData(-30, null, -30, true)]
    [InlineData(0, 60, 60, false)]
    [InlineData(null, 60, 60, false)]
    public void Expiry_has_priority_while_active_then_retest_extends_it(int? expiry, int? retest, int expected, bool expired)
    {
        var now = new DateTime(2026, 11, 3, 12, 0, 0, DateTimeKind.Utc);
        var batch = new MaterialBatch { Material = new Material { Kind = MaterialKind.Raw },
            ExpiryDate = expiry.HasValue ? now.AddDays(expiry.Value) : null,
            RetestDate = retest.HasValue ? now.AddDays(retest.Value) : null };
        Assert.Equal(now.AddDays(expected), MaterialValidity.EffectiveDate(batch, now));
        Assert.Equal(expired, MaterialValidity.IsExpired(batch, now));
        Assert.Equal(now.AddDays(expected), MaterialValidity.DateAt(now).Compile()(batch));
    }

    [Fact]
    public void Packaging_does_not_gain_a_raw_material_extension()
    {
        var now = DateTime.UtcNow;
        var batch = new MaterialBatch { Material = new Material { Kind = MaterialKind.Package },
            ExpiryDate = now.AddDays(-1), RetestDate = now.AddDays(30) };
        Assert.True(MaterialValidity.IsExpired(batch, now));
    }

    [Fact]
    public async Task Real_stock_queries_and_reservations_accept_extended_raw_stock_and_reject_elapsed_or_frozen_stock()
    {
        using var harness = new QcWorksheetTestContext();
        var batch = await harness.SeedMaterialBatch("MIGRATED-EXTENSION", BatchStatus.Available);
        batch.ExpiryDate = DateTime.UtcNow.AddDays(-30);
        batch.RetestDate = DateTime.UtcNow.AddDays(60);
        batch.TotalQuantity = 20;
        var warehouse = new Warehouse { Id = Guid.NewGuid(), Name = "Raw store" };
        var location = new WarehouseLocation { Id = Guid.NewGuid(), Warehouse = warehouse };
        var rack = new WarehouseLocationRack { Id = Guid.NewGuid(), WarehouseLocation = location };
        var shelf = new WarehouseLocationShelf { Id = Guid.NewGuid(), WarehouseLocationRack = rack };
        harness.Db.ShelfMaterialBatches.Add(new ShelfMaterialBatch { Id = Guid.NewGuid(), MaterialBatch = batch,
            WarehouseLocationShelf = shelf, UoMId = batch.UoMId, Quantity = 20 });
        await harness.Db.SaveChangesAsync();
        using var mapping = Mapping(harness);
        var repository = new MaterialRepository(harness.Db, mapping.GetRequiredService<IMapper>());
        Assert.Equal(20, (await repository.GetUsableShelfMaterialStockInWarehouse(batch.MaterialId, warehouse.Id)).Value);
        Assert.Equal(0, (await repository.GetShelfMaterialStockAndExpiredQuantityInWarehouse(batch.MaterialId, warehouse.Id)).Value.ExpiredQuantity);
        Assert.True((await repository.BatchesToSupplyForGivenQuantity(batch.MaterialId, warehouse.Id, 5)).IsSuccess);
        Assert.True((await repository.ReserveQuantityFromBatchForProduction(batch.Id, warehouse.Id, Guid.NewGuid(), 5, batch.UoMId, shelf.Id)).IsSuccess);
        batch.RetestDate = DateTime.UtcNow.AddDays(-1);
        await harness.Db.SaveChangesAsync();
        Assert.Equal(0, (await repository.GetUsableShelfMaterialStockInWarehouse(batch.MaterialId, warehouse.Id)).Value);
        Assert.False((await repository.BatchesToSupplyForGivenQuantity(batch.MaterialId, warehouse.Id, 5)).IsSuccess);
        Assert.False((await repository.ReserveQuantityFromBatchForProduction(batch.Id, warehouse.Id, Guid.NewGuid(), 5, batch.UoMId, shelf.Id)).IsSuccess);
        batch.RetestDate = DateTime.UtcNow.AddDays(60);
        batch.Status = BatchStatus.Frozen;
        await harness.Db.SaveChangesAsync();
        Assert.False((await repository.ReserveQuantityFromBatchForProduction(batch.Id, warehouse.Id, Guid.NewGuid(), 5, batch.UoMId, shelf.Id)).IsSuccess);
    }

    [Fact]
    public async Task Expiry_lists_search_before_pagination_and_combine_optional_dates_with_effective_validity()
    {
        using var harness = new QcWorksheetTestContext();
        var expired = await harness.SeedMaterialBatch("EXPIRED", BatchStatus.Available);
        expired.ExpiryDate = DateTime.UtcNow.AddDays(-90);
        expired.Material.Code = "RMC022";
        expired.Material.Name = "Lactose";
        var extended = await harness.SeedMaterialBatch("EXTENDED", BatchStatus.Available);
        extended.ExpiryDate = DateTime.UtcNow.AddDays(-90);
        extended.RetestDate = DateTime.UtcNow.AddDays(30);
        extended.Material.Code = "RMC023";
        extended.Material.Name = "Extended starch";
        await harness.Db.SaveChangesAsync();
        using var mapping = Mapping(harness);
        var repository = new MaterialRepository(harness.Db, mapping.GetRequiredService<IMapper>());
        var found = await repository.GetExpiredMaterialBatches(new MaterialFilter { SearchQuery = "rmc022", PageSize = 1 });
        Assert.Equal(1, found.Value.TotalRecordCount);
        Assert.Equal(expired.Id, Assert.Single(found.Value.Data).Id);
        var byName = await repository.GetExpiredMaterialBatches(new MaterialFilter { SearchQuery = "LACTOSE" });
        Assert.Equal(1, byName.Value.TotalRecordCount);
        var filtered = await repository.GetExpiredMaterialBatches(new MaterialFilter { SearchQuery = "RMC022", StartDate = DateTime.UtcNow.AddDays(-5) });
        Assert.Equal(0, filtered.Value.TotalRecordCount);
        var extensionNotExpired = await repository.GetExpiredMaterialBatches(new MaterialFilter { SearchQuery = "RMC023" });
        Assert.Empty(extensionNotExpired.Value.Data);
        var soon = await repository.GetAboutToExpireMaterialBatches(new MaterialFilter { SearchQuery = "Extended" });
        Assert.Equal(extended.Id, Assert.Single(soon.Value.Data).Id);
    }

    [Fact]
    public void Effective_date_query_is_translated_to_postgres_without_client_evaluation()
    {
        using var db = new INFRASTRUCTURE.Context.ApplicationDbContext(new DbContextOptionsBuilder<INFRASTRUCTURE.Context.ApplicationDbContext>()
            .UseNpgsql("Host=localhost;Database=unused").Options, new NoCurrentUser());
        var sql = db.ShelfMaterialBatches.WhereMaterialDate(s => s.MaterialBatch, date => date > DateTime.UtcNow).ToQueryString();
        Assert.Contains("CASE", sql);
        Assert.Contains("RetestDate", sql);
        Assert.Contains("ExpiryDate", sql);
    }
}
