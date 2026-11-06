using System.Reflection;
using API.Authorization;
using API.Controllers;
using APP.Utils;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.ProductionSchedules;
using DOMAIN.Entities.ProductionSchedules.StockTransfers;
using INFRASTRUCTURE.Context;
using INFRASTRUCTURE.Migrations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Authorization;

/// <summary>Production schedule writes: each task needs its own key on the server (audit phase 4).</summary>
public sealed class ProductionSchedulePermissionTests
{
    private static IEnumerable<MethodInfo> Actions(string name) => typeof(ProductionScheduleController).GetMethods().Where(method => method.Name == name);

    [Theory]
    [InlineData("CreateProductionSchedule", PermissionKeys.CanCreateProductSchedule)]
    [InlineData("DeleteProductionSchedule", PermissionKeys.CanDeleteProductSchedule)]
    [InlineData("StartProductionActivity", PermissionKeys.CanStartProduction)]
    [InlineData("UpdateStatusOfProductionActivityStep", PermissionKeys.CanUpdateProductionStep)]
    [InlineData("ReturnAfterProduction", PermissionKeys.CanReturnProductionMaterials)]
    [InlineData("DeleteFinalPacking", PermissionKeys.CanRecordFinalPacking)]
    [InlineData("CreateFinishedGoodsTransferNote", PermissionKeys.CanCreateFgtn)]
    [InlineData("ApproveTransferNote", PermissionKeys.CanApproveFgtn)]
    [InlineData("CreateExtraPacking", PermissionKeys.CanRequestExtraPacking)]
    [InlineData("RejectStockTransfer", PermissionKeys.CanRejectIncomingStockTransferRequest)]
    [InlineData("IssueBatchManufacturingRecord", PermissionKeys.CanIssueBmr)]
    [InlineData("IssueBatchPackagingRecord", PermissionKeys.CanIssueBpr)]
    [InlineData("CompleteMaterialReturnNoteById", PermissionKeys.CanCompleteMaterialReturnNote)]
    public void A_schedule_write_needs_its_task_key(string action, string key) =>
        Assert.All(Actions(action), method => Assert.Equal(key, Assert.Single(method.GetCustomAttributes<AuthorizeAttribute>()).Policy));

    [Theory]
    [InlineData("CreateStockTransfer", PermissionKeys.CanCreateRawMaterialRequisitions, PermissionKeys.CanCreatePackageMaterialRequisitions)]
    [InlineData("IssueStockTransfer", PermissionKeys.CanIssueRawMaterialStockTransfers, PermissionKeys.CanIssuePackagingMaterialStockTransfers)]
    [InlineData("ApproveProductionExtraPacking", PermissionKeys.CanIssueRawMaterialRequisitions, PermissionKeys.CanIssuePackagingMaterialRequisitions)]
    public void Material_moves_use_the_key_for_the_material_kind(string action, string raw, string packaging)
    {
        var attribute = Assert.Single(Actions(action).SelectMany(method => method.GetCustomAttributes<MaterialKindPermissionAttribute>()));
        Assert.Equal((raw, packaging), (attribute.RawKey, attribute.PackagingKey));
    }

    private sealed class Actor : ICurrentUserService
    {
        public Guid? UserId => Guid.Empty;
        public Guid? DepartmentId => null;
        public string DepartmentType => string.Empty;
    }

    [Fact]
    public async Task A_stored_transfer_or_extra_packing_request_names_its_material_kind()
    {
        var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, new Actor());
        var foil = new Material { Id = Guid.NewGuid(), Code = "PM-1", Name = "Foil", Kind = MaterialKind.Package };
        var api = new Material { Id = Guid.NewGuid(), Code = "RM-1", Name = "Paracetamol", Kind = MaterialKind.Raw };
        var transfer = new StockTransfer { Id = Guid.NewGuid(), Code = "ST-1", MaterialId = foil.Id };
        var extra = new ProductionExtraPacking { Id = Guid.NewGuid(), MaterialId = api.Id };
        db.Materials.AddRange(foil, api);
        db.StockTransfers.Add(transfer);
        db.ProductionExtraPackings.Add(extra);
        await db.SaveChangesAsync();
        Assert.Equal([MaterialKind.Package], await MaterialKindPermissionAttribute.Kinds(db, new Dictionary<string, object> { ["stockTransferId"] = transfer.Id }));
        Assert.Equal([MaterialKind.Raw], await MaterialKindPermissionAttribute.Kinds(db, new Dictionary<string, object> { ["productionExtraPackingId"] = extra.Id }));
    }

    [Fact]
    public void New_keys_are_registered_and_granted_from_the_keys_that_gated_them()
    {
        var registered = PermissionUtils.GeneratePermissions().Select(permission => permission.Key).ToHashSet();
        Assert.All(GrantProductionScheduleTasks.Grants, grant => { Assert.Contains(grant.Key, registered); Assert.Contains(grant.From, registered); });
        Assert.Contains(GrantProductionScheduleTasks.Grants, grant => grant == (PermissionKeys.CanUpdateProductionStep, PermissionKeys.CanViewPlannedProducts));
        Assert.DoesNotContain(GrantProductionScheduleTasks.Grants, grant => grant.Key is PermissionKeys.CanDeleteProductSchedule or PermissionKeys.CanCompleteMaterialReturnNote);
    }
}
