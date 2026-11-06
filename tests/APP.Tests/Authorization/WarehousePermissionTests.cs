using System.Reflection;
using API.Authorization;
using API.Controllers;
using APP.Utils;
using DOMAIN.Entities.Checklists;
using DOMAIN.Entities.Grns;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.Permissions;
using DOMAIN.Entities.Requisitions;
using DOMAIN.Entities.Roles;
using DOMAIN.Entities.Users;
using DOMAIN.Entities.Warehouses;
using INFRASTRUCTURE.Context;
using Microsoft.AspNetCore.Authorization;
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

/// <summary>Warehouse structure and receiving: each task has its key, and receiving uses the materials' kind.</summary>
public sealed class WarehousePermissionTests
{
    private sealed class Actor(Guid id) : ICurrentUserService
    {
        public Guid? UserId => id;
        public Guid? DepartmentId => null;
        public string DepartmentType => string.Empty;
    }

    private static int _claimId = 11000;

    [Fact]
    public void Warehouse_structure_writes_name_their_own_key()
    {
        var expected = new Dictionary<string, string>
        {
            [nameof(WarehouseController.CreateWarehouse)] = PermissionKeys.CanCreateWarehouse,
            [nameof(WarehouseController.UpdateWarehouse)] = PermissionKeys.CanEditWarehouse,
            [nameof(WarehouseController.DeleteWarehouse)] = PermissionKeys.CanDeleteWarehouse,
            [nameof(WarehouseController.CreateWarehouseLocation)] = PermissionKeys.CanAddNewLocation,
            [nameof(WarehouseController.UpdateWarehouseLocation)] = PermissionKeys.CanEditLocation,
            [nameof(WarehouseController.DeleteWarehouseLocation)] = PermissionKeys.CanDeleteLocation,
            [nameof(WarehouseController.CreateWarehouseLocationRack)] = PermissionKeys.CanAddNewRack,
            [nameof(WarehouseController.UpdateWarehouseLocationRack)] = PermissionKeys.CanEditRack,
            [nameof(WarehouseController.DeleteWarehouseLocationRack)] = PermissionKeys.CanDeleteRack,
            [nameof(WarehouseController.CreateWarehouseLocationShelf)] = PermissionKeys.CanAddNewShelf,
            [nameof(WarehouseController.UpdateWarehouseLocationShelf)] = PermissionKeys.CanEditShelf,
            [nameof(WarehouseController.DeleteWarehouseLocationShelf)] = PermissionKeys.CanDeleteShelf,
        };
        foreach (var (name, key) in expected)
        {
            var method = Assert.Single(typeof(WarehouseController).GetMethods().Where(method => method.Name == name));
            Assert.Equal(key, Assert.Single(method.GetCustomAttributes<AuthorizeAttribute>()).Policy);
        }
        var checklist = typeof(WarehouseController).GetMethod(nameof(WarehouseController.CreateChecklist))!.GetCustomAttribute<MaterialKindPermissionAttribute>()!;
        Assert.Equal((PermissionKeys.CanCreateChecklistForRawMaterials, PermissionKeys.CanCreateChecklistForPackagingMaterials), (checklist.RawKey, checklist.PackagingKey));
        var grn = typeof(WarehouseController).GetMethod(nameof(WarehouseController.CreateGrn))!.GetCustomAttribute<MaterialKindPermissionAttribute>()!;
        Assert.Equal((PermissionKeys.CanCreateGrnForRawMaterials, PermissionKeys.CanCreateGrnForPackagingMaterials), (grn.RawKey, grn.PackagingKey));
    }

    [Fact]
    public async Task Receiving_uses_the_kind_of_the_batches_and_distributed_materials_named()
    {
        var user = Guid.NewGuid();
        await using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, new Actor(user));
        var role = new Role { Id = Guid.NewGuid(), Name = "role", NormalizedName = "ROLE", DisplayName = "role" };
        db.Roles.Add(role);
        db.Users.Add(new User { Id = user, UserName = "u", Email = "u@example.com", FirstName = "U", LastName = "U" });
        db.UserRoles.Add(new IdentityUserRole<Guid> { UserId = user, RoleId = role.Id });
        foreach (var key in new[] { PermissionKeys.CanCreateGrnForRawMaterials, PermissionKeys.CanCreateChecklistForPackagingMaterials })
        {
            var claim = new IdentityRoleClaim<Guid> { Id = ++_claimId, RoleId = role.Id, ClaimType = AppConstants.Permission, ClaimValue = key };
            db.RoleClaims.Add(claim);
            db.PermissionTypes.Add(new PermissionType { Id = Guid.NewGuid(), Key = key, RoleClaimId = claim.Id, Type = "Access" });
        }
        var raw = new Material { Id = Guid.NewGuid(), Code = "RM", Name = "Raw", Kind = MaterialKind.Raw };
        var packaging = new Material { Id = Guid.NewGuid(), Code = "PM", Name = "Foil", Kind = MaterialKind.Package };
        var rawBatch = new MaterialBatch { Id = Guid.NewGuid(), MaterialId = raw.Id, BatchNumber = "R1" };
        var packagingBatch = new MaterialBatch { Id = Guid.NewGuid(), MaterialId = packaging.Id, BatchNumber = "P1" };
        var distributed = new DistributedRequisitionMaterial { Id = Guid.NewGuid(), MaterialId = packaging.Id };
        db.Materials.AddRange(raw, packaging);
        db.MaterialBatches.AddRange(rawBatch, packagingBatch);
        db.DistributedRequisitionMaterials.Add(distributed);
        await db.SaveChangesAsync();

        async Task<bool> Allowed(MaterialKindPermissionAttribute attribute, object body)
        {
            var http = new DefaultHttpContext { RequestServices = new ServiceCollection().AddSingleton(db).BuildServiceProvider() };
            http.Items["Sub"] = user.ToString();
            var action = new ActionContext(http, new RouteData(), new ActionDescriptor());
            var executing = new ActionExecutingContext(action, new List<IFilterMetadata>(), new Dictionary<string, object> { ["request"] = body }!, controller: null!);
            var ran = false;
            await attribute.OnActionExecutionAsync(executing, () =>
            {
                ran = true;
                return Task.FromResult(new ActionExecutedContext(action, new List<IFilterMetadata>(), controller: null!));
            });
            if (!ran) Assert.Equal(StatusCodes.Status403Forbidden, Assert.IsType<ObjectResult>(executing.Result).StatusCode);
            return ran;
        }

        var grn = new MaterialKindPermissionAttribute(PermissionKeys.CanCreateGrnForRawMaterials, PermissionKeys.CanCreateGrnForPackagingMaterials);
        Assert.True(await Allowed(grn, new CreateGrnRequest { MaterialBatchIds = [rawBatch.Id] }));
        Assert.False(await Allowed(grn, new CreateGrnRequest { MaterialBatchIds = [rawBatch.Id, packagingBatch.Id] }));
        var checklist = new MaterialKindPermissionAttribute(PermissionKeys.CanCreateChecklistForRawMaterials, PermissionKeys.CanCreateChecklistForPackagingMaterials);
        Assert.True(await Allowed(checklist, new CreateChecklistRequest { DistributedRequisitionMaterialId = distributed.Id }));
        Assert.False(await Allowed(checklist, new CreateChecklistRequest { DistributedRequisitionMaterialId = distributed.Id, MaterialId = raw.Id }));
    }
}
