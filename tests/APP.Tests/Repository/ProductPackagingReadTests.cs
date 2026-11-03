using System.Text.Json;
using APP.Repository;
using AutoMapper;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Departments;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Products;
using DOMAIN.Entities.Products.Production;
using DOMAIN.Entities.ProductionSchedules;
using DOMAIN.Entities.Users;
using DOMAIN.Entities.Warehouses;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using SHARED;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Repository;

public class ProductPackagingReadTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Product_details_match_the_eight_active_edit_rows_even_after_history_was_tracked(bool clearTracker)
    {
        await using var fixture = await Fixture.Create();
        if (clearTracker) fixture.Context.ChangeTracker.Clear();
        var repository = new ProductRepository(fixture.Context, fixture.Mapper);
        var edit = await repository.GetProductPackages(fixture.ProductId);
        var detail = await repository.GetProduct(fixture.ProductId);

        Assert.True(edit.IsSuccess);
        Assert.True(detail.IsSuccess);
        Assert.Equal(8, edit.Value.Count());
        Assert.Equal(edit.Value.Select(item => item.Id).Order(), detail.Value.Packages.Select(item => item.Id).Order());
        Assert.Equal(8, detail.Value.Packages.Count);
        foreach (var item in detail.Value.Packages)
            Assert.Equal(fixture.Active.Single(active => active.Id == item.Id).ProductPackingId, item.ProductPacking?.Id);
        Assert.Equal(14, await fixture.Context.ProductPackages.IgnoreQueryFilters().CountAsync());
        Assert.Equal(6, await fixture.Context.ProductPackages.IgnoreQueryFilters().CountAsync(item => item.DeletedAt != null));
    }

    [Theory]
    [InlineData(BatchSize.Full)]
    [InlineData(BatchSize.Half)]
    public async Task Legacy_schedule_uses_only_active_selected_style_and_all_style_rows(BatchSize size)
    {
        await using var fixture = await Fixture.Create(size);
        // A valid row for another packing style must remain on the product but not on this batch.
        var otherStyle = new ProductPacking { Id = Guid.NewGuid(), ProductId = fixture.ProductId, Name = "Other style" };
        fixture.Context.ProductPackings.Add(otherStyle);
        fixture.Context.ProductPackages.Add(new ProductPackage { ProductId = fixture.ProductId, MaterialId = fixture.Active[0].MaterialId,
            ProductPackingId = otherStyle.Id, PrescribedQuantity = 9999, BaseQuantity = 1 });
        await fixture.Context.SaveChangesAsync();

        var result = await fixture.Schedules().CheckPackageMaterialStockLevelsForProductionSchedule(fixture.BatchId, null);
        Assert.True(result.IsSuccess);
        Assert.Equal(8, result.Value.Count);
        Assert.Equal(fixture.Active.Select(item => item.MaterialId).Order(), result.Value.Select(item => item.Material.Id).Order());
        foreach (var package in fixture.Active)
        {
            var row = Assert.Single(result.Value, item => item.Material.Id == package.MaterialId);
            Assert.Equal((size == BatchSize.Full ? package.PrescribedQuantity : package.PrescribedQuantity / 2) + package.Loose, row.QuantityNeeded);
            Assert.Equal(package.PrescribedQuantity, row.PrescribedQuantity);
        }
        var unavailable = await fixture.Schedules().CheckPackageMaterialStockLevelsForProductionSchedule(fixture.BatchId, MaterialRequisitionStatus.None);
        Assert.Equal(8, unavailable.Value.Count);
        var inHouse = await fixture.Schedules().CheckPackageMaterialStockLevelsForProductionSchedule(fixture.BatchId, MaterialRequisitionStatus.InHouse);
        Assert.Empty(inHouse.Value);
        Assert.Equal(15, await fixture.Context.ProductPackages.IgnoreQueryFilters().CountAsync());
    }

    [Fact]
    public async Task An_applied_batch_keeps_its_adopted_packaging_when_the_product_master_changes()
    {
        await using var fixture = await Fixture.Create();
        var historical = await fixture.Context.ProductPackages.IgnoreQueryFilters().FirstAsync(item => item.DeletedAt != null);
        var requirements = new[] { new ProductionMaterialRequirement { MaterialId = historical.MaterialId, SourceMaterialId = historical.MaterialId,
            UnitId = fixture.UnitId, Unit = "piece", BaseUnitId = fixture.UnitId, BaseUnit = "piece", Quantity = 123.456789m, PrescribedQuantity = 246.5m } };
        var json = JsonSerializer.Serialize(requirements);
        fixture.Context.ProductionMaterialConfigurations.Add(new ProductionMaterialConfiguration { ProductionScheduleProductId = fixture.BatchId,
            Kind = MaterialKind.Package, DepartmentId = fixture.DepartmentId, StorageWarehouseId = fixture.StorageId,
            ProductionWarehouseId = fixture.FloorId, Revision = 2, RequirementsJson = json });
        await fixture.Context.SaveChangesAsync();

        var result = await fixture.Schedules().CheckPackageMaterialStockLevelsForProductionSchedule(fixture.BatchId, null);
        Assert.True(result.IsSuccess);
        var row = Assert.Single(result.Value);
        Assert.Equal(historical.MaterialId, row.Material.Id);
        Assert.Equal(123.456789m, row.QuantityNeeded);
        Assert.Equal(json, (await fixture.Context.ProductionMaterialConfigurations.SingleAsync()).RequirementsJson);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task A_duplicate_in_a_replacement_request_is_rejected_before_any_rows_are_removed(bool allStyles)
    {
        await using var fixture = await Fixture.Create();
        var active = fixture.Active[0];
        Guid? style = allStyles ? null : active.ProductPackingId;
        var result = await new ProductRepository(fixture.Context, fixture.Mapper).CreateProductPackage([
            new() { MaterialId = active.MaterialId, ProductPackingId = style, PrescribedQuantity = 5 },
            new() { MaterialId = active.MaterialId, ProductPackingId = style, PrescribedQuantity = 10 },
        ], fixture.ProductId, fixture.ActorId);

        Assert.True(result.IsFailure);
        Assert.Equal("Product.Package.Duplicate", result.Error.Code);
        Assert.Equal(14, await fixture.Context.ProductPackages.IgnoreQueryFilters().CountAsync());
        Assert.Equal(8, await fixture.Context.ProductPackages.CountAsync());
        Assert.Equal(active.PrescribedQuantity, (await fixture.Context.ProductPackages.SingleAsync(item => item.Id == active.Id)).PrescribedQuantity);
    }

    [Fact]
    public async Task The_same_material_can_be_saved_for_two_styles_and_All_styles()
    {
        await using var fixture = await Fixture.Create();
        var product = new Product { Id = Guid.NewGuid(), Name = "Style variants", DepartmentId = fixture.DepartmentId, BaseUomId = fixture.UnitId };
        var first = new ProductPacking { Id = Guid.NewGuid(), ProductId = product.Id, Name = "First" };
        var second = new ProductPacking { Id = Guid.NewGuid(), ProductId = product.Id, Name = "Second" };
        fixture.Context.AddRange(product, first, second);
        await fixture.Context.SaveChangesAsync();
        var materialId = fixture.Active[0].MaterialId;
        var repository = new ProductRepository(fixture.Context, fixture.Mapper);
        var result = await repository.CreateProductPackage([
            new() { MaterialId = materialId, ProductPackingId = first.Id, PrescribedQuantity = 5 },
            new() { MaterialId = materialId, ProductPackingId = second.Id, PrescribedQuantity = 10 },
            new() { MaterialId = materialId, ProductPackingId = null, PrescribedQuantity = 15 },
        ], product.Id, fixture.ActorId);

        Assert.True(result.IsSuccess);
        var saved = await repository.GetProductPackages(product.Id);
        Assert.Equal(3, saved.Value.Count());
        Assert.Equal(new Guid?[] { null, first.Id, second.Id }.Order(), saved.Value.Select(item => item.ProductPacking?.Id).Order());
        var detail = await repository.GetProduct(product.Id);
        Assert.Equal(3, detail.Value.Packages.Count);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Editing_a_row_cannot_duplicate_another_active_pair_but_can_keep_its_own_pair(bool allStyles)
    {
        await using var fixture = await Fixture.Create();
        var first = fixture.Active[0];
        var other = fixture.Active[allStyles ? 4 : 1];
        var repository = new ProductRepository(fixture.Context, fixture.Mapper);
        var rejected = await repository.UpdateProductPackage(new() { MaterialId = other.MaterialId,
            ProductPackingId = other.ProductPackingId, PrescribedQuantity = 999 }, first.Id, fixture.ActorId);
        Assert.True(rejected.IsFailure);
        Assert.Equal("Product.Package.Duplicate", rejected.Error.Code);
        Assert.Equal(first.MaterialId, (await fixture.Context.ProductPackages.SingleAsync(item => item.Id == first.Id)).MaterialId);

        // The same pair also exists in deleted history; neither it nor the edited row is a conflict.
        var kept = await repository.UpdateProductPackage(new() { MaterialId = first.MaterialId,
            ProductPackingId = first.ProductPackingId, PrescribedQuantity = 7 }, first.Id, fixture.ActorId);
        Assert.True(kept.IsSuccess);
        Assert.Equal(7m, (await fixture.Context.ProductPackages.SingleAsync(item => item.Id == first.Id)).PrescribedQuantity);
    }

    [Theory]
    [InlineData(BatchSize.Full)]
    [InlineData(BatchSize.Half)]
    public async Task The_batchs_saved_style_overrides_the_default_and_keeps_other_material_defaults(BatchSize size)
    {
        await using var fixture = await Fixture.Create(size);
        var first = fixture.Active[0];
        var originalStyle = first.ProductPackingId;
        var other = new ProductPacking { Id = Guid.NewGuid(), ProductId = fixture.ProductId, Name = "Another saved style" };
        fixture.Context.ProductPackings.Add(other);
        fixture.Context.ProductPackages.AddRange(
            new ProductPackage { ProductId = fixture.ProductId, MaterialId = first.MaterialId, PrescribedQuantity = 999, BaseQuantity = 1 },
            new ProductPackage { ProductId = fixture.ProductId, MaterialId = first.MaterialId, ProductPackingId = other.Id, PrescribedQuantity = 555, BaseQuantity = 1 });
        await fixture.Context.SaveChangesAsync();
        var repository = new ProductRepository(fixture.Context, fixture.Mapper);
        Assert.Equal(10, (await repository.GetProduct(fixture.ProductId)).Value.Packages.Count);

        async Task Verify(int count, decimal prescribed)
        {
            var rows = await fixture.Schedules().CheckPackageMaterialStockLevelsForProductionSchedule(fixture.BatchId, null);
            Assert.True(rows.IsSuccess);
            Assert.Equal(count, rows.Value.Count);
            var row = Assert.Single(rows.Value, item => item.Material.Id == first.MaterialId);
            Assert.Equal(size == BatchSize.Full ? prescribed : prescribed / 2, row.QuantityNeeded);
            var source = await ProductionMaterialSources.Load(fixture.Context, fixture.BatchId, MaterialKind.Package);
            Assert.True(source.IsSuccess);
            Assert.Empty(source.Value.Blockers);
            Assert.Equal(count, source.Value.Requirements.Count);
            Assert.Equal(row.QuantityNeeded, Assert.Single(source.Value.Requirements, item => item.MaterialId == first.MaterialId).Quantity);
        }

        await Verify(8, first.PrescribedQuantity);
        var batch = await fixture.Context.ProductionScheduleProducts.SingleAsync();
        batch.ProductPackingId = other.Id;
        await fixture.Context.SaveChangesAsync();
        await Verify(3, 555);
        batch.ProductPackingId = originalStyle;
        first.DeletedAt = DateTime.UtcNow;
        await fixture.Context.SaveChangesAsync();
        await Verify(8, 999);
    }

    private sealed class Fixture : IAsyncDisposable
    {
        internal ApplicationDbContext Context { get; }
        internal IMapper Mapper { get; }
        private readonly ServiceProvider services;
        internal Guid DepartmentId { get; } = Guid.NewGuid();
        internal Guid ActorId { get; } = Guid.NewGuid();
        internal Guid ProductId { get; } = Guid.NewGuid();
        internal Guid BatchId { get; } = Guid.NewGuid();
        internal Guid UnitId { get; } = Guid.NewGuid();
        internal Guid StorageId { get; } = Guid.NewGuid();
        internal Guid FloorId { get; } = Guid.NewGuid();
        internal List<ProductPackage> Active { get; } = [];

        private Fixture()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>();
            var connection = Environment.GetEnvironmentVariable("ORYX_PACKAGING_READ_TEST_CONNECTION");
            if (string.IsNullOrWhiteSpace(connection)) options.UseInMemoryDatabase(Guid.NewGuid().ToString());
            else
            {
                var parsed = new NpgsqlConnectionStringBuilder(connection);
                if (!parsed.Host.StartsWith("/tmp/oryx-packaging-pg-", StringComparison.Ordinal)
                    || parsed.Port != 55444 || parsed.Database != "oryx_packaging_read_test")
                    throw new InvalidOperationException("Only the disposable local packaging-read database is permitted.");
                options.UseNpgsql(connection);
            }
            Context = new ApplicationDbContext(options.Options, new CurrentUser(DepartmentId, ActorId));
            var collection = new ServiceCollection();
            collection.AddLogging(); collection.AddHttpContextAccessor(); collection.AddSingleton(Context);
            collection.AddAutoMapper(_ => { }, typeof(APP.Mapper.OryxMapper));
            services = collection.BuildServiceProvider(); Mapper = services.GetRequiredService<IMapper>();
        }

        internal ProductionScheduleRepository Schedules() => new(Context, Mapper, null!, null!, null!, null!, new NoOpProductionActivityStepEventPublisher());

        internal static async Task<Fixture> Create(BatchSize size = BatchSize.Full)
        {
            var fixture = new Fixture(); var db = fixture.Context;
            if (db.Database.IsRelational())
            {
                await db.Database.EnsureDeletedAsync();
                await db.Database.EnsureCreatedAsync();
            }
            var department = new Department { Id = fixture.DepartmentId, Name = "Production", Type = DepartmentType.Production };
            var actor = new User { Id = fixture.ActorId, UserName = "packaging-reader", DepartmentId = department.Id };
            db.AddRange(department, actor, new UnitOfMeasure { Id = fixture.UnitId, Name = "Piece", Symbol = "piece" });
            db.Warehouses.AddRange(new Warehouse { Id = fixture.StorageId, Name = "Packaging", DepartmentId = department.Id, Type = WarehouseType.PackagedStorage },
                new Warehouse { Id = fixture.FloorId, Name = "Production", DepartmentId = department.Id, Type = WarehouseType.Production });
            var product = new Product { Id = fixture.ProductId, Name = "Blopen fixture", Code = "NE-001", DepartmentId = department.Id, BaseUomId = fixture.UnitId };
            var style = new ProductPacking { Id = Guid.NewGuid(), ProductId = product.Id, Name = "18x20x30g" };
            var schedule = new ProductionSchedule { Id = Guid.NewGuid(), Code = "PACK-READ", CreatedById = actor.Id, DepartmentId = department.Id };
            db.AddRange(product, style, schedule);
            db.ProductionScheduleProducts.Add(new ProductionScheduleProduct { Id = fixture.BatchId, ProductId = product.Id,
                ProductPackingId = style.Id, ProductionScheduleId = schedule.Id, BatchSize = size, Quantity = 1 });
            decimal[] quantities = [1, 19, 360, 7100, 1, 19, 7100, 7100];
            for (var index = 0; index < 12; index++)
            {
                var material = new Material { Id = Guid.NewGuid(), Name = $"Packaging {index}", Code = $"PM{index:000}", Kind = MaterialKind.Package };
                db.Materials.Add(material);
                db.MaterialDepartments.Add(new MaterialDepartment { MaterialId = material.Id, DepartmentId = department.Id, UoMId = fixture.UnitId });
                var package = new ProductPackage { Id = Guid.NewGuid(), ProductId = product.Id, MaterialId = material.Id,
                    ProductPackingId = index is 4 or 7 ? null : style.Id, BaseQuantity = 1,
                    PrescribedQuantity = index < 8 ? quantities[index] : 9000, Loose = index == 7 ? 0.125m : 0,
                    DeletedAt = index < 8 ? null : DateTime.UtcNow.AddDays(-1) };
                db.ProductPackages.Add(package);
                if (index < 8) fixture.Active.Add(package);
            }
            foreach (var active in fixture.Active.Take(2)) db.ProductPackages.Add(new ProductPackage { ProductId = product.Id,
                MaterialId = active.MaterialId, ProductPackingId = style.Id, PrescribedQuantity = active.PrescribedQuantity, DeletedAt = DateTime.UtcNow.AddDays(-1) });
            await db.SaveChangesAsync();
            return fixture;
        }

        public async ValueTask DisposeAsync() { await services.DisposeAsync(); await Context.DisposeAsync(); }
    }

    private sealed class CurrentUser(Guid departmentId, Guid actorId) : ICurrentUserService
    {
        public Guid? UserId => actorId;
        public Guid? DepartmentId => departmentId;
        public string DepartmentType => "Production";
    }
}
