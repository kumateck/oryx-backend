using System.Reflection;
using API.Authorization;
using API.Controllers;
using APP.Utils;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Materials;
using INFRASTRUCTURE.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Authorization;

/// <summary>Reference lists, equipment and invoices: each write needs its own key on the server (audit phase 4).</summary>
public sealed class CollectionPermissionTests
{
    private sealed class Actor : ICurrentUserService
    {
        public Guid? UserId => Guid.Empty;
        public Guid? DepartmentId => null;
        public string DepartmentType => string.Empty;
    }

    private static ApplicationDbContext Db() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, new Actor());

    [Theory]
    [InlineData(CollectionTask.Create, "ProductCategory", "CanCreateProductCategory")]
    [InlineData(CollectionTask.Edit, "Site", "CanEditWorkSite")]
    [InlineData(CollectionTask.Delete, "TermsOfPayment", "CanDeletePaymentTerm")]
    [InlineData(CollectionTask.Edit, "Currency", PermissionKeys.CanManageOtherCollectionTypes)]
    public async Task A_collection_write_needs_the_key_of_its_list(CollectionTask task, string type, string key) =>
        Assert.Equal([key], await new CollectionPermissionAttribute(task).Needed(Db(), new Dictionary<string, object?> { ["itemType"] = type }));

    [Fact]
    public async Task Material_categories_use_the_raw_or_packaging_key()
    {
        var db = Db();
        var foil = new MaterialCategory { Id = Guid.NewGuid(), Name = "Foil", MaterialKind = MaterialKind.Package };
        db.MaterialCategories.Add(foil);
        await db.SaveChangesAsync();
        Assert.Equal(["CanDeletePackageCategory"], await new CollectionPermissionAttribute(CollectionTask.Delete).Needed(db,
            new Dictionary<string, object?> { ["itemType"] = "MaterialCategory", ["itemId"] = foil.Id }));
        Assert.Equal(["CanCreateRawCategory"], await new CollectionPermissionAttribute(CollectionTask.Create).Needed(db,
            new Dictionary<string, object?> { ["itemType"] = "MaterialCategory", ["request"] = new CreateItemRequest { MaterialKind = MaterialKind.Raw } }));
        // Moving a category to the other kind needs both keys.
        Assert.Equal(2, (await new CollectionPermissionAttribute(CollectionTask.Edit).Needed(db, new Dictionary<string, object?>
        {
            ["itemType"] = "MaterialCategory", ["itemId"] = foil.Id, ["request"] = new CreateItemRequest { MaterialKind = MaterialKind.Raw },
        })).Count);
    }

    [Theory]
    [InlineData(typeof(CollectionController), "DeleteUoM", "CanDeleteUnitOfMeasure")]
    [InlineData(typeof(ProductController), "UpdateEquipment", "CanEditEquipmentDetails")]
    [InlineData(typeof(ProductController), "UploadProductStock", PermissionKeys.CanUploadProductStock)]
    [InlineData(typeof(ProcurementController), "DeletePurchaseOrderInvoice", PermissionKeys.CanDeletePurchaseOrderInvoice)]
    public void Units_equipment_and_invoices_need_their_task_key(Type controller, string action, string key) =>
        Assert.Equal(key, Assert.Single(controller.GetMethod(action)!.GetCustomAttributes<AuthorizeAttribute>()).Policy);

    [Fact]
    public void Vendor_writes_need_the_view_and_task_keys()
    {
        var keys = typeof(VendorController).GetMethod("DeleteVendor")!.GetCustomAttributes<AuthorizeAttribute>().Select(attribute => attribute.Policy).Order();
        Assert.Equal(["CanDeleteVendor", "CanViewVendors"], keys);
    }
}
