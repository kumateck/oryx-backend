using APP.Mapper;
using APP.Repository;
using AutoMapper;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Products;
using DOMAIN.Entities.RndAnalyticalMethods;
using DOMAIN.Entities.RndProjects;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Repository;

file class NoCurrentUserService : ICurrentUserService
{
    public Guid? UserId => null;
    public Guid? DepartmentId => null;
    public string DepartmentType => string.Empty;
}

public class RndAnalyticalMethodRepositoryTests
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
        services.AddHttpContextAccessor();
        services.AddAutoMapper(cfg => { }, typeof(OryxMapper));
        return services.BuildServiceProvider().GetRequiredService<IMapper>();
    }

    private static RndAnalyticalMethodRepository CreateRepository(ApplicationDbContext context) =>
        new(context, CreateMapper());

    private static async Task<(Guid projectId, Guid materialId)> SeedProjectAndMaterial(
        ApplicationDbContext context,
        bool approved = true
    )
    {
        var project = new RndProject
        {
            Id = Guid.NewGuid(),
            Code = "RND-2026-0001",
            Title = "Test project",
            DepartmentId = Guid.NewGuid(),
            RequestedById = Guid.NewGuid(),
            Status = RndProjectStatus.InDevelopment,
            Approved = approved,
        };
        var material = new Material { Id = Guid.NewGuid() };
        context.AddRange(project, material);
        await context.SaveChangesAsync();
        return (project.Id, material.Id);
    }

    private static CreateRndAnalyticalMethodRequest BuildRequest(Guid? materialId, Guid? productId) =>
        new()
        {
            MaterialId = materialId,
            ProductId = productId,
            MethodName = "Assay by HPLC",
            Description = "Reverse-phase HPLC assay method.",
        };

    [Fact]
    public async Task CreateMethod_rejects_pending_project()
    {
        await using var context = CreateContext();
        var (projectId, materialId) = await SeedProjectAndMaterial(context, approved: false);
        var repository = CreateRepository(context);

        var result = await repository.CreateMethod(projectId, BuildRequest(materialId, null), Guid.NewGuid());

        Assert.True(result.IsFailure);
        Assert.Equal("Approval.Required", result.Error.Code);
        Assert.Empty(context.RndAnalyticalMethods);
    }

    [Fact]
    public async Task CreateMethod_rejects_neither_material_nor_product_set()
    {
        await using var context = CreateContext();
        var (projectId, _) = await SeedProjectAndMaterial(context);
        var repository = CreateRepository(context);

        var result = await repository.CreateMethod(projectId, BuildRequest(null, null), Guid.NewGuid());

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task CreateMethod_rejects_both_material_and_product_set()
    {
        await using var context = CreateContext();
        var (projectId, materialId) = await SeedProjectAndMaterial(context);
        var product = new Product { Id = Guid.NewGuid() };
        context.Products.Add(product);
        await context.SaveChangesAsync();
        var repository = CreateRepository(context);

        var result = await repository.CreateMethod(
            projectId,
            BuildRequest(materialId, product.Id),
            Guid.NewGuid()
        );

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task CreateMethod_rejects_nonexistent_material()
    {
        await using var context = CreateContext();
        var (projectId, _) = await SeedProjectAndMaterial(context);
        var repository = CreateRepository(context);

        var result = await repository.CreateMethod(
            projectId,
            BuildRequest(Guid.NewGuid(), null),
            Guid.NewGuid()
        );

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task CreateMethod_defaults_to_draft()
    {
        await using var context = CreateContext();
        var (projectId, materialId) = await SeedProjectAndMaterial(context);
        var repository = CreateRepository(context);

        var result = await repository.CreateMethod(projectId, BuildRequest(materialId, null), Guid.NewGuid());

        Assert.True(result.IsSuccess);
        var method = await context.RndAnalyticalMethods.SingleAsync(m => m.Id == result.Value);
        Assert.Equal(RndAnalyticalMethodStatus.Draft, method.Status);
        Assert.Equal(materialId, method.MaterialId);
        Assert.Null(method.ProductId);
    }

    [Fact]
    public async Task UpdateMethod_fails_once_out_of_draft()
    {
        await using var context = CreateContext();
        var (projectId, materialId) = await SeedProjectAndMaterial(context);
        var repository = CreateRepository(context);
        var created = await repository.CreateMethod(projectId, BuildRequest(materialId, null), Guid.NewGuid());
        await repository.UpdateStatus(
            created.Value,
            new UpdateRndAnalyticalMethodStatusRequest { Status = RndAnalyticalMethodStatus.UnderValidation },
            Guid.NewGuid()
        );

        var result = await repository.UpdateMethod(
            created.Value,
            BuildRequest(materialId, null),
            Guid.NewGuid()
        );

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task UpdateStatus_rejects_skipping_straight_to_validated()
    {
        await using var context = CreateContext();
        var (projectId, materialId) = await SeedProjectAndMaterial(context);
        var repository = CreateRepository(context);
        var created = await repository.CreateMethod(projectId, BuildRequest(materialId, null), Guid.NewGuid());

        var result = await repository.UpdateStatus(
            created.Value,
            new UpdateRndAnalyticalMethodStatusRequest { Status = RndAnalyticalMethodStatus.Validated },
            Guid.NewGuid()
        );

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task UpdateStatus_allows_draft_to_under_validation_to_validated_and_stamps_validator()
    {
        await using var context = CreateContext();
        var (projectId, materialId) = await SeedProjectAndMaterial(context);
        var repository = CreateRepository(context);
        var created = await repository.CreateMethod(projectId, BuildRequest(materialId, null), Guid.NewGuid());
        var validatorId = Guid.NewGuid();

        var toUnderValidation = await repository.UpdateStatus(
            created.Value,
            new UpdateRndAnalyticalMethodStatusRequest { Status = RndAnalyticalMethodStatus.UnderValidation },
            Guid.NewGuid()
        );
        var toValidated = await repository.UpdateStatus(
            created.Value,
            new UpdateRndAnalyticalMethodStatusRequest { Status = RndAnalyticalMethodStatus.Validated },
            validatorId
        );

        Assert.True(toUnderValidation.IsSuccess);
        Assert.True(toValidated.IsSuccess);
        var method = await context.RndAnalyticalMethods.SingleAsync(m => m.Id == created.Value);
        Assert.Equal(RndAnalyticalMethodStatus.Validated, method.Status);
        Assert.Equal(validatorId, method.ValidatedById);
        Assert.NotNull(method.ValidatedAt);
    }
}
