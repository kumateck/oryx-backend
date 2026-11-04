using APP.Mapper;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using APP.Repository;
using APP.Tests.QcWorksheets;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Departments;
using DOMAIN.Entities.Users;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.Warehouses;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace APP.Tests.Repository;

public class MaterialValidityTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Expiry_reports_name_only_departments_holding_positive_stock_without_duplicates(bool soon)
    {
        using var harness = new QcWorksheetTestContext();
        var batch = await harness.SeedMaterialBatch("DEPARTMENT-REPORT", BatchStatus.Available);
        batch.Material.Code = "DEPT-MATERIAL";
        batch.ExpiryDate = DateTime.UtcNow.AddDays(soon ? 30 : -30);
        var solids = new Department { Id = Guid.NewGuid(), Name = "Oral solids", Code = "SOL" };
        var liquids = new Department { Id = Guid.NewGuid(), Name = "Liquids", Code = "LIQ" };
        var unused = new Department { Id = Guid.NewGuid(), Name = "No stock" };
        var deleted = new Department { Id = Guid.NewGuid(), Name = "Deleted department", DeletedAt = DateTime.UtcNow };
        void AddShelf(Department? department, decimal quantity, bool removed = false)
        {
            var warehouse = new Warehouse { Id = Guid.NewGuid(), Name = "Stock store", Department = department, DepartmentId = department?.Id };
            var location = new WarehouseLocation { Id = Guid.NewGuid(), Warehouse = warehouse };
            var rack = new WarehouseLocationRack { Id = Guid.NewGuid(), WarehouseLocation = location };
            var shelf = new WarehouseLocationShelf { Id = Guid.NewGuid(), WarehouseLocationRack = rack };
            harness.Db.ShelfMaterialBatches.Add(new ShelfMaterialBatch { Id = Guid.NewGuid(), MaterialBatch = batch,
                WarehouseLocationShelf = shelf, Quantity = quantity, DeletedAt = removed ? DateTime.UtcNow : null });
        }
        AddShelf(solids, 2);
        AddShelf(solids, 3);
        AddShelf(unused, 0);
        AddShelf(unused, 1, removed: true);
        AddShelf(deleted, 1);
        AddShelf(null, 1);
        harness.Db.MaterialBatchReservedQuantities.Add(new MaterialBatchReservedQuantity { Id = Guid.NewGuid(), MaterialBatch = batch,
            Warehouse = new Warehouse { Id = Guid.NewGuid(), Name = "Production store", Department = liquids }, Quantity = 4 });
        harness.Db.MaterialBatchReservedQuantities.Add(new MaterialBatchReservedQuantity { Id = Guid.NewGuid(), MaterialBatch = batch,
            Warehouse = new Warehouse { Id = Guid.NewGuid(), Name = "Empty production store", Department = unused }, Quantity = 0 });
        StockReportTestPositions.MarkIssued(harness);
        await harness.Db.SaveChangesAsync();
        harness.Db.ChangeTracker.Clear(); // Department navigation must come from the report query, not tracked seed objects.
        using var mapping = Mapping(harness);
        var repository = new MaterialRepository(harness.Db, mapping.GetRequiredService<IMapper>());
        var filter = new MaterialFilter { SearchQuery = "DEPT-MATERIAL", PageSize = 1 };
        var actor = new User { Id = Guid.NewGuid(), UserName = "expiry-global-reader" };
        harness.Db.Users.Add(actor);
        await harness.Db.SaveChangesAsync();
        var result = soon ? await repository.GetAboutToExpireMaterialBatches(filter, actor.Id) : await repository.GetExpiredMaterialBatches(filter, actor.Id);
        Assert.Equal(1, result.Value.TotalRecordCount);
        var row = Assert.Single(result.Value.Data);
        Assert.Equal(new[] { "Liquids", "Oral solids" }, row.StockDepartments.Select(department => department.Name));
        Assert.Equal(new Guid?[] { liquids.Id, solids.Id }, row.StockDepartments.Select(department => department.Id));
        Assert.Equal(new[] { "LIQ", "SOL" }, row.StockDepartments.Select(department => department.Code));
    }

    internal static ServiceProvider Mapping(QcWorksheetTestContext harness)
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
    public void Undated_legacy_batches_are_neither_expired_nor_expiring_soon()
    {
        foreach (DateTime? date in new DateTime?[] { null, DateTime.MinValue })
        {
            var dto = new MaterialBatchDto { EffectiveExpiryDate = date };
            Assert.False(dto.Expired);
            Assert.False(dto.AboutToExpire);
        }
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
        var actor = new User { Id = Guid.NewGuid(), UserName = "expiry-reader" };
        harness.Db.Users.Add(actor);
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
        var found = await repository.GetExpiredMaterialBatches(new MaterialFilter { SearchQuery = "rmc022", PageSize = 1 }, actor.Id);
        Assert.Equal(1, found.Value.TotalRecordCount);
        Assert.Equal(expired.Id, Assert.Single(found.Value.Data).Id);
        var byName = await repository.GetExpiredMaterialBatches(new MaterialFilter { SearchQuery = "LACTOSE" }, actor.Id);
        Assert.Equal(1, byName.Value.TotalRecordCount);
        var filtered = await repository.GetExpiredMaterialBatches(new MaterialFilter { SearchQuery = "RMC022", StartDate = DateTime.UtcNow.AddDays(-5) }, actor.Id);
        Assert.Equal(0, filtered.Value.TotalRecordCount);
        var extensionNotExpired = await repository.GetExpiredMaterialBatches(new MaterialFilter { SearchQuery = "RMC023" }, actor.Id);
        Assert.Empty(extensionNotExpired.Value.Data);
        var soon = await repository.GetAboutToExpireMaterialBatches(new MaterialFilter { SearchQuery = "Extended" }, actor.Id);
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
