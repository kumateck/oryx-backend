using APP.Mapper;
using APP.Repository;
using AutoMapper;
using DOMAIN.Entities.Forms;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.MaterialSpecifications;
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

public class MaterialSpecificationSearchTests
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
        services.AddSingleton(context);
        services.AddAutoMapper(cfg => { }, typeof(OryxMapper));
        return services.BuildServiceProvider().GetRequiredService<IMapper>();
    }

    private static MaterialSpecification SeedMaterialWithSpecification(
        ApplicationDbContext context,
        string materialCode
    )
    {
        var material = new Material
        {
            Id = Guid.NewGuid(),
            Code = materialCode,
            Name = "Some Raw Material Totally Unrelated To The Search Term",
            Kind = MaterialKind.Raw,
        };
        context.Materials.Add(material);

        var form = new Form { Id = Guid.NewGuid(), Name = "Test Form" };
        context.Forms.Add(form);

        var spec = new MaterialSpecification
        {
            Id = Guid.NewGuid(),
            SpecificationNumber = "QCD/SPC/RM/999",
            Description = "Unrelated description",
            MaterialId = material.Id,
            FormId = form.Id,
            EffectiveDate = DateTime.UtcNow,
            ReviewDate = DateTime.UtcNow,
            DueDate = DateTime.UtcNow,
        };
        context.MaterialSpecifications.Add(spec);

        return spec;
    }

    [Fact]
    public async Task SearchByMaterialCode_FindsSpecification()
    {
        await using var context = CreateContext();
        var mapper = CreateMapper(context);
        var spec = SeedMaterialWithSpecification(context, "RMA003");
        await context.SaveChangesAsync();

        var repository = new MaterialSpecificationRepository(context, mapper);
        var result = await repository.GetMaterialSpecifications(
            page: 1,
            pageSize: 50,
            searchQuery: "RMA003",
            materialKind: MaterialKind.Raw
        );

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.TotalRecordCount);
        Assert.Equal(spec.Id, Assert.Single(result.Value.Data).Id);
    }

    [Fact]
    public async Task SearchByUnrelatedTerm_FindsNothing()
    {
        await using var context = CreateContext();
        var mapper = CreateMapper(context);
        SeedMaterialWithSpecification(context, "RMA003");
        await context.SaveChangesAsync();

        var repository = new MaterialSpecificationRepository(context, mapper);
        var result = await repository.GetMaterialSpecifications(
            page: 1,
            pageSize: 50,
            searchQuery: "NoSuchCodeOrName",
            materialKind: MaterialKind.Raw
        );

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value.TotalRecordCount);
    }
}
