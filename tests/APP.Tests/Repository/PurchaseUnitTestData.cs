using APP.Mapper;
using APP.Repository;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.Approvals;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Currencies;
using DOMAIN.Entities.Departments;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Permissions;
using DOMAIN.Entities.Procurement.Manufacturers;
using DOMAIN.Entities.Procurement.Suppliers;
using DOMAIN.Entities.Roles;
using DOMAIN.Entities.Users;
using INFRASTRUCTURE.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace APP.Tests.Repository;

internal sealed class PurchaseUnitTestData
{
    public Guid SupplierId { get; } = Guid.NewGuid();
    public Guid MaterialId { get; } = Guid.NewGuid();
    public Guid UnitId { get; } = Guid.NewGuid();
    public Guid ManufacturerId { get; } = Guid.NewGuid();
    public Guid CurrencyId { get; } = Guid.NewGuid();
    public Guid Maker { get; } = Guid.NewGuid();
    public Guid Checker { get; } = Guid.NewGuid();

    public async Task Seed(ApplicationDbContext context, bool workflow = true, bool permission = true)
    {
        context.AddRange(new Supplier { Id = SupplierId, Name = "Supplier" },
            new Material { Id = MaterialId, Name = "Material", Code = "RM-1" },
            new UnitOfMeasure { Id = UnitId, Name = "Milligram", Symbol = "mg" },
            new Manufacturer { Id = ManufacturerId, Name = "Factory" },
            new Currency { Id = CurrencyId, Name = "Cedi", Symbol = "GH₵" },
            new SupplierManufacturer { Id = Guid.NewGuid(), SupplierId = SupplierId,
                MaterialId = MaterialId, UoMId = UnitId, ManufacturerId = ManufacturerId, PurchaseUnit = "kg", Default = true },
            new User { Id = Maker, FirstName = "Maker", LastName = "Test", UserName = "maker" });
        if (workflow) context.Approvals.Add(new Approval { Id = Guid.NewGuid(),
            ItemType = nameof(SupplierPricingAgreement), ApprovalStages = [new ApprovalStage {
                Id = Guid.NewGuid(), Order = 1, Required = true, UserId = Checker }] });
        if (permission)
        {
            var role = new Role { Id = Guid.NewGuid(), Name = "Contract managers", DisplayName = "Contract managers" };
            context.Roles.Add(role);
            context.UserRoles.Add(new IdentityUserRole<Guid> { UserId = Maker, RoleId = role.Id });
            var claim = new IdentityRoleClaim<Guid> { RoleId = role.Id, ClaimType = AppConstants.Permission,
                ClaimValue = PermissionKeys.CanManageSupplierContracts };
            context.RoleClaims.Add(claim);
            await context.SaveChangesAsync();
            context.PermissionTypes.Add(new PermissionType { Id = Guid.NewGuid(), RoleClaimId = claim.Id,
                Key = PermissionKeys.CanManageSupplierContracts, Type = "Access" });
        }
        await context.SaveChangesAsync();
    }

    public SupplierPricingAgreement Agreement(string symbol = "kg", decimal price = 20m) => new() {
        Id = Guid.NewGuid(), SupplierId = SupplierId, MaterialId = MaterialId, UoMId = UnitId,
        CurrencyId = CurrencyId, PriceUoM = symbol, AgreedPrice = price,
        EffectiveFrom = DateTime.UtcNow.Date.AddDays(-5), Status = SupplierPricingAgreementStatus.Approved,
    };

    public MaterialDepartment Link(Guid departmentId, Guid unitId) => new() {
        Id = Guid.NewGuid(), MaterialId = MaterialId, DepartmentId = departmentId, UoMId = unitId,
    };

    public static IMapper Mapper(ApplicationDbContext context)
    {
        var services = new ServiceCollection(); services.AddLogging(); services.AddSingleton(context);
        services.AddHttpContextAccessor();
        services.AddAutoMapper(cfg => { }, typeof(OryxMapper));
        return services.BuildServiceProvider().GetRequiredService<IMapper>();
    }

    public static ApprovalRepository Approvals(ApplicationDbContext context) => new(context, null, null, null,
        NullLogger<ApprovalRepository>.Instance, null, null);

    public static ProcurementRepository Procurement(ApplicationDbContext context) =>
        new(context, Mapper(context), null, null, Approvals(context), null, null);
}
