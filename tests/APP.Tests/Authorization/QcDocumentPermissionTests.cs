using System.Reflection;
using API.Authorization;
using API.Controllers;
using APP.Services.Authorization;
using APP.Utils;
using DOMAIN.Entities.MaterialARD;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.MaterialSpecifications;
using DOMAIN.Entities.MaterialStandardTestProcedures;
using DOMAIN.Entities.Permissions;
using DOMAIN.Entities.Roles;
using DOMAIN.Entities.Users;
using INFRASTRUCTURE.Context;
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

/// <summary>QC documents: each task has its key, and material documents use the key for the material's kind.</summary>
public sealed class QcDocumentPermissionTests
{
    private sealed class Actor(Guid id) : ICurrentUserService
    {
        public Guid? UserId => id;
        public Guid? DepartmentId => null;
        public string DepartmentType => string.Empty;
    }

    private sealed record Seeded(ApplicationDbContext Db, Guid User, Guid Raw, Guid Packaging, Guid RawStp, Guid PackagingStp, Guid PackagingSpec, Guid PackagingArd);

    private static int _claimId = 9000;

    private static Seeded Seed(params string[] keys)
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
        var rawStp = new MaterialStandardTestProcedure { Id = Guid.NewGuid(), StpNumber = "STP-R", MaterialId = raw.Id };
        var packagingStp = new MaterialStandardTestProcedure { Id = Guid.NewGuid(), StpNumber = "STP-P", MaterialId = packaging.Id };
        var spec = new MaterialSpecification { Id = Guid.NewGuid(), SpecificationNumber = "SP-P", MaterialId = packaging.Id };
        var ard = new MaterialAnalyticalRawData { Id = Guid.NewGuid(), SpecNumber = "SP-P", StpId = packagingStp.Id };
        db.Materials.AddRange(raw, packaging);
        db.MaterialStandardTestProcedures.AddRange(rawStp, packagingStp);
        db.MaterialSpecifications.Add(spec);
        db.MaterialAnalyticalRawData.Add(ard);
        db.SaveChanges();
        return new(db, user, raw.Id, packaging.Id, rawStp.Id, packagingStp.Id, spec.Id, ard.Id);
    }

    private static async Task<bool> Allowed(Seeded seeded, QcDocument document, QcTask task, Dictionary<string, object> arguments)
    {
        var http = new DefaultHttpContext { RequestServices = new ServiceCollection().AddSingleton(seeded.Db).BuildServiceProvider() };
        http.Items["Sub"] = seeded.User.ToString();
        var action = new ActionContext(http, new RouteData(), new ActionDescriptor());
        var executing = new ActionExecutingContext(action, new List<IFilterMetadata>(), arguments!, controller: null!);
        var ran = false;
        await new QcDocumentPermissionAttribute(document, task).OnActionExecutionAsync(executing, () =>
        {
            ran = true;
            return Task.FromResult(new ActionExecutedContext(action, new List<IFilterMetadata>(), controller: null!));
        });
        if (!ran) Assert.Equal(StatusCodes.Status403Forbidden, Assert.IsType<ObjectResult>(executing.Result).StatusCode);
        return ran;
    }

    [Fact]
    public async Task Stored_documents_use_their_material_kind()
    {
        var seeded = Seed(PermissionKeys.CanEditRawMaterialStp, PermissionKeys.CanDeleteRawMaterialArd, PermissionKeys.CanEditRawMaterialSpecification);
        Assert.True(await Allowed(seeded, QcDocument.MaterialStp, QcTask.Edit, new() { ["id"] = seeded.RawStp }));
        Assert.False(await Allowed(seeded, QcDocument.MaterialStp, QcTask.Edit, new() { ["id"] = seeded.PackagingStp }));
        // An ARD's kind is its STP's material.
        Assert.False(await Allowed(seeded, QcDocument.MaterialArd, QcTask.Delete, new() { ["id"] = seeded.PackagingArd }));
        Assert.False(await Allowed(seeded, QcDocument.MaterialSpecification, QcTask.Edit, new() { ["id"] = seeded.PackagingSpec }));
    }

    [Fact]
    public async Task Creating_and_relinking_need_the_key_for_every_material_involved()
    {
        var seeded = Seed(PermissionKeys.CanCreateRawMaterialArd, PermissionKeys.CanEditRawMaterialSpecification, PermissionKeys.CanCreatePackagingMaterialStp);
        Assert.True(await Allowed(seeded, QcDocument.MaterialArd, QcTask.Create,
            new() { ["request"] = new CreateMaterialAnalyticalRawDataRequest { StpId = seeded.RawStp, MaterialId = seeded.Raw } }));
        Assert.False(await Allowed(seeded, QcDocument.MaterialArd, QcTask.Create,
            new() { ["request"] = new CreateMaterialAnalyticalRawDataRequest { StpId = seeded.PackagingStp, MaterialId = seeded.Raw } }));
        Assert.True(await Allowed(seeded, QcDocument.MaterialSpecification, QcTask.Edit,
            new() { ["request"] = new AddRemoveMaterialToSpecificationRequest { MaterialIdsToAdd = [seeded.Raw] } }));
        Assert.False(await Allowed(seeded, QcDocument.MaterialSpecification, QcTask.Edit,
            new() { ["request"] = new AddRemoveMaterialToSpecificationRequest { MaterialIdsToAdd = [seeded.Raw], MaterialIdsToRemove = [seeded.Packaging] } }));
        Assert.True(await Allowed(seeded, QcDocument.MaterialStp, QcTask.Create,
            new() { ["request"] = new CreateMaterialStandardTestProcedureRequest { MaterialIds = [seeded.Packaging] } }));
        Assert.False(await Allowed(seeded, QcDocument.MaterialStp, QcTask.Create,
            new() { ["request"] = new CreateMaterialStandardTestProcedureRequest { MaterialIds = [seeded.Packaging, seeded.Raw] } }));
    }

    [Fact]
    public async Task Product_documents_need_their_own_key()
    {
        var seeded = Seed(PermissionKeys.CanEditProductStp);
        Assert.True(await Allowed(seeded, QcDocument.ProductStp, QcTask.Edit, new() { ["id"] = Guid.NewGuid() }));
        Assert.False(await Allowed(seeded, QcDocument.ProductStp, QcTask.Delete, new() { ["id"] = Guid.NewGuid() }));
        Assert.False(await Allowed(seeded, QcDocument.ProductArd, QcTask.Create, []));
    }

    [Fact]
    public void Every_qc_document_write_names_its_task_and_every_key_is_registered()
    {
        var expected = new (Type Controller, string Action, QcDocument Document, QcTask Task)[]
        {
            (typeof(MaterialStandardTestProcedureController), "CreateStandardTestProcedure", QcDocument.MaterialStp, QcTask.Create),
            (typeof(MaterialStandardTestProcedureController), "AddRemoveMaterialsToStp", QcDocument.MaterialStp, QcTask.Edit),
            (typeof(MaterialStandardTestProcedureController), "DeleteStandardTestProcedure", QcDocument.MaterialStp, QcTask.Delete),
            (typeof(MaterialSpecificationController), "ImportMaterialSpecification", QcDocument.MaterialSpecification, QcTask.Create),
            (typeof(MaterialSpecificationController), "DeleteMaterialSpecification", QcDocument.MaterialSpecification, QcTask.Delete),
            (typeof(MaterialAnalyticalRawDataController), "UpdateAnalyticalRawData", QcDocument.MaterialArd, QcTask.Edit),
            (typeof(ProductStandardTestProcedureController), "AddRemoveProductsToStp", QcDocument.ProductStp, QcTask.Edit),
            (typeof(ProductSpecificationController), "CreateProductSpecification", QcDocument.ProductSpecification, QcTask.Create),
            (typeof(ProductAnalyticalRawDataController), "DeleteAnalyticRawData", QcDocument.ProductArd, QcTask.Delete),
        };
        foreach (var (controller, action, document, task) in expected)
        {
            var attribute = Assert.Single(controller.GetMethods().Where(method => method.Name == action)
                .SelectMany(method => method.GetCustomAttributes<QcDocumentPermissionAttribute>()));
            Assert.Equal((document, task), (attribute.Document, attribute.Task));
        }
        var registered = PermissionUtils.GeneratePermissions().Select(permission => permission.Key).ToHashSet();
        Assert.All(QcDocumentPermissions.All, key => Assert.Contains(key, registered));
    }
}
