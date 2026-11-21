using APP.Mapper;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using APP.Repository;
using APP.Services.Authorization;
using APP.Tests.QcWorksheets;
using APP.Utils;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Departments;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Permissions;
using DOMAIN.Entities.Products;
using DOMAIN.Entities.Roles;
using DOMAIN.Entities.Users;
using DOMAIN.Entities.Warehouses;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SHARED;
using Xunit;
using System.Reflection;
using API.Authorization;
using API.Controllers;
using Microsoft.AspNetCore.Authorization;

namespace APP.Tests.Repository;

public class QaProductMaterialTests
{
    [Theory]
    [InlineData(nameof(MaterialController.GetUoMForMaterial))]
    [InlineData(nameof(MaterialController.GetMaterialDepartments))]
    public void Shared_product_material_reads_accept_both_contexts_and_require_authentication(string action)
    {
        var method = Assert.Single(typeof(MaterialController).GetMethods(), method => method.Name == action
            && method.GetParameters().Any(parameter => parameter.Name == "productId"));
        Assert.Empty(method.GetCustomAttributes<AllowAnonymousAttribute>());
        Assert.Empty(typeof(MaterialController).GetCustomAttributes<AllowAnonymousAttribute>());
        Assert.NotEmpty(typeof(MaterialController).GetCustomAttributes<AuthorizeAttribute>());
        var keys = Assert.Single(method.GetCustomAttributes<AnyPermissionAttribute>()).Keys;
        var catalog = PermissionUtils.GeneratePermissions().ToList();
        foreach (var key in new[] { PermissionKeys.CanViewQaProductBom, PermissionKeys.CanViewQaProductPackingMaterials,
            PermissionKeys.CanViewProductionProductBom, PermissionKeys.CanViewProductionProductPackingMaterials })
        {
            Assert.Contains(key, keys);
            Assert.Single(catalog, permission => permission.Key == key);
        }
    }

    [Fact]
    public async Task Product_context_uses_product_linkage_and_preserves_legacy_user_linkage()
    {
        using var f = new Fixture();
        await f.Seed();
        var own = await f.Repo.GetUnitOfMeasureForMaterialDepartment(f.Material.Id, f.Actor.Id);
        Assert.Equal("mg", own.Value.Symbol);
        var selected = await f.Repo.GetUnitOfMeasureForMaterialDepartment(f.Material.Id, f.Actor.Id, f.Product.Id, ProductContext.Qa);
        Assert.True(selected.IsSuccess, selected.Error?.Code + ": " + selected.Error?.Description);
        Assert.Equal("piece", selected.Value.Symbol);
        var rows = await f.Repo.GetMaterialDepartments(1, 20, null, MaterialKind.Raw, null, "", null, f.Actor.Id, f.Product.Id, ProductContext.Qa);
        Assert.True(rows.IsSuccess, rows.Error?.Description);
        Assert.Equal("piece", Assert.Single(rows.Value.Data).UoM.Symbol);
        Assert.Equal(f.Actor.DepartmentId, (await f.Db.Users.FindAsync(f.Actor.Id)).DepartmentId);
    }

    [Theory]
    [InlineData(PermissionKeys.CanViewQaProductBom)]
    public async Task Product_context_refuses_missing_or_revoked_live_grants(string revoked)
    {
        using var f = new Fixture(); await f.Seed();
        f.Db.PermissionTypes.RemoveRange(f.Db.PermissionTypes.Where(p => p.Key == revoked));
        await f.Db.SaveChangesAsync();
        var unit = await f.Repo.GetUnitOfMeasureForMaterialDepartment(f.Material.Id, f.Actor.Id, f.Product.Id, ProductContext.Qa);
        Assert.Equal(ErrorType.Forbidden, unit.Error.Type);
        var rows = await f.Repo.GetMaterialDepartments(1, 20, null, MaterialKind.Raw, null, "", null, f.Actor.Id, f.Product.Id, ProductContext.Qa);
        Assert.Equal(ErrorType.Forbidden, rows.Error.Type);
    }

    [Theory]
    [InlineData(MaterialKind.Raw, PermissionKeys.CanViewQaProductBom, ProductContext.Qa)]
    [InlineData(MaterialKind.Package, PermissionKeys.CanViewQaProductPackingMaterials, ProductContext.Qa)]
    [InlineData(MaterialKind.Raw, PermissionKeys.CanViewProductionProductBom, ProductContext.Production)]
    [InlineData(MaterialKind.Package, PermissionKeys.CanViewProductionProductPackingMaterials, ProductContext.Production)]
    public async Task Each_context_uses_its_own_view_key_without_legacy_or_edit_grants(MaterialKind kind, string key, ProductContext productContext)
    {
        using var f = new Fixture(); await f.Seed([key]);
        f.Material.Kind = kind;
        if (productContext == ProductContext.Production) f.Actor.Department = f.Production;
        f.Db.Warehouses.IgnoreQueryFilters().Single().Type = kind == MaterialKind.Raw ? WarehouseType.RawMaterialStorage : WarehouseType.PackagedStorage;
        await f.Db.SaveChangesAsync();
        var unit = await f.Repo.GetUnitOfMeasureForMaterialDepartment(f.Material.Id, f.Actor.Id, f.Product.Id, productContext);
        Assert.True(unit.IsSuccess, unit.Error?.Description);
        Assert.Equal("piece", unit.Value.Symbol);
        var rows = await f.Repo.GetMaterialDepartments(1, 20, null, kind, null, "", null, f.Actor.Id, f.Product.Id, productContext);
        Assert.True(rows.IsSuccess, rows.Error?.Description);
        Assert.Equal("piece", Assert.Single(rows.Value.Data).UoM.Symbol);
        // Removing the same live grant must immediately refuse both reads.
        f.Db.PermissionTypes.RemoveRange(f.Db.PermissionTypes.Where(p => p.Key == key));
        await f.Db.SaveChangesAsync();
        Assert.Equal(ErrorType.Forbidden, (await f.Repo.GetUnitOfMeasureForMaterialDepartment(f.Material.Id, f.Actor.Id, f.Product.Id, productContext)).Error.Type);
        Assert.Equal(ErrorType.Forbidden, (await f.Repo.GetMaterialDepartments(1, 20, null, kind, null, "", null, f.Actor.Id, f.Product.Id, productContext)).Error.Type);
    }

    [Theory]
    [InlineData(PermissionKeys.CanViewQaProductPackingMaterials)]
    [InlineData(PermissionKeys.CanViewQaProductInfo)]
    [InlineData(PermissionKeys.CanEditQaProductBom)]
    [InlineData(PermissionKeys.CanViewProductionProductPackingMaterials)]
    [InlineData(PermissionKeys.CanViewProductionProductBom)]
    public async Task An_unrelated_key_does_not_open_raw_product_materials(string key)
    {
        using var f = new Fixture(); await f.Seed([key]);
        Assert.Equal(ErrorType.Forbidden, (await f.Repo.GetUnitOfMeasureForMaterialDepartment(f.Material.Id, f.Actor.Id, f.Product.Id, ProductContext.Qa)).Error.Type);
        Assert.Equal(ErrorType.Forbidden, (await f.Repo.GetMaterialDepartments(1, 20, null, MaterialKind.Raw, null, "", null, f.Actor.Id, f.Product.Id, ProductContext.Qa)).Error.Type);
    }

    [Theory]
    [InlineData(MaterialKind.Raw, PermissionKeys.CanViewQaProductBom)]
    [InlineData(MaterialKind.Package, PermissionKeys.CanViewQaProductPackingMaterials)]
    public async Task Qa_keys_do_not_open_production_product_materials(MaterialKind kind, string key)
    {
        using var f = new Fixture(); await f.Seed([key]);
        f.Actor.Department = f.Production; f.Material.Kind = kind; await f.Db.SaveChangesAsync();
        Assert.Equal(ErrorType.Forbidden, (await f.Repo.GetUnitOfMeasureForMaterialDepartment(f.Material.Id, f.Actor.Id, f.Product.Id)).Error.Type);
        Assert.Equal(ErrorType.Forbidden, (await f.Repo.GetMaterialDepartments(1, 20, null, kind, null, "", null, f.Actor.Id, f.Product.Id)).Error.Type);
    }

    [Fact]
    public async Task A_bom_view_does_not_open_packaging_or_an_unfiltered_picker()
    {
        using var f = new Fixture(); await f.Seed([PermissionKeys.CanViewQaProductBom]);
        f.Material.Kind = MaterialKind.Package; await f.Db.SaveChangesAsync();
        Assert.Equal(ErrorType.Forbidden, (await f.Repo.GetUnitOfMeasureForMaterialDepartment(f.Material.Id, f.Actor.Id, f.Product.Id, ProductContext.Qa)).Error.Type);
        Assert.Equal(ErrorType.Forbidden, (await f.Repo.GetMaterialDepartments(1, 20, null, MaterialKind.Package, null, "", null, f.Actor.Id, f.Product.Id, ProductContext.Qa)).Error.Type);
        Assert.Equal(ErrorType.Forbidden, (await f.Repo.GetMaterialDepartments(1, 20, null, null, null, "", null, f.Actor.Id, f.Product.Id, ProductContext.Qa)).Error.Type);
    }

    [Fact]
    public async Task No_grants_or_only_legacy_production_grants_do_not_open_product_scoped_materials()
    {
        foreach (var keys in new[] { Array.Empty<string>(), new[] { PermissionKeys.CanViewPlannedProducts, PermissionKeys.CanEditProductionPlan } })
        {
            using var f = new Fixture(); await f.Seed(keys);
            Assert.Equal(ErrorType.Forbidden, (await f.Repo.GetUnitOfMeasureForMaterialDepartment(f.Material.Id, f.Actor.Id, f.Product.Id, ProductContext.Qa)).Error.Type);
            Assert.Equal(ErrorType.Forbidden, (await f.Repo.GetMaterialDepartments(1, 20, null, MaterialKind.Raw, null, "", null, f.Actor.Id, f.Product.Id, ProductContext.Qa)).Error.Type);
        }
    }

    [Fact]
    public async Task Archived_product_and_nonproduction_department_cannot_supply_a_product_linkage()
    {
        using var f = new Fixture(); await f.Seed();
        f.Product.DeletedAt = DateTime.UtcNow; await f.Db.SaveChangesAsync();
        Assert.Equal(ErrorType.NotFound, (await f.Repo.GetUnitOfMeasureForMaterialDepartment(f.Material.Id, f.Actor.Id, f.Product.Id, ProductContext.Qa)).Error.Type);
        f.Product.DeletedAt = null; f.Production.Type = DepartmentType.NonProduction; await f.Db.SaveChangesAsync();
        Assert.Equal(ErrorType.NotFound, (await f.Repo.GetUnitOfMeasureForMaterialDepartment(f.Material.Id, f.Actor.Id, f.Product.Id, ProductContext.Qa)).Error.Type);
    }

    [Fact]
    public async Task Removed_department_link_never_falls_back_to_the_callers_unit()
    {
        using var f = new Fixture(); await f.Seed();
        f.Db.MaterialDepartments.Single(link => link.DepartmentId == f.Production.Id).DeletedAt = DateTime.UtcNow;
        await f.Db.SaveChangesAsync();
        var unit = await f.Repo.GetUnitOfMeasureForMaterialDepartment(f.Material.Id, f.Actor.Id, f.Product.Id, ProductContext.Qa);
        Assert.True(unit.IsFailure);
        var rows = await f.Repo.GetMaterialDepartments(1, 20, null, MaterialKind.Raw, null, "", null, f.Actor.Id, f.Product.Id, ProductContext.Qa);
        Assert.True(rows.IsSuccess);
        Assert.Empty(rows.Value.Data);
        Assert.Equal("mg", (await f.Repo.GetUnitOfMeasureForMaterialDepartment(f.Material.Id, f.Actor.Id)).Value.Symbol);
    }

    private sealed class Fixture : IDisposable
    {
        private readonly QcWorksheetTestContext harness = new();
        internal INFRASTRUCTURE.Context.ApplicationDbContext Db => harness.Db;
        private readonly ServiceProvider mapping;
        internal Fixture()
        {
            var services = new ServiceCollection();
            services.AddLogging(); services.AddSingleton(Db);
            services.AddSingleton<IHttpContextAccessor, NullHttpContextAccessor>();
            services.AddAutoMapper(cfg => { }, typeof(OryxMapper));
            mapping = services.BuildServiceProvider();
        }
        internal MaterialRepository Repo => new(Db, mapping.GetRequiredService<IMapper>());
        internal Department Production = new() { Id = Guid.NewGuid(), Name = "Tablet", Type = DepartmentType.Production };
        internal User Actor = new() { Id = Guid.NewGuid(), UserName = "QA", Department = new() { Id = Guid.NewGuid(), Name = "QA", Type = DepartmentType.NonProduction } };
        internal Material Material = new() { Id = Guid.NewGuid(), Name = "Shells", Code = "RMG033", Kind = MaterialKind.Raw };
        internal Product Product;
        internal async Task Seed(string[]? keys = null)
        {
            var mg = new UnitOfMeasure { Id = Guid.NewGuid(), Name = "Milligram", Symbol = "mg" };
            var piece = new UnitOfMeasure { Id = Guid.NewGuid(), Name = "Piece", Symbol = "piece" };
            Product = new() { Id = Guid.NewGuid(), Name = "Zinvite", Department = Production, BaseUoM = mg };
            var role = new Role { Id = Guid.NewGuid(), Name = "QA products fixture" };
            Db.Users.Add(Actor); Db.Products.Add(Product); Db.Roles.Add(role);
            Db.UserRoles.Add(new IdentityUserRole<Guid> { UserId = Actor.Id, RoleId = role.Id });
            var id = 991;
            foreach (var key in keys ?? [PermissionKeys.CanViewQaProductBom, PermissionKeys.CanViewQaProductPackingMaterials])
            {
                Db.RoleClaims.Add(new IdentityRoleClaim<Guid> { Id = id, RoleId = role.Id, ClaimType = AppConstants.Permission, ClaimValue = key });
                Db.PermissionTypes.Add(new PermissionType { Id = Guid.NewGuid(), Key = key, RoleClaimId = id++, Type = "Full" });
            }
            Db.MaterialDepartments.AddRange(new MaterialDepartment { Id = Guid.NewGuid(), Material = Material, Department = Actor.Department, UoM = mg },
                new MaterialDepartment { Id = Guid.NewGuid(), Material = Material, Department = Production, UoM = piece });
            Db.Warehouses.Add(new Warehouse { Id = Guid.NewGuid(), Name = "Tablet raw materials", Department = Production, Type = WarehouseType.RawMaterialStorage });
            await Db.SaveChangesAsync();
        }
        public void Dispose() { mapping.Dispose(); harness.Dispose(); }
    }
}
