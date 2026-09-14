using APP.Repository;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Departments;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Reports;
using DOMAIN.Entities.Users;
using DOMAIN.Entities.Warehouses;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SHARED;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Repository;

file sealed class ChecklistReportCurrentUser(
    Guid departmentId,
    DepartmentType departmentType
) : ICurrentUserService
{
    public Guid? UserId => null;
    public Guid? DepartmentId => departmentId;
    public string DepartmentType => departmentType.ToString();
}

public class ReportRepositoryMaterialsReadyForChecklistTests
{
    [Fact]
    public async Task Non_production_user_gets_consolidated_production_report()
    {
        var fixture = await CreateFixture(DepartmentType.NonProduction);
        await using var context = fixture.Context;
        AddPendingMaterial(context, fixture.ProductionA, WarehouseType.RawMaterialStorage, "RAW-A");
        AddPendingMaterial(context, fixture.ProductionB, WarehouseType.PackagedStorage, "PACK-B");
        AddPendingMaterial(context, fixture.UserDepartment, WarehouseType.RawMaterialStorage, "QC-RAW");
        await context.SaveChangesAsync();

        var result = await fixture.Repository.GetMaterialsReadyForChecklist(
            new MaterialsReadyForChecklistFilter(),
            fixture.UserId
        );

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count);
        Assert.Equal(["PACK-B", "RAW-A"], result.Value.Select(row => row.MaterialCode).Order());
    }

    [Fact]
    public async Task Non_production_user_can_filter_one_production_department()
    {
        var fixture = await CreateFixture(DepartmentType.NonProduction);
        await using var context = fixture.Context;
        AddPendingMaterial(context, fixture.ProductionA, WarehouseType.RawMaterialStorage, "RAW-A");
        AddPendingMaterial(context, fixture.ProductionB, WarehouseType.RawMaterialStorage, "RAW-B");
        await context.SaveChangesAsync();

        var result = await fixture.Repository.GetMaterialsReadyForChecklist(
            new MaterialsReadyForChecklistFilter { DepartmentId = fixture.ProductionB.Id },
            fixture.UserId
        );

        Assert.True(result.IsSuccess);
        Assert.Equal("RAW-B", Assert.Single(result.Value).MaterialCode);
    }

    [Fact]
    public async Task Production_user_is_restricted_to_own_department()
    {
        var fixture = await CreateFixture(DepartmentType.Production);
        await using var context = fixture.Context;
        AddPendingMaterial(context, fixture.UserDepartment, WarehouseType.RawMaterialStorage, "OWN");
        AddPendingMaterial(context, fixture.ProductionB, WarehouseType.RawMaterialStorage, "OTHER");
        await context.SaveChangesAsync();

        var ownResult = await fixture.Repository.GetMaterialsReadyForChecklist(
            new MaterialsReadyForChecklistFilter(),
            fixture.UserId
        );
        var otherResult = await fixture.Repository.GetMaterialsReadyForChecklist(
            new MaterialsReadyForChecklistFilter { DepartmentId = fixture.ProductionB.Id },
            fixture.UserId
        );

        Assert.True(ownResult.IsSuccess);
        Assert.Equal("OWN", Assert.Single(ownResult.Value).MaterialCode);
        Assert.False(otherResult.IsSuccess);
    }

    [Fact]
    public async Task Material_kind_is_optional_and_filters_when_supplied()
    {
        var fixture = await CreateFixture(DepartmentType.NonProduction);
        await using var context = fixture.Context;
        AddPendingMaterial(context, fixture.ProductionA, WarehouseType.RawMaterialStorage, "RAW");
        AddPendingMaterial(context, fixture.ProductionA, WarehouseType.PackagedStorage, "PACK");
        await context.SaveChangesAsync();

        var allResult = await fixture.Repository.GetMaterialsReadyForChecklist(
            new MaterialsReadyForChecklistFilter(),
            fixture.UserId
        );
        var rawResult = await fixture.Repository.GetMaterialsReadyForChecklist(
            new MaterialsReadyForChecklistFilter { MaterialKind = MaterialKind.Raw },
            fixture.UserId
        );

        Assert.True(allResult.IsSuccess);
        Assert.Equal(2, allResult.Value.Count);
        Assert.Equal("RAW", Assert.Single(rawResult.Value).MaterialCode);
    }

    private static async Task<Fixture> CreateFixture(DepartmentType userDepartmentType)
    {
        var userDepartment = Department("User Department", userDepartmentType);
        var productionA = userDepartmentType == DepartmentType.Production
            ? userDepartment
            : Department("Production A", DepartmentType.Production);
        var productionB = Department("Production B", DepartmentType.Production);
        var currentUser = new ChecklistReportCurrentUser(userDepartment.Id, userDepartmentType);
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new ApplicationDbContext(options, currentUser);
        var userId = Guid.NewGuid();

        context.Departments.AddRange(userDepartment, productionA, productionB);
        context.Users.Add(new User
        {
            Id = userId,
            UserName = $"report-{userId}",
            DepartmentId = userDepartment.Id,
            Department = userDepartment
        });
        await context.SaveChangesAsync();

        var repository = new ReportRepository(
            context,
            null!,
            null!,
            NullLogger<ReportRepository>.Instance
        );
        return new Fixture(context, repository, userId, userDepartment, productionA, productionB);
    }

    private static Department Department(string name, DepartmentType type) => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        Code = name.Replace(" ", "-").ToUpperInvariant(),
        Type = type
    };

    private static void AddPendingMaterial(
        ApplicationDbContext context,
        Department department,
        WarehouseType warehouseType,
        string code
    )
    {
        var warehouse = new Warehouse
        {
            Id = Guid.NewGuid(),
            Name = $"{code} warehouse",
            DepartmentId = department.Id,
            Department = department,
            Type = warehouseType
        };
        var arrival = new WarehouseArrivalLocation
        {
            Id = Guid.NewGuid(),
            Name = $"{code} arrival",
            WarehouseId = warehouse.Id,
            Warehouse = warehouse
        };
        var material = new Material
        {
            Id = Guid.NewGuid(),
            Code = code,
            Name = code,
            Kind = warehouseType == WarehouseType.RawMaterialStorage
                ? MaterialKind.Raw
                : MaterialKind.Package
        };
        var uom = new UnitOfMeasure
        {
            Id = Guid.NewGuid(),
            Name = "Kilogram",
            Symbol = "kg"
        };

        context.DistributedRequisitionMaterials.Add(new DistributedRequisitionMaterial
        {
            Id = Guid.NewGuid(),
            WarehouseArrivalLocationId = arrival.Id,
            WarehouseArrivalLocation = arrival,
            MaterialId = material.Id,
            Material = material,
            UoMId = uom.Id,
            UoM = uom,
            Quantity = 10,
            Status = DistributedRequisitionMaterialStatus.Pending,
            CreatedAt = DateTime.UtcNow
        });
    }

    private sealed record Fixture(
        ApplicationDbContext Context,
        ReportRepository Repository,
        Guid UserId,
        Department UserDepartment,
        Department ProductionA,
        Department ProductionB
    );
}
