using System.Reflection;
using API.Authorization;
using API.Controllers;
using APP.Services.Authorization;
using APP.Utils;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Permissions;
using DOMAIN.Entities.Products;
using DOMAIN.Entities.Roles;
using DOMAIN.Entities.Users;
using INFRASTRUCTURE.Context;
using INFRASTRUCTURE.Migrations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Authorization;

/// <summary>Products: one key per task and section, per context, and bulk saves need a key for each kind of change.</summary>
public sealed class ProductPermissionTests
{
    private sealed class Actor(Guid id) : ICurrentUserService
    {
        public Guid? UserId => id;
        public Guid? DepartmentId => null;
        public string DepartmentType => string.Empty;
    }

    private static int _claimId = 5000;

    private static (ApplicationDbContext Db, Guid User) Seed(params string[] keys)
    {
        var user = Guid.NewGuid();
        var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, new Actor(user));
        var role = new Role { Id = Guid.NewGuid(), Name = "role", NormalizedName = "ROLE", DisplayName = "role" };
        db.Roles.Add(role);
        db.Users.Add(new User { Id = user, UserName = "u", Email = "u@example.com", FirstName = "U", LastName = "U" });
        db.UserRoles.Add(new IdentityUserRole<Guid> { UserId = user, RoleId = role.Id });
        foreach (var key in keys)
        {
            var claim = new IdentityRoleClaim<Guid> { Id = ++_claimId, RoleId = role.Id, ClaimType = AppConstants.Permission, ClaimValue = key };
            db.RoleClaims.Add(claim);
            db.PermissionTypes.Add(new PermissionType { Id = Guid.NewGuid(), Key = key, RoleClaimId = claim.Id, Type = "Access" });
        }
        db.SaveChanges();
        return (db, user);
    }

    /// <summary>Runs the attribute as MVC would and reports whether the action was allowed to run.</summary>
    private static async Task<(bool Ran, IActionResult Result)> Run(ApplicationDbContext db, Guid user, ProductPermissionAttribute attribute,
        Dictionary<string, object> arguments, string context = null)
    {
        var http = new DefaultHttpContext { RequestServices = new ServiceCollection().AddSingleton(db).BuildServiceProvider() };
        http.Items["Sub"] = user.ToString();
        if (context is not null) http.Request.QueryString = new QueryString($"?context={context}");
        var action = new ActionContext(http, new RouteData(), new ActionDescriptor());
        var executing = new ActionExecutingContext(action, new List<IFilterMetadata>(), arguments!, controller: null!);
        var ran = false;
        await attribute.OnActionExecutionAsync(executing, () =>
        {
            ran = true;
            return Task.FromResult(new ActionExecutedContext(action, new List<IFilterMetadata>(), controller: null!));
        });
        return (ran, executing.Result);
    }

    [Fact]
    public void Section_changes_are_classified_by_row()
    {
        (string, string)[] stored = [("a", "1"), ("b", "2")];
        Assert.Equal([ProductVerb.Add], ProductSectionChanges.Compare(stored, [("a", "1"), ("b", "2"), ("c", "3")]));
        Assert.Equal([ProductVerb.Delete], ProductSectionChanges.Compare(stored, [("a", "1")]));
        Assert.Equal([ProductVerb.Edit], ProductSectionChanges.Compare(stored, [("a", "1"), ("b", "9")]));
        Assert.Equal(new[] { ProductVerb.Add, ProductVerb.Delete, ProductVerb.Edit }.Order(), ProductSectionChanges.Compare(stored, [("a", "9"), ("c", "3")]).Order());
        // Saving unchanged rows is still a write.
        Assert.Equal([ProductVerb.Edit], ProductSectionChanges.Compare(stored, stored));
    }

    private static CreateProductPackageRequest Row(Guid material, decimal quantity) => new() { MaterialId = material, BaseQuantity = quantity };

    [Fact]
    public async Task A_packing_material_save_needs_the_key_for_each_kind_of_change_in_its_context()
    {
        var (db, user) = Seed(PermissionKeys.CanAddProductionProductPackingMaterials);
        var product = Guid.NewGuid();
        var foil = Guid.NewGuid();
        db.ProductPackages.Add(new ProductPackage { Id = Guid.NewGuid(), ProductId = product, MaterialId = foil, BaseQuantity = 1 });
        await db.SaveChangesAsync();
        var save = new ProductPermissionAttribute(ProductSection.PackingMaterials, ProductVerb.Save);

        var adding = await Run(db, user, save, new() { ["productId"] = product, ["request"] = new List<CreateProductPackageRequest> { Row(foil, 1), Row(Guid.NewGuid(), 2) } });
        Assert.True(adding.Ran);

        var editing = await Run(db, user, save, new() { ["productId"] = product, ["request"] = new List<CreateProductPackageRequest> { Row(foil, 5) } });
        Assert.False(editing.Ran);
        var refusal = Assert.IsType<ObjectResult>(editing.Result);
        Assert.Equal(StatusCodes.Status403Forbidden, refusal.StatusCode);
        Assert.Contains(PermissionKeys.CanEditProductionProductPackingMaterials, System.Text.Json.JsonSerializer.Serialize(Assert.IsType<ProblemDetails>(refusal.Value).Extensions["errors"]));

        // Production keys grant nothing on the QA product pages.
        Assert.False((await Run(db, user, save, new() { ["productId"] = product, ["request"] = new List<CreateProductPackageRequest> { Row(foil, 1), Row(Guid.NewGuid(), 2) } }, "qa")).Ran);
    }

    [Fact]
    public async Task Single_tasks_need_their_own_key_and_qa_cannot_delete_products()
    {
        var (db, user) = Seed(PermissionKeys.CanEditProductionProductInfo, PermissionKeys.CanEditQaProductInfo);
        Assert.True((await Run(db, user, new(ProductSection.Info, ProductVerb.Edit), [])).Ran);
        Assert.True((await Run(db, user, new(ProductSection.Info, ProductVerb.Edit), [], "qa")).Ran);
        Assert.False((await Run(db, user, new(ProductSection.Info, ProductVerb.Delete), [])).Ran);
        var qaDelete = await Run(db, user, new(ProductSection.Info, ProductVerb.Delete), [], "qa");
        Assert.False(qaDelete.Ran);
        Assert.Null(ProductPermissions.Key(ProductContext.Qa, ProductSection.Info, ProductVerb.Delete));
    }

    [Fact]
    public void Every_product_write_endpoint_names_its_section_and_task()
    {
        var expected = new Dictionary<string, (ProductSection, ProductVerb)>
        {
            [nameof(ProductController.CreateProduct)] = (ProductSection.Info, ProductVerb.Create),
            [nameof(ProductController.UpdateProduct)] = (ProductSection.Info, ProductVerb.Edit),
            [nameof(ProductController.DeleteProduct)] = (ProductSection.Info, ProductVerb.Delete),
            [nameof(ProductController.CreateRoute)] = (ProductSection.Procedure, ProductVerb.Save),
            [nameof(ProductController.DeleteRoute)] = (ProductSection.Procedure, ProductVerb.Delete),
            [nameof(ProductController.CreateProductPackage)] = (ProductSection.PackingMaterials, ProductVerb.Save),
            [nameof(ProductController.CreateProductPacking)] = (ProductSection.PackingStyles, ProductVerb.Save),
            [nameof(ProductController.DeleteProductPackage)] = (ProductSection.PackingMaterials, ProductVerb.Delete),
            [nameof(ProductController.ArchiveBillOfMaterial)] = (ProductSection.Bom, ProductVerb.Archive),
            [nameof(ProductController.UploadProducts)] = (ProductSection.List, ProductVerb.Import),
            [nameof(ProductController.UploadProductBom)] = (ProductSection.Bom, ProductVerb.Import),
            [nameof(ProductController.UploadProductPackages)] = (ProductSection.PackingMaterials, ProductVerb.Import),
            [nameof(ProductController.DownloadProducts)] = (ProductSection.List, ProductVerb.Export),
        };
        foreach (var (name, (section, verb)) in expected)
        {
            var attribute = Assert.Single(typeof(ProductController).GetMethods().Where(method => method.Name == name)
                .SelectMany(method => method.GetCustomAttributes<ProductPermissionAttribute>()));
            Assert.Equal((section, verb), (attribute.Section, attribute.Verb));
        }
        var bom = typeof(BillOfMaterialController).GetMethods().SelectMany(method => method.GetCustomAttributes<ProductPermissionAttribute>()).ToList();
        Assert.Contains(bom, attribute => attribute is { Section: ProductSection.Bom, Verb: ProductVerb.Save });
        Assert.Contains(bom, attribute => attribute is { Section: ProductSection.Bom, Verb: ProductVerb.Delete });
    }

    [Fact]
    public void Every_new_product_key_is_registered_and_granted_to_the_roles_that_had_the_task_except_product_delete()
    {
        var registered = PermissionUtils.GeneratePermissions().Select(permission => permission.Key).ToHashSet();
        var granted = GrantProductSectionPermissions.Grants.Select(grant => grant.Key).ToHashSet();
        string[] viewOnly = [PermissionKeys.CanViewFgProducts, PermissionKeys.CanPrintProductionProduct, PermissionKeys.CanViewProductionProductSpecifications,
            PermissionKeys.CanViewProductionProductStps, PermissionKeys.CanViewProductionProductArds, PermissionKeys.CanViewQaProductSpecifications,
            PermissionKeys.CanViewQaProductStps, PermissionKeys.CanViewQaProductArds];
        foreach (var key in ProductPermissions.All.Concat(viewOnly))
        {
            Assert.Contains(key, registered);
            if (key != PermissionKeys.CanDeleteProductionProduct) Assert.Contains(key, granted);
        }
        Assert.DoesNotContain(PermissionKeys.CanDeleteProductionProduct, granted);
        Assert.All(GrantProductSectionPermissions.Grants, grant => Assert.Contains(grant.From, registered));
    }
}
