using APP.Mapper;
using APP.Repository;
using AutoMapper;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.MaterialStandardTestProcedures;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Repository;

file class FakeCurrentUserService : ICurrentUserService
{
    public Guid? UserId => null;
    public Guid? DepartmentId => null;
    public string DepartmentType => string.Empty;
}

public class MaterialStandardTestProcedureSearchTests
{
    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options, new FakeCurrentUserService());
    }

    private static IMapper CreateMapper(ApplicationDbContext context)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHttpContextAccessor();
        services.AddSingleton(context);
        services.AddAutoMapper(cfg => { }, typeof(OryxMapper));
        return services.BuildServiceProvider().GetRequiredService<IMapper>();
    }

    [Fact]
    public async Task SearchByMaterialCode_FindsStandardTestProcedure()
    {
        await using var context = CreateContext();
        var mapper = CreateMapper(context);

        var material = new Material
        {
            Id = Guid.NewGuid(),
            Code = "RMA003",
            Name = "Alginic Acid",
            Kind = MaterialKind.Raw,
        };
        context.Materials.Add(material);

        var stp = new MaterialStandardTestProcedure
        {
            Id = Guid.NewGuid(),
            StpNumber = "QCD/STP/RM/003",
            MaterialId = material.Id,
        };
        context.MaterialStandardTestProcedures.Add(stp);
        await context.SaveChangesAsync();

        var repository = new MaterialStandardTestProcedureRepository(context, mapper);
        var result = await repository.GetMaterialStandardTestProcedures(
            page: 1,
            pageSize: 50,
            searchQuery: "RMA003",
            materialKind: MaterialKind.Raw,
            unused: false
        );

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.TotalRecordCount);
        Assert.Equal(stp.Id, Assert.Single(result.Value.Data).Id);
    }

    [Fact]
    public async Task SearchByUnrelatedTerm_FindsNothing()
    {
        await using var context = CreateContext();
        var mapper = CreateMapper(context);

        var material = new Material
        {
            Id = Guid.NewGuid(),
            Code = "RMA003",
            Name = "Alginic Acid",
            Kind = MaterialKind.Raw,
        };
        context.Materials.Add(material);

        var stp = new MaterialStandardTestProcedure
        {
            Id = Guid.NewGuid(),
            StpNumber = "QCD/STP/RM/003",
            MaterialId = material.Id,
        };
        context.MaterialStandardTestProcedures.Add(stp);
        await context.SaveChangesAsync();

        var repository = new MaterialStandardTestProcedureRepository(context, mapper);
        var result = await repository.GetMaterialStandardTestProcedures(
            page: 1,
            pageSize: 50,
            searchQuery: "NoSuchCodeOrName",
            materialKind: MaterialKind.Raw,
            unused: false
        );

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value.TotalRecordCount);
    }
}
