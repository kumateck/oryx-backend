using System.Reflection;
using API.Controllers;
using APP.Repository;
using APP.Services.Production;
using APP.Utils;
using DOMAIN.Entities.BillOfMaterials;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.Permissions;
using DOMAIN.Entities.Products.Production;
using DOMAIN.Entities.Requisitions;
using DOMAIN.Entities.Roles;
using DOMAIN.Entities.Warehouses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SHARED;
using Xunit;

namespace APP.Tests.Repository;

/// <summary>
/// A BOM substitute carries its own quantity (it may be a dry form of a hydrated main material), and the store issuing
/// the requisition decides which one option leaves the store.
/// </summary>
public class BomSubstituteChoiceTests
{
    private static int claimId = 50_000;

    private static async Task<MaterialBatch> AddSubstitute(ScheduleStockFixture fixture, decimal onShelf, decimal? prescribed)
    {
        var db = fixture.Harness.Db;
        var lot = await fixture.Harness.SeedMaterialBatch("SUB-001", BatchStatus.Available);
        lot.Material.Name = "Dry substitute";
        lot.Material.Kind = MaterialKind.Raw;
        lot.UoM.Symbol = "mg";
        lot.ExpiryDate = DateTime.UtcNow.AddDays(60);
        db.MaterialDepartments.Add(new() { Id = Guid.NewGuid(), MaterialId = lot.MaterialId, DepartmentId = fixture.Department.Id, UoMId = lot.UoMId });
        db.ShelfMaterialBatches.Add(new ShelfMaterialBatch { Id = Guid.NewGuid(), MaterialBatchId = lot.Id, Quantity = onShelf, UoMId = lot.UoMId,
            WarehouseLocationShelfId = fixture.Shelf.WarehouseLocationShelfId });
        var line = await db.BillOfMaterialItems.SingleAsync();
        db.BillOfMaterialItemSubstitutes.Add(new BillOfMaterialItemSubstitute { Id = Guid.NewGuid(), BillOfMaterialItemId = line.Id,
            SubstituteMaterialId = lot.MaterialId, BaseUoMId = prescribed.HasValue ? lot.UoMId : null,
            BaseQuantity = prescribed / 12_500, PrescribedQuantity = prescribed });
        await db.SaveChangesAsync();
        return lot;
    }

    private static async Task EmptyMainShelf(ScheduleStockFixture fixture)
    {
        var shelf = await fixture.Harness.Db.ShelfMaterialBatches.SingleAsync(item => item.MaterialBatchId == fixture.Lot.Id);
        shelf.Quantity = 100;
        await fixture.Harness.Db.SaveChangesAsync();
    }

    private static void Grant(ScheduleStockFixture fixture, params string[] keys)
    {
        var db = fixture.Harness.Db;
        var role = new Role { Id = Guid.NewGuid(), Name = $"store-{Guid.NewGuid():N}", DisplayName = "Store" };
        db.Roles.Add(role);
        foreach (var key in keys)
        {
            var claim = new IdentityRoleClaim<Guid> { Id = ++claimId, RoleId = role.Id, ClaimType = AppConstants.Permission, ClaimValue = key };
            db.RoleClaims.Add(claim);
            db.PermissionTypes.Add(new PermissionType { Id = Guid.NewGuid(), Key = key, RoleClaimId = claim.Id, Type = "Access" });
        }
        db.UserRoles.Add(new IdentityUserRole<Guid> { UserId = fixture.Actor.Id, RoleId = role.Id });
    }

    private static async Task<Requisition> Requisition(ScheduleStockFixture fixture)
    {
        var requisition = new Requisition { Id = Guid.NewGuid(), Code = "SR-1", RequisitionType = RequisitionType.Stock, DepartmentId = fixture.Department.Id,
            RequestedById = fixture.Creator.Id, ProductionScheduleProductId = fixture.Batch.Id, Approved = true,
            Items = [new() { Id = Guid.NewGuid(), MaterialId = fixture.Lot.MaterialId, UoMId = fixture.Lot.UoMId, Quantity = 300_000 }] };
        fixture.Harness.Db.Requisitions.Add(requisition);
        await fixture.Harness.Db.SaveChangesAsync();
        return requisition;
    }

    private static Task<List<MaterialBatchReservedQuantity>> Holds(ScheduleStockFixture fixture) =>
        fixture.Harness.Db.MaterialBatchReservedQuantities.AsNoTracking().IgnoreAutoIncludes().Include(item => item.MaterialBatch)
            .Where(item => item.ProductionScheduleProductId == fixture.Batch.Id && item.ReleasedAt == null).ToListAsync();

    [Fact]
    public async Task A_substitute_uses_its_own_quantity_and_the_main_material_stays_selected_while_it_is_in_stock()
    {
        using var fixture = new ScheduleStockFixture();
        await fixture.Seed();
        var substitute = await AddSubstitute(fixture, 500_000, 200_000);
        fixture.Harness.Db.ChangeTracker.Clear();

        var source = (await ProductionMaterialSources.Load(fixture.Harness.Db, fixture.Batch.Id, MaterialKind.Raw)).Value;

        var requirement = Assert.Single(source.Requirements);
        Assert.Equal(fixture.Lot.MaterialId, requirement.MaterialId);
        Assert.Equal(300_000, requirement.Quantity);
        Assert.Collection(requirement.Options!,
            main => Assert.Equal((fixture.Lot.MaterialId, 300_000m), (main.MaterialId, main.Quantity)),
            dry => Assert.Equal((substitute.MaterialId, 200_000m), (dry.MaterialId, dry.Quantity)));
    }

    [Fact]
    public async Task Requirements_without_a_quantified_substitute_serialize_as_before()
    {
        using var fixture = new ScheduleStockFixture();
        await fixture.Seed();
        await AddSubstitute(fixture, 500_000, prescribed: null);
        fixture.Harness.Db.ChangeTracker.Clear();

        var source = (await ProductionMaterialSources.Load(fixture.Harness.Db, fixture.Batch.Id, MaterialKind.Raw)).Value;

        Assert.Null(Assert.Single(source.Requirements).Options);
        Assert.DoesNotContain("Options", ProductionMaterialSources.Json(source.Requirements));
    }

    [Fact]
    public async Task A_requested_substitute_without_its_own_quantity_never_borrows_the_main_quantity()
    {
        using var fixture = new ScheduleStockFixture();
        await fixture.Seed();
        var substitute = await AddSubstitute(fixture, 500_000, prescribed: null);
        fixture.Harness.Db.Requisitions.Add(new() { Id = Guid.NewGuid(), RequisitionType = RequisitionType.Stock, ProductionScheduleProductId = fixture.Batch.Id,
            DepartmentId = fixture.Department.Id, RequestedById = fixture.Creator.Id,
            Items = [new() { Id = Guid.NewGuid(), MaterialId = substitute.MaterialId, UoMId = substitute.UoMId, Quantity = 300_000 }] });
        await fixture.Harness.Db.SaveChangesAsync();
        fixture.Harness.Db.ChangeTracker.Clear();

        var source = (await ProductionMaterialSources.Load(fixture.Harness.Db, fixture.Batch.Id, MaterialKind.Raw)).Value;

        Assert.Empty(source.Requirements);
        Assert.Contains(source.Blockers, blocker => blocker.Contains("no quantity of its own"));
    }

    [Fact]
    public async Task Start_reserves_a_substitute_at_its_own_quantity_when_the_main_material_is_short()
    {
        using var fixture = new ScheduleStockFixture();
        await fixture.Seed();
        var substitute = await AddSubstitute(fixture, 500_000, 200_000);
        await EmptyMainShelf(fixture);
        fixture.Harness.Db.ChangeTracker.Clear();

        var started = await ProductionMaterialRepairs.Initialize(fixture.Harness.Db, fixture.Batch.Id);

        Assert.True(started.IsSuccess, started.IsFailure ? started.Error.Description : null);
        var hold = Assert.Single(await Holds(fixture));
        Assert.Equal((substitute.MaterialId, 200_000m), (hold.MaterialBatch.MaterialId, hold.Quantity));
        var configuration = await fixture.Harness.Db.ProductionMaterialConfigurations.SingleAsync();
        var requirement = Assert.Single(ProductionMaterialSources.Requirements(configuration.RequirementsJson));
        Assert.Equal((substitute.MaterialId, fixture.Lot.MaterialId, 200_000m), (requirement.MaterialId, requirement.SourceMaterialId, requirement.Quantity));
    }

    [Fact]
    public async Task Production_can_choose_the_substitute_at_start_even_while_the_main_is_in_stock()
    {
        using var fixture = new ScheduleStockFixture();
        await fixture.Seed();
        var substitute = await AddSubstitute(fixture, 500_000, 200_000);
        fixture.Harness.Db.ChangeTracker.Clear();

        var started = await ProductionMaterialRepairs.Initialize(fixture.Harness.Db, fixture.Batch.Id,
            new Dictionary<Guid, Guid> { [fixture.Lot.MaterialId] = substitute.MaterialId });

        Assert.True(started.IsSuccess, started.IsFailure ? started.Error.Description : null);
        // The chosen substitute is what is reserved, requested and issued.
        var hold = Assert.Single(await Holds(fixture));
        Assert.Equal((substitute.MaterialId, 200_000m), (hold.MaterialBatch.MaterialId, hold.Quantity));
        var configuration = await fixture.Harness.Db.ProductionMaterialConfigurations.SingleAsync();
        Assert.Equal(substitute.MaterialId, Assert.Single(ProductionMaterialSources.Requirements(configuration.RequirementsJson)).MaterialId);
    }

    [Fact]
    public async Task A_chosen_main_material_that_is_short_is_reported_not_swapped_for_the_substitute()
    {
        using var fixture = new ScheduleStockFixture();
        await fixture.Seed();
        await AddSubstitute(fixture, 500_000, 200_000);
        await EmptyMainShelf(fixture);
        fixture.Harness.Db.ChangeTracker.Clear();

        var started = await ProductionMaterialRepairs.Initialize(fixture.Harness.Db, fixture.Batch.Id,
            new Dictionary<Guid, Guid> { [fixture.Lot.MaterialId] = fixture.Lot.MaterialId });

        Assert.Equal("Production.Materials", started.Error.Code);
        Assert.Empty(await Holds(fixture));
    }

    [Fact]
    public async Task A_choice_that_is_not_a_substitute_on_the_line_is_refused()
    {
        using var fixture = new ScheduleStockFixture();
        await fixture.Seed();
        await AddSubstitute(fixture, 500_000, 200_000);
        fixture.Harness.Db.ChangeTracker.Clear();

        var started = await ProductionMaterialRepairs.Initialize(fixture.Harness.Db, fixture.Batch.Id,
            new Dictionary<Guid, Guid> { [fixture.Lot.MaterialId] = Guid.NewGuid() });

        Assert.Equal("Production.Substitute", started.Error.Code);
        Assert.Empty(await Holds(fixture));
    }

    [Fact]
    public async Task The_forecast_lists_each_substitute_right_below_its_main_material_with_its_own_need_and_stock()
    {
        using var fixture = new ScheduleStockFixture();
        await fixture.Seed();
        var substitute = await AddSubstitute(fixture, 500_000, 200_000);
        await using var signedIn = fixture.Harness.OpenAs(new ProductionUser(fixture.Department.Id));
        var repository = new ProductionScheduleRepository(signedIn, fixture.Mapper, fixture.Harness.UserManager, null!, null!, fixture.Harness.ApprovalRepository,
            new NoOpProductionActivityStepEventPublisher());

        var forecast = await repository.ForecastProductionScheduleProduct(fixture.Batch.ProductId, 1, Guid.Empty,
            DOMAIN.Entities.ProductionSchedules.BatchSize.Full, fixture.Actor.Id);

        Assert.True(forecast.IsSuccess, forecast.IsFailure ? forecast.Error.Description : null);
        Assert.Equal(2, forecast.Value.Count);
        var (main, sub) = (forecast.Value[0], forecast.Value[1]);
        Assert.Equal((fixture.Lot.MaterialId, false), (main.Material.Id, main.IsSubstitute));
        Assert.Equal((substitute.MaterialId, true, fixture.Lot.MaterialId), (sub.Material.Id, sub.IsSubstitute, sub.SubstituteFor.Id));
        Assert.Equal((200_000m, 500_000m), (sub.QuantityNeeded, sub.FreeQuantity.GetValueOrDefault()));
    }

    [Fact]
    public async Task A_substitute_without_its_own_quantity_is_never_used_at_start()
    {
        using var fixture = new ScheduleStockFixture();
        await fixture.Seed();
        await AddSubstitute(fixture, 500_000, prescribed: null);
        await EmptyMainShelf(fixture);
        fixture.Harness.Db.ChangeTracker.Clear();

        var started = await ProductionMaterialRepairs.Initialize(fixture.Harness.Db, fixture.Batch.Id);

        Assert.Equal("Production.Materials", started.Error.Code);
        Assert.Empty(await Holds(fixture));
    }

    [Fact]
    public async Task The_store_sees_each_option_with_its_stock_and_expiry_and_can_issue_the_substitute_instead()
    {
        using var fixture = new ScheduleStockFixture();
        await fixture.Seed();
        var substitute = await AddSubstitute(fixture, 500_000, 200_000);
        Assert.True((await ProductionMaterialRepairs.Initialize(fixture.Harness.Db, fixture.Batch.Id)).IsSuccess);
        var requisition = await Requisition(fixture);
        Grant(fixture, PermissionKeys.CanChooseRawMaterialRequisitionSubstitute, PermissionKeys.CanViewRawMaterialRequisitions);
        await fixture.Harness.Db.SaveChangesAsync();
        fixture.Harness.Db.ChangeTracker.Clear();
        var service = new StockRequisitionMaterialChoiceService(fixture.Harness.Db, fixture.Mapper);

        var line = Assert.Single((await service.Options(requisition.Id, fixture.Actor.Id)).Value);
        Assert.Equal(fixture.Lot.MaterialId, line.CurrentMaterialId);
        Assert.True(line.CanChange);
        var dry = Assert.Single(line.Options, option => !option.IsMain);
        Assert.Equal((200_000m, 500_000m, true), (dry.Quantity, dry.Available, dry.Covers));
        Assert.Equal(DateTime.UtcNow.AddDays(60).Date, dry.EarliestExpiry?.Date);
        Assert.Equal(1_000_000m, Assert.Single(line.Options, option => option.IsMain).Available);

        var chosen = await service.Choose(requisition.Id, new() { SourceMaterialId = fixture.Lot.MaterialId, MaterialId = substitute.MaterialId,
            Revision = line.Revision, Reason = "Main lot expires later; use the dry form first." }, fixture.Actor.Id);

        Assert.True(chosen.IsSuccess, chosen.IsFailure ? chosen.Error.Description : null);
        var hold = Assert.Single(await Holds(fixture));
        Assert.Equal((substitute.MaterialId, 200_000m), (hold.MaterialBatch.MaterialId, hold.Quantity));
        var item = await fixture.Harness.Db.RequisitionItems.AsNoTracking().SingleAsync();
        Assert.Equal((substitute.MaterialId, 200_000m, (Guid?)substitute.UoMId), (item.MaterialId, item.Quantity, item.UoMId));
        var audit = await fixture.Harness.Db.StockRequisitionMaterialChoices.SingleAsync();
        Assert.Equal((fixture.Lot.MaterialId, substitute.MaterialId, 300_000m, 200_000m, fixture.Actor.Id),
            (audit.FromMaterialId, audit.ToMaterialId, audit.FromQuantity, audit.ToQuantity, audit.ChosenById));
        var configuration = await fixture.Harness.Db.ProductionMaterialConfigurations.AsNoTracking().SingleAsync();
        Assert.Equal(substitute.MaterialId, Assert.Single(ProductionMaterialSources.Requirements(configuration.RequirementsJson)).MaterialId);
        Assert.Equal(line.Revision + 1, configuration.Revision);
        var read = new RequisitionDto { Id = requisition.Id, Items = [new() { Id = item.Id, Material = new() { Id = substitute.MaterialId } }] };
        await StockRequisitionMaterialChoiceRead.Apply(fixture.Harness.Db, read);
        Assert.Equal(fixture.Lot.MaterialId, read.Items[0].SubstituteFor.Id);
    }

    [Fact]
    public async Task Choosing_without_the_permission_is_refused_and_nothing_moves()
    {
        using var fixture = new ScheduleStockFixture();
        await fixture.Seed();
        var substitute = await AddSubstitute(fixture, 500_000, 200_000);
        Assert.True((await ProductionMaterialRepairs.Initialize(fixture.Harness.Db, fixture.Batch.Id)).IsSuccess);
        var requisition = await Requisition(fixture);
        Grant(fixture, PermissionKeys.CanIssueRawMaterialRequisitions);
        await fixture.Harness.Db.SaveChangesAsync();
        fixture.Harness.Db.ChangeTracker.Clear();

        var refused = await new StockRequisitionMaterialChoiceService(fixture.Harness.Db, fixture.Mapper).Choose(requisition.Id,
            new() { SourceMaterialId = fixture.Lot.MaterialId, MaterialId = substitute.MaterialId, Revision = 1, Reason = "Try" }, fixture.Actor.Id);

        Assert.Equal(ErrorType.Forbidden, refused.Error.Type);
        Assert.Equal(fixture.Lot.MaterialId, Assert.Single(await Holds(fixture)).MaterialBatch.MaterialId);
        Assert.Empty(fixture.Harness.Db.StockRequisitionMaterialChoices);
    }

    [Fact]
    public async Task The_choice_is_locked_once_the_requisition_has_been_issued()
    {
        using var fixture = new ScheduleStockFixture();
        await fixture.Seed();
        var substitute = await AddSubstitute(fixture, 500_000, 200_000);
        Assert.True((await ProductionMaterialRepairs.Initialize(fixture.Harness.Db, fixture.Batch.Id)).IsSuccess);
        var requisition = await Requisition(fixture);
        Grant(fixture, PermissionKeys.CanChooseRawMaterialRequisitionSubstitute, PermissionKeys.CanViewRawMaterialRequisitions);
        fixture.Harness.Db.StockRequisitionIssues.Add(new() { Id = Guid.NewGuid(), RequisitionId = requisition.Id, IssuedById = fixture.Actor.Id, IssuedAt = DateTime.UtcNow });
        await fixture.Harness.Db.SaveChangesAsync();
        fixture.Harness.Db.ChangeTracker.Clear();
        var service = new StockRequisitionMaterialChoiceService(fixture.Harness.Db, fixture.Mapper);

        Assert.False(Assert.Single((await service.Options(requisition.Id, fixture.Actor.Id)).Value).CanChange);
        var refused = await service.Choose(requisition.Id,
            new() { SourceMaterialId = fixture.Lot.MaterialId, MaterialId = substitute.MaterialId, Revision = 1, Reason = "Late" }, fixture.Actor.Id);

        Assert.Equal("Substitute.Locked", refused.Error.Code);
        Assert.Equal(fixture.Lot.MaterialId, Assert.Single(await Holds(fixture)).MaterialBatch.MaterialId);
    }

    [Fact]
    public async Task Choosing_a_substitute_without_enough_stock_is_refused()
    {
        using var fixture = new ScheduleStockFixture();
        await fixture.Seed();
        var substitute = await AddSubstitute(fixture, 150_000, 200_000);
        Assert.True((await ProductionMaterialRepairs.Initialize(fixture.Harness.Db, fixture.Batch.Id)).IsSuccess);
        var requisition = await Requisition(fixture);
        Grant(fixture, PermissionKeys.CanChooseRawMaterialRequisitionSubstitute, PermissionKeys.CanViewRawMaterialRequisitions);
        await fixture.Harness.Db.SaveChangesAsync();
        fixture.Harness.Db.ChangeTracker.Clear();

        var refused = await new StockRequisitionMaterialChoiceService(fixture.Harness.Db, fixture.Mapper).Choose(requisition.Id,
            new() { SourceMaterialId = fixture.Lot.MaterialId, MaterialId = substitute.MaterialId, Revision = 1, Reason = "Older" }, fixture.Actor.Id);

        Assert.Equal("Substitute.Shortage", refused.Error.Code);
        Assert.Equal(300_000m, (await fixture.Harness.Db.RequisitionItems.AsNoTracking().SingleAsync()).Quantity);
    }

    [Fact]
    public void Material_option_endpoints_name_their_permission()
    {
        static string[] Keys(string action)
        {
            var method = typeof(RequisitionController).GetMethod(action)!;
            Assert.Empty(method.GetCustomAttributes<AllowAnonymousAttribute>());
            return Assert.Single(method.GetCustomAttributes<API.Authorization.AnyPermissionAttribute>()).Keys.Order().ToArray();
        }
        // Raw and packaging each keep their own key; the service checks the one for the requisition's kind.
        Assert.Equal([PermissionKeys.CanViewPackagingMaterialRequisitions, PermissionKeys.CanViewRawMaterialRequisitions], Keys(nameof(RequisitionController.GetMaterialOptions)));
        Assert.Equal([PermissionKeys.CanChoosePackagingMaterialRequisitionSubstitute, PermissionKeys.CanChooseRawMaterialRequisitionSubstitute], Keys(nameof(RequisitionController.ChooseMaterialOption)));
        Assert.Single(PermissionUtils.GeneratePermissions().Where(item => item.Key == PermissionKeys.CanChooseRawMaterialRequisitionSubstitute));
        Assert.Single(PermissionUtils.GeneratePermissions().Where(item => item.Key == PermissionKeys.CanChoosePackagingMaterialRequisitionSubstitute));
    }

    /// <summary>A packing line (main bottle) with one substitute bottle, both on a shelf of the department's packing store.</summary>
    private static async Task<(MaterialBatch Main, MaterialBatch Substitute, Warehouse Store)> Packaging(ScheduleStockFixture fixture)
    {
        var db = fixture.Harness.Db;
        var store = await db.Warehouses.IgnoreQueryFilters().SingleAsync(item => item.Type == WarehouseType.PackagedStorage);
        var shelf = new WarehouseLocationShelf { Id = Guid.NewGuid(), Name = "P1", WarehouseLocationRack = new()
            { Id = Guid.NewGuid(), Name = "Rack P", WarehouseLocation = new() { Id = Guid.NewGuid(), Name = "Packing room", WarehouseId = store.Id } } };
        db.WarehouseLocationShelves.Add(shelf);
        async Task<MaterialBatch> Bottle(string number, string name)
        {
            var lot = await fixture.Harness.SeedMaterialBatch(number, BatchStatus.Available);
            lot.Material.Name = name; lot.Material.Kind = MaterialKind.Package; lot.UoM.Symbol = "pcs"; lot.ExpiryDate = DateTime.UtcNow.AddYears(2);
            db.MaterialDepartments.Add(new() { Id = Guid.NewGuid(), MaterialId = lot.MaterialId, DepartmentId = fixture.Department.Id, UoMId = lot.UoMId });
            db.ShelfMaterialBatches.Add(new ShelfMaterialBatch { Id = Guid.NewGuid(), MaterialBatchId = lot.Id, Quantity = 5_000, UoMId = lot.UoMId, WarehouseLocationShelfId = shelf.Id });
            return lot;
        }
        var main = await Bottle("PM-MAIN", "Amber bottle 100 ml");
        var substitute = await Bottle("PM-ALT", "Clear bottle 100 ml");
        var style = new DOMAIN.Entities.Products.ProductPacking { Id = Guid.NewGuid(), ProductId = fixture.Batch.ProductId, Name = "Bottle pack" };
        db.ProductPackings.Add(style);
        db.ProductPackages.Add(new DOMAIN.Entities.Products.ProductPackage { Id = Guid.NewGuid(), ProductId = fixture.Batch.ProductId, ProductPackingId = style.Id,
            MaterialId = main.MaterialId, BaseQuantity = 1, PrescribedQuantity = 1_000,
            Substitutes = [new() { Id = Guid.NewGuid(), SubstituteMaterialId = substitute.MaterialId }] });
        (await db.ProductionScheduleProducts.IgnoreQueryFilters().SingleAsync(item => item.Id == fixture.Batch.Id)).ProductPackingId = style.Id;
        await db.SaveChangesAsync();
        return (main, substitute, store);
    }

    [Fact]
    public async Task A_packaging_substitute_is_offered_one_for_one_and_issued_only_with_the_packaging_key()
    {
        using var fixture = new ScheduleStockFixture();
        await fixture.Seed();
        var (main, substitute, store) = await Packaging(fixture);
        var db = fixture.Harness.Db;
        var source = await ProductionMaterialSources.Load(db, fixture.Batch.Id, MaterialKind.Package);
        Assert.True(source.IsSuccess, source.IsFailure ? source.Error.Description : null);
        var requirement = Assert.Single(source.Value.Requirements);
        Assert.Equal(main.MaterialId, requirement.MaterialId);
        // The substitute bottle replaces the main one pack for pack.
        Assert.Equal([(main.MaterialId, 1_000m), (substitute.MaterialId, 1_000m)], requirement.Options!.Select(option => (option.MaterialId, option.Quantity)).ToList());
        db.ProductionMaterialConfigurations.Add(new ProductionMaterialConfiguration { Id = Guid.NewGuid(), ProductionScheduleProductId = fixture.Batch.Id,
            Kind = MaterialKind.Package, DepartmentId = fixture.Department.Id, StorageWarehouseId = store.Id, ProductionWarehouseId = fixture.Production.Id,
            RequirementsJson = ProductionMaterialSources.Json(source.Value.Requirements), SourceHash = source.Value.SourceHash, Revision = 1 });
        var requisition = new Requisition { Id = Guid.NewGuid(), Code = "PM-1", RequisitionType = RequisitionType.Stock, DepartmentId = fixture.Department.Id,
            RequestedById = fixture.Creator.Id, ProductionScheduleProductId = fixture.Batch.Id, Approved = true,
            Items = [new() { Id = Guid.NewGuid(), MaterialId = main.MaterialId, UoMId = main.UoMId, Quantity = 1_000 }] };
        db.Requisitions.Add(requisition);
        // The raw-material key does not cover packaging.
        Grant(fixture, PermissionKeys.CanChooseRawMaterialRequisitionSubstitute, PermissionKeys.CanViewPackagingMaterialRequisitions);
        await db.SaveChangesAsync(); db.ChangeTracker.Clear();
        var service = new StockRequisitionMaterialChoiceService(db, fixture.Mapper);
        var line = Assert.Single((await service.Options(requisition.Id, fixture.Actor.Id)).Value);
        Assert.Equal((5_000m, true), (Assert.Single(line.Options, option => !option.IsMain).Available, Assert.Single(line.Options, option => !option.IsMain).Covers));
        ChooseStockRequisitionMaterialRequest Request() => new() { SourceMaterialId = main.MaterialId, MaterialId = substitute.MaterialId,
            Revision = line.Revision, Reason = "Amber bottles are short; clear bottles approved for this market." };
        Assert.Equal(ErrorType.Forbidden, (await service.Choose(requisition.Id, Request(), fixture.Actor.Id)).Error.Type);

        Grant(fixture, PermissionKeys.CanChoosePackagingMaterialRequisitionSubstitute);
        await db.SaveChangesAsync(); db.ChangeTracker.Clear();
        var chosen = await service.Choose(requisition.Id, Request(), fixture.Actor.Id);
        Assert.True(chosen.IsSuccess, chosen.IsFailure ? chosen.Error.Description : null);
        var item = await db.RequisitionItems.AsNoTracking().SingleAsync(row => row.RequisitionId == requisition.Id);
        Assert.Equal((substitute.MaterialId, 1_000m), (item.MaterialId, item.Quantity));
        var hold = Assert.Single(await Holds(fixture), row => row.MaterialBatch.MaterialId == substitute.MaterialId || row.MaterialBatch.MaterialId == main.MaterialId);
        Assert.Equal((substitute.MaterialId, 1_000m), (hold.MaterialBatch.MaterialId, hold.Quantity));
    }

    private sealed class ProductionUser(Guid department) : SHARED.Services.Identity.ICurrentUserService
    {
        public Guid? UserId => null;
        public Guid? DepartmentId => department;
        public string DepartmentType => "Production";
    }
}
