using APP.Mapper;
using APP.Repository;
using AutoMapper;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.MaterialStandardTestProcedures;
using DOMAIN.Entities.Reports;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Repository;

file class NoCurrentUserService : ICurrentUserService
{
    public Guid? UserId => null;
    public Guid? DepartmentId => null;
    public string DepartmentType => string.Empty;
}

/// <summary>
/// The QA/QC dashboard "Material Type" filter sent a MaterialKind query param that the
/// repository accepted but never applied to any query - selecting "Raw Material" or
/// "Packaging Material" in the dashboard UI had no effect on the returned counts.
/// </summary>
public class ReportRepositoryDashboardFilterTests
{
    private static ApplicationDbContext CreateContext() =>
        new(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options,
            new NoCurrentUserService()
        );

    private static IMapper CreateMapper()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAutoMapper(cfg => { }, typeof(OryxMapper));
        return services.BuildServiceProvider().GetRequiredService<IMapper>();
    }

    private static ReportRepository CreateRepository(ApplicationDbContext context) =>
        new(context, CreateMapper(), null!, NullLogger<ReportRepository>.Instance);

    [Fact]
    public async Task GetQaDashboardReport_filters_materials_by_kind()
    {
        await using var context = CreateContext();
        context.Materials.AddRange(
            new Material { Id = Guid.NewGuid(), Name = "Raw One", Kind = MaterialKind.Raw },
            new Material { Id = Guid.NewGuid(), Name = "Raw Two", Kind = MaterialKind.Raw },
            new Material { Id = Guid.NewGuid(), Name = "Pack One", Kind = MaterialKind.Package }
        );
        await context.SaveChangesAsync();

        var repository = CreateRepository(context);

        var unfiltered = await repository.GetQaDashboardReport(new ReportFilter(), null);
        Assert.True(unfiltered.IsSuccess);
        Assert.Equal(2, unfiltered.Value.NumberOfRawMaterials);
        Assert.Equal(1, unfiltered.Value.NumberOfPackingMaterials);

        var rawOnly = await repository.GetQaDashboardReport(
            new ReportFilter { MaterialKind = MaterialKind.Raw },
            null
        );
        Assert.True(rawOnly.IsSuccess);
        Assert.Equal(2, rawOnly.Value.NumberOfRawMaterials);
        Assert.Equal(0, rawOnly.Value.NumberOfPackingMaterials);
    }

    [Fact]
    public async Task GetQcDashboardReport_filters_material_stp_by_kind()
    {
        await using var context = CreateContext();
        var rawMaterialId = Guid.NewGuid();
        var packMaterialId = Guid.NewGuid();
        context.Materials.AddRange(
            new Material { Id = rawMaterialId, Name = "Raw", Kind = MaterialKind.Raw },
            new Material { Id = packMaterialId, Name = "Pack", Kind = MaterialKind.Package }
        );
        context.MaterialStandardTestProcedures.AddRange(
            new MaterialStandardTestProcedure { Id = Guid.NewGuid(), MaterialId = rawMaterialId, StpNumber = "STP-1" },
            new MaterialStandardTestProcedure { Id = Guid.NewGuid(), MaterialId = packMaterialId, StpNumber = "STP-2" }
        );
        await context.SaveChangesAsync();

        var repository = CreateRepository(context);

        var unfiltered = await repository.GetQcDashboardReport(new ReportFilter(), null, null);
        Assert.True(unfiltered.IsSuccess);
        Assert.Equal(1, unfiltered.Value.NumberOfStpRawMaterials);
        Assert.Equal(1, unfiltered.Value.NumberOfStpPackingMaterials);

        var packagingOnly = await repository.GetQcDashboardReport(
            new ReportFilter { MaterialKind = MaterialKind.Package },
            null,
            null
        );
        Assert.True(packagingOnly.IsSuccess);
        Assert.Equal(0, packagingOnly.Value.NumberOfStpRawMaterials);
        Assert.Equal(1, packagingOnly.Value.NumberOfStpPackingMaterials);
    }
}
