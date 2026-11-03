using APP.Mapper;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using APP.Repository;
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
using SHARED;
using Xunit;

namespace APP.Tests.Repository;

public class QaProductMaterialTests
{
    [Fact]
    public async Task Product_context_uses_product_linkage_and_preserves_legacy_user_linkage()
    {
        using var f = new Fixture();
        await f.Seed();
        var own = await f.Repo.GetUnitOfMeasureForMaterialDepartment(f.Material.Id, f.Actor.Id);
        Assert.Equal("mg", own.Value.Symbol);
        var selected = await f.Repo.GetUnitOfMeasureForMaterialDepartment(f.Material.Id, f.Actor.Id, f.Product.Id);
        Assert.True(selected.IsSuccess, selected.Error?.Code + ": " + selected.Error?.Description);
        Assert.Equal("piece", selected.Value.Symbol);
        var rows = await f.Repo.GetMaterialDepartments(1, 20, null, MaterialKind.Raw, null, "", null, f.Actor.Id, f.Product.Id);
        Assert.True(rows.IsSuccess, rows.Error?.Description);
        Assert.Equal("piece", Assert.Single(rows.Value.Data).UoM.Symbol);
        Assert.Equal(f.Actor.DepartmentId, (await f.Db.Users.FindAsync(f.Actor.Id)).DepartmentId);
    }

    [Theory]
    [InlineData(PermissionKeys.CanViewPlannedProducts)]
    [InlineData(PermissionKeys.CanEditProductionPlan)]
    public async Task Product_context_refuses_missing_or_revoked_live_grants(string revoked)
    {
        using var f = new Fixture(); await f.Seed();
        f.Db.PermissionTypes.RemoveRange(f.Db.PermissionTypes.Where(p => p.Key == revoked));
        await f.Db.SaveChangesAsync();
        var unit = await f.Repo.GetUnitOfMeasureForMaterialDepartment(f.Material.Id, f.Actor.Id, f.Product.Id);
        Assert.Equal(ErrorType.Forbidden, unit.Error.Type);
        var rows = await f.Repo.GetMaterialDepartments(1, 20, null, null, null, "", null, f.Actor.Id, f.Product.Id);
        Assert.Equal(ErrorType.Forbidden, rows.Error.Type);
    }

    [Fact]
    public async Task Archived_product_and_nonproduction_department_cannot_supply_a_product_linkage()
    {
        using var f = new Fixture(); await f.Seed();
        f.Product.DeletedAt = DateTime.UtcNow; await f.Db.SaveChangesAsync();
        Assert.Equal(ErrorType.NotFound, (await f.Repo.GetUnitOfMeasureForMaterialDepartment(f.Material.Id, f.Actor.Id, f.Product.Id)).Error.Type);
        f.Product.DeletedAt = null; f.Production.Type = DepartmentType.NonProduction; await f.Db.SaveChangesAsync();
        Assert.Equal(ErrorType.NotFound, (await f.Repo.GetUnitOfMeasureForMaterialDepartment(f.Material.Id, f.Actor.Id, f.Product.Id)).Error.Type);
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
        internal async Task Seed()
        {
            var mg = new UnitOfMeasure { Id = Guid.NewGuid(), Name = "Milligram", Symbol = "mg" };
            var piece = new UnitOfMeasure { Id = Guid.NewGuid(), Name = "Piece", Symbol = "piece" };
            Product = new() { Id = Guid.NewGuid(), Name = "Zinvite", Department = Production, BaseUoM = mg };
            var role = new Role { Id = Guid.NewGuid(), Name = "QA products fixture" };
            Db.Users.Add(Actor); Db.Products.Add(Product); Db.Roles.Add(role);
            Db.UserRoles.Add(new IdentityUserRole<Guid> { UserId = Actor.Id, RoleId = role.Id });
            var id = 991;
            foreach (var key in new[] { PermissionKeys.CanViewPlannedProducts, PermissionKeys.CanEditProductionPlan })
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
