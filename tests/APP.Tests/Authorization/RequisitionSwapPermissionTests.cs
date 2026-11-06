using System.Reflection;
using API.Authorization;
using API.Controllers;
using APP.Utils;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.Requisitions;
using DOMAIN.Entities.Requisitions.Request;
using DOMAIN.Entities.Warehouses;
using INFRASTRUCTURE.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Authorization;

/// <summary>Requisitions, swaps and the warehouse tasks no page offers yet (audit phase 4).</summary>
public sealed class RequisitionSwapPermissionTests
{
    private sealed class Actor : ICurrentUserService
    {
        public Guid? UserId => Guid.Empty;
        public Guid? DepartmentId => null;
        public string DepartmentType => string.Empty;
    }

    private static ApplicationDbContext Db() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, new Actor());

    [Fact]
    public async Task Raising_a_requisition_needs_the_key_for_what_is_raised()
    {
        var db = Db();
        var api = new Material { Id = Guid.NewGuid(), Code = "RM-1", Name = "Paracetamol", Kind = MaterialKind.Raw };
        var foil = new Material { Id = Guid.NewGuid(), Code = "PM-1", Name = "Foil", Kind = MaterialKind.Package };
        db.Materials.AddRange(api, foil);
        await db.SaveChangesAsync();
        var purchase = new CreateRequisitionRequest { RequisitionType = RequisitionType.Purchase, Items = [new() { MaterialId = api.Id }, new() { MaterialId = foil.Id }] };
        Assert.Equal([PermissionKeys.CanCreatePackageMaterialRequisitions, PermissionKeys.CanCreateRawMaterialRequisitions],
            (await RequisitionCreatePermissionAttribute.Keys(db, purchase)).Order());
        Assert.Equal([PermissionKeys.CanUpdateProductionStep],
            await RequisitionCreatePermissionAttribute.Keys(db, new CreateRequisitionRequest { RequisitionType = RequisitionType.Stock, Items = [new() { MaterialId = api.Id }] }));
        Assert.Equal([PermissionKeys.CanCreateRndTrialBatch], await RequisitionCreatePermissionAttribute.Keys(db, new CreateRequisitionRequest { RequisitionType = RequisitionType.Trial }));
        Assert.Single(typeof(RequisitionController).GetMethod("CreateRequisition")!.GetCustomAttributes<RequisitionCreatePermissionAttribute>());
    }

    [Fact]
    public async Task A_swap_names_the_kinds_of_the_batches_it_swaps()
    {
        var db = Db();
        var foil = new Material { Id = Guid.NewGuid(), Code = "PM-1", Name = "Foil", Kind = MaterialKind.Package };
        var batch = new MaterialBatch { Id = Guid.NewGuid(), MaterialId = foil.Id };
        var swap = new SwapRequest { Id = Guid.NewGuid(), FirstSwapShelfMaterialBatches = [new SwapShelfMaterialBatch { MaterialBatchId = batch.Id }] };
        db.Materials.Add(foil);
        db.MaterialBatches.Add(batch);
        db.SwapRequests.Add(swap);
        await db.SaveChangesAsync();
        Assert.Equal([MaterialKind.Package], await MaterialKindPermissionAttribute.Kinds(db, new Dictionary<string, object> { ["id"] = swap.Id }, idIsSwapRequest: true));
        Assert.Empty(await MaterialKindPermissionAttribute.Kinds(db, new Dictionary<string, object> { ["id"] = swap.Id }));
        Assert.Equal([MaterialKind.Package], await MaterialKindPermissionAttribute.Kinds(db, new Dictionary<string, object>
        {
            ["request"] = new CreateSwapRequest { SecondSwapShelfMaterialBatches = [new CreateSwapShelfMaterialBatch { MaterialBatchId = batch.Id }] },
        }));
        foreach (var action in new[] { "ApproveSwapRequest", "RejectSwapRequest" })
            Assert.True(Assert.Single(typeof(WarehouseController).GetMethod(action)!.GetCustomAttributes<MaterialKindPermissionAttribute>()).IdIsSwapRequest);
    }

    [Theory]
    [InlineData(typeof(WarehouseController), "ConfirmArrival", PermissionKeys.CanConfirmMaterialArrival)]
    [InlineData(typeof(MaterialController), "ImportMaterialBatchesFromCsv", PermissionKeys.CanImportMaterialBatches)]
    [InlineData(typeof(MaterialController), "ResolveInconsistencies", PermissionKeys.CanFixStockInconsistencies)]
    [InlineData(typeof(MaterialController), "RequestRetest", PermissionKeys.CanRequestMaterialBatchRetest)]
    [InlineData(typeof(RequisitionController), "DeleteSourceRequisition", PermissionKeys.CanDeleteSourceRequisition)]
    public void A_task_no_page_offers_has_a_key_of_its_own(Type controller, string action, string key)
    {
        Assert.Equal(key, Assert.Single(controller.GetMethod(action)!.GetCustomAttributes<AuthorizeAttribute>()).Policy);
        Assert.Contains(key, PermissionUtils.GeneratePermissions().Select(permission => permission.Key));
    }
}
