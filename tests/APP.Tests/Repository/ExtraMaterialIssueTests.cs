using APP.Repository;
using APP.Services.Production;
using DOMAIN.Entities.Products.Production;
using APP.Utils;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.Permissions;
using DOMAIN.Entities.ProductionSchedules;
using DOMAIN.Entities.Roles;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SHARED;
using Xunit;

namespace APP.Tests.Repository;

public sealed class ExtraMaterialIssueTests
{
    private static async Task<ProductionExtraPacking> Seed(ScheduleStockFixture f, bool grant = true, MaterialKind kind = MaterialKind.Raw)
    {
        await f.Seed(); var db = f.Harness.Db;
        f.Lot.Material.Kind = kind; f.Storage.Type = kind == MaterialKind.Raw ? DOMAIN.Entities.Warehouses.WarehouseType.RawMaterialStorage : DOMAIN.Entities.Warehouses.WarehouseType.PackagedStorage;
        f.Shelf.Quantity = 100; f.Lot.QuantityAssigned = 100; f.Lot.TotalQuantity = 100;
        var extra = new ProductionExtraPacking { Id = Guid.NewGuid(), MaterialId = f.Lot.MaterialId, ProductionScheduleProductId = f.Batch.Id,
            UoMId = f.Lot.UoMId, Quantity = 20, Approved = true, Status = ProductionExtraPackingStatus.InProgress };
        db.ProductionExtraPackings.Add(extra);
        if (grant)
        {
            var role = new Role { Name = "Extra issue test" }; db.Roles.Add(role);
            db.UserRoles.Add(new IdentityUserRole<Guid> { UserId = f.Actor.Id, RoleId = role.Id });
            var key = kind == MaterialKind.Raw ? PermissionKeys.CanIssueRawMaterialRequisitions : PermissionKeys.CanIssuePackagingMaterialRequisitions;
            db.RoleClaims.Add(new IdentityRoleClaim<Guid> { Id = 9601, RoleId = role.Id, ClaimType = AppConstants.Permission, ClaimValue = key });
            db.PermissionTypes.Add(new PermissionType { RoleClaimId = 9601, Key = key, Type = "Action" });
        }
        await db.SaveChangesAsync(); return extra;
    }

    [Theory]
    [InlineData(MaterialKind.Raw)]
    [InlineData(MaterialKind.Package)]
    public async Task Shelf_preview_and_issue_agree_and_repeat_cannot_deduct_twice(MaterialKind kind)
    {
        using var f = new ScheduleStockFixture(); var extra = await Seed(f, kind: kind); var db = f.Harness.Db;
        // A historical global balance differs from the physical store; it must not replace shelf availability.
        f.Lot.TotalQuantity = 10; await db.SaveChangesAsync();
        var preview = await f.Repository.BatchesToSupplyForExtraPackingMaterial(extra.Id);
        Assert.True(preview.IsSuccess, preview.Error.Description); Assert.Equal(20, Assert.Single(preview.Value).QuantityToTake);
        var result = await f.Repository.ApproveProductionExtraPacking(extra.Id, [new() { BatchId = f.Lot.Id, Quantity = 20 }], f.Actor.Id);
        Assert.True(result.IsSuccess, result.Error.Description);
        Assert.Equal(80, f.Shelf.Quantity); Assert.Equal(80, f.Lot.QuantityAssigned);
        Assert.Equal(10, f.Lot.TotalQuantity); Assert.Equal(0, f.Lot.ConsumedQuantity);
        Assert.Equal(2, await db.BinCardInformation.CountAsync());
        var repeat = await f.Repository.ApproveProductionExtraPacking(extra.Id, [new() { BatchId = f.Lot.Id, Quantity = 20 }], f.Actor.Id);
        Assert.Equal("ExtraMaterial.AlreadyIssued", repeat.Error.Code); Assert.Equal(80, f.Shelf.Quantity);
    }

    [Theory]
    [InlineData("free")]
    [InlineData("short")]
    [InlineData("retest-due")]
    [InlineData("held")]
    public async Task Request_checks_current_free_and_valid_stock(string scenario)
    {
        using var f = new ScheduleStockFixture(); var existing = await Seed(f); var db = f.Harness.Db;
        db.ProductionExtraPackings.Remove(existing);
        db.Requisitions.Add(new() { Id = Guid.NewGuid(), Code = "RM-EXTRA-TEST", ProductionScheduleProductId = f.Batch.Id,
            RequisitionType = DOMAIN.Entities.Requisitions.RequisitionType.Stock });
        var roleId = await db.UserRoles.Where(row => row.UserId == f.Actor.Id).Select(row => row.RoleId).SingleAsync();
        var key = PermissionKeys.CanRequestExtraPacking;
        db.RoleClaims.Add(new IdentityRoleClaim<Guid> { Id = 9602, RoleId = roleId, ClaimType = AppConstants.Permission, ClaimValue = key });
        db.PermissionTypes.Add(new PermissionType { RoleClaimId = 9602, Key = key, Type = "Action" });
        if (scenario == "short") f.Shelf.Quantity = 5;
        if (scenario == "retest-due") f.Lot.RetestDate = DateTime.UtcNow.AddDays(-1);
        if (scenario == "held") db.MaterialBatchReservedQuantities.Add(new() { MaterialBatchId = f.Lot.Id,
            ProductionScheduleProductId = f.Batch.Id, WarehouseId = f.Production.Id, WarehouseLocationShelfId = f.Shelf.WarehouseLocationShelfId,
            UoMId = f.Lot.UoMId, Quantity = 90 });
        await db.SaveChangesAsync();
        var result = await f.Repository.CreateExtraPacking(f.Batch.Id, [new() { MaterialId = f.Lot.MaterialId, UoMId = f.Lot.UoMId, Quantity = 20 }], f.Actor.Id);
        if (scenario == "free") Assert.True(result.IsSuccess, result.Error.Description);
        else { Assert.Equal("ExtraMaterial.InsufficientStock", result.Error.Code); Assert.Single(await db.ProductionExtraPackings.IgnoreQueryFilters().ToListAsync()); }
    }

    [RepairPostgresFact]
    public async Task Concurrent_issue_commits_one_movement_and_one_pair_of_bin_cards()
    {
        await using var f = new IssuedMaterialRepairFixture();
        await f.Seed(IssuedMaterialRepairKind.Bom, stock: 1000, issueStock: false);
        using var mapping = new ScheduleStockFixture(); await mapping.Seed();
        Guid extraId; Guid lotId;
        await using (var db = f.Open())
        {
            var lot = await db.MaterialBatches.OrderBy(row => row.Id).FirstAsync(); lotId = lot.Id;
            var extra = new ProductionExtraPacking { Id = Guid.NewGuid(), MaterialId = lot.MaterialId,
                ProductionScheduleProductId = f.BatchId, UoMId = lot.UoMId, Quantity = 10, Approved = true, Status = ProductionExtraPackingStatus.InProgress };
            extraId = extra.Id; db.ProductionExtraPackings.Add(extra);
            var key = PermissionKeys.CanIssueRawMaterialRequisitions;
            db.RoleClaims.Add(new IdentityRoleClaim<Guid> { Id = 9603, RoleId = f.RoleId, ClaimType = AppConstants.Permission, ClaimValue = key });
            db.PermissionTypes.Add(new PermissionType { RoleClaimId = 9603, Key = key, Type = "Action" });
            await db.SaveChangesAsync();
        }
        async Task<Result> Issue()
        {
            await using var db = f.Open();
            var repo = new ProductionScheduleRepository(db, mapping.Mapper, null!, null!, null!, null!, new NoOpProductionActivityStepEventPublisher());
            return await repo.ApproveProductionExtraPacking(extraId, [new() { BatchId = lotId, Quantity = 10 }], f.Actor.UserId!.Value);
        }
        var results = await Task.WhenAll(Issue(), Issue());
        Assert.Single(results, result => result.IsSuccess);
        Assert.Equal("ExtraMaterial.AlreadyIssued", Assert.Single(results, result => result.IsFailure).Error.Code);
        await using var check = f.Open();
        Assert.Equal(2, await check.BinCardInformation.CountAsync(card => card.QuantityIssued == 10 || card.QuantityReceived == 10));
        Assert.Single(await check.MassMaterialBatchMovements.ToListAsync());
    }

    [Fact]
    public void Controller_retains_specific_request_and_variant_issue_permissions()
    {
        var create = typeof(API.Controllers.ProductionScheduleController).GetMethod("CreateExtraPacking")!;
        Assert.Equal(PermissionKeys.CanRequestExtraPacking, Assert.Single(create.GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute), false).Cast<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>()).Policy);
        var issue = typeof(API.Controllers.ProductionScheduleController).GetMethod("ApproveProductionExtraPacking")!;
        var policy = Assert.Single(issue.GetCustomAttributes(typeof(API.Authorization.MaterialKindPermissionAttribute), false).Cast<API.Authorization.MaterialKindPermissionAttribute>());
        Assert.Equal(PermissionKeys.CanIssueRawMaterialRequisitions, policy.RawKey);
        Assert.Equal(PermissionKeys.CanIssuePackagingMaterialRequisitions, policy.PackagingKey);
        Assert.Empty(issue.GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute), false));
    }

    [Theory]
    [InlineData("held")]
    [InlineData("short")]
    [InlineData("expired")]
    [InlineData("retest-due")]
    [InlineData("wrong-quantity")]
    [InlineData("permission")]
    public async Task Refused_issue_does_not_mutate_stock_or_history(string scenario)
    {
        using var f = new ScheduleStockFixture(); var extra = await Seed(f, scenario != "permission"); var db = f.Harness.Db;
        if (scenario == "held") db.MaterialBatchReservedQuantities.Add(new() { MaterialBatchId = f.Lot.Id,
            ProductionScheduleProductId = f.Batch.Id, WarehouseId = f.Production.Id, WarehouseLocationShelfId = f.Shelf.WarehouseLocationShelfId,
            UoMId = f.Lot.UoMId, Quantity = 90 });
        if (scenario == "short") f.Shelf.Quantity = 10;
        if (scenario == "retest-due") { f.Lot.ExpiryDate = DateTime.UtcNow.AddYears(1); f.Lot.RetestDate = DateTime.UtcNow.AddDays(-1); }
        if (scenario == "expired") f.Lot.RetestDate = DateTime.UtcNow.AddDays(-1);
        await db.SaveChangesAsync(); var before = f.Shelf.Quantity;
        var result = await f.Repository.ApproveProductionExtraPacking(extra.Id,
            [new() { BatchId = f.Lot.Id, Quantity = scenario == "wrong-quantity" ? 21 : 20 }], f.Actor.Id);
        Assert.True(result.IsFailure); Assert.Equal(before, f.Shelf.Quantity); Assert.Null(extra.IssuedAt);
        Assert.Empty(await db.BinCardInformation.ToListAsync()); Assert.Empty(await db.MassMaterialBatchMovements.ToListAsync());
    }
}
