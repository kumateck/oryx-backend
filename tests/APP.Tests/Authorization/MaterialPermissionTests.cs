using System.Reflection;
using API.Authorization;
using API.Controllers;
using APP.Services.Authorization;
using APP.Utils;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Permissions;
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

/// <summary>Materials: raw and packaging keys are checked against the stored material, never only the request.</summary>
public sealed class MaterialPermissionTests
{
    private sealed class Actor(Guid id) : ICurrentUserService
    {
        public Guid? UserId => id;
        public Guid? DepartmentId => null;
        public string DepartmentType => string.Empty;
    }

    private static int _claimId = 7000;

    private static (ApplicationDbContext Db, Guid User, Guid Raw, Guid Packaging) Seed(params string[] keys)
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
        var raw = new Material { Id = Guid.NewGuid(), Code = "RM-1", Name = "Paracetamol", Kind = MaterialKind.Raw };
        var packaging = new Material { Id = Guid.NewGuid(), Code = "PM-1", Name = "Foil", Kind = MaterialKind.Package };
        db.Materials.AddRange(raw, packaging);
        db.SaveChanges();
        return (db, user, raw.Id, packaging.Id);
    }

    private static async Task<bool> Allowed(ApplicationDbContext db, Guid user, MaterialTask task, Dictionary<string, object> arguments)
    {
        var http = new DefaultHttpContext { RequestServices = new ServiceCollection().AddSingleton(db).BuildServiceProvider() };
        http.Items["Sub"] = user.ToString();
        var action = new ActionContext(http, new RouteData(), new ActionDescriptor());
        var executing = new ActionExecutingContext(action, new List<IFilterMetadata>(), arguments!, controller: null!);
        var ran = false;
        await new MaterialPermissionAttribute(task).OnActionExecutionAsync(executing, () =>
        {
            ran = true;
            return Task.FromResult(new ActionExecutedContext(action, new List<IFilterMetadata>(), controller: null!));
        });
        if (!ran) Assert.Equal(StatusCodes.Status403Forbidden, Assert.IsType<ObjectResult>(executing.Result).StatusCode);
        return ran;
    }

    [Fact]
    public async Task Record_endpoints_check_the_stored_kind_not_the_request()
    {
        var (db, user, raw, packaging) = Seed(PermissionKeys.CanEditRawMaterials, PermissionKeys.CanDeleteRawMaterials);
        Assert.True(await Allowed(db, user, MaterialTask.Edit, new() { ["materialId"] = raw, ["request"] = new CreateMaterialRequest { Kind = MaterialKind.Raw } }));
        // A request that claims "raw" cannot reach a stored packaging material.
        Assert.False(await Allowed(db, user, MaterialTask.Edit, new() { ["materialId"] = packaging, ["request"] = new CreateMaterialRequest { Kind = MaterialKind.Raw } }));
        Assert.True(await Allowed(db, user, MaterialTask.Delete, new() { ["materialId"] = raw }));
        Assert.False(await Allowed(db, user, MaterialTask.Delete, new() { ["materialId"] = packaging }));
        // Changing a raw material into packaging needs the packaging key too.
        Assert.False(await Allowed(db, user, MaterialTask.Edit, new() { ["materialId"] = raw, ["request"] = new CreateMaterialRequest { Kind = MaterialKind.Package } }));
    }

    [Fact]
    public async Task Creating_uploading_and_linking_need_the_key_for_every_kind_involved()
    {
        var (db, user, raw, packaging) = Seed(PermissionKeys.CanCreateNewPackagingMaterials, PermissionKeys.CanLinkRawMaterials);
        Assert.True(await Allowed(db, user, MaterialTask.Create, new() { ["request"] = new CreateMaterialRequest { Kind = MaterialKind.Package } }));
        Assert.False(await Allowed(db, user, MaterialTask.Create, new() { ["request"] = new CreateMaterialRequest { Kind = MaterialKind.Raw } }));
        Assert.False(await Allowed(db, user, MaterialTask.Create, new() { ["kind"] = MaterialKind.Raw }));
        Assert.True(await Allowed(db, user, MaterialTask.Link, new() { ["materialDepartments"] = new List<CreateMaterialDepartment> { new() { MaterialId = raw } } }));
        Assert.False(await Allowed(db, user, MaterialTask.Link, new() { ["materialDepartments"] = new List<CreateMaterialDepartment> { new() { MaterialId = raw }, new() { MaterialId = packaging } } }));
        Assert.False(await Allowed(db, user, MaterialTask.Unlink, new() { ["materialId"] = raw }));
    }

    [Fact]
    public void Every_material_master_write_names_its_task()
    {
        var expected = new Dictionary<string, MaterialTask>
        {
            [nameof(MaterialController.CreateMaterial)] = MaterialTask.Create,
            [nameof(MaterialController.UpdateMaterial)] = MaterialTask.Edit,
            [nameof(MaterialController.UpdateReOrderLevel)] = MaterialTask.Edit,
            [nameof(MaterialController.DeleteMaterial)] = MaterialTask.Delete,
            [nameof(MaterialController.UploadMaterials)] = MaterialTask.Create,
            [nameof(MaterialController.CreateMaterialDepartment)] = MaterialTask.Link,
            [nameof(MaterialController.RemoveMaterialDepartment)] = MaterialTask.Unlink,
        };
        foreach (var (name, task) in expected)
            Assert.Equal(task, Assert.Single(typeof(MaterialController).GetMethods().Where(method => method.Name == name)
                .SelectMany(method => method.GetCustomAttributes<MaterialPermissionAttribute>())).Task);
        var registered = PermissionUtils.GeneratePermissions().Select(permission => permission.Key).ToHashSet();
        Assert.All(MaterialPermissions.All, key => Assert.Contains(key, registered));
    }

    [Fact]
    public void Production_requisition_views_are_their_own_keys_and_granted_to_the_roles_that_had_them()
    {
        Assert.NotEqual(PermissionKeys.CanViewRawMaterialRequisitions, PermissionKeys.CanViewRawMaterialRequisitionsForProduction);
        Assert.NotEqual(PermissionKeys.CanViewPackagingMaterialRequisitions, PermissionKeys.CanViewPackagingMaterialRequisitionsForProduction);
        var catalog = PermissionUtils.GeneratePermissions().ToList();
        Assert.Single(catalog, permission => permission.Key == PermissionKeys.CanViewRawMaterialRequisitions);
        Assert.Single(catalog, permission => permission.Key == PermissionKeys.CanViewRawMaterialRequisitionsForProduction && permission.Module == PermissionModules.Production);
        Assert.Contains(GrantProductionRequisitionViews.Grants, grant =>
            grant == (PermissionKeys.CanViewRawMaterialRequisitionsForProduction, PermissionKeys.CanViewRawMaterialRequisitions));
        Assert.Contains(GrantProductionRequisitionViews.Grants, grant =>
            grant == (PermissionKeys.CanViewPackagingMaterialRequisitionsForProduction, PermissionKeys.CanViewPackagingMaterialRequisitions));
    }
}
