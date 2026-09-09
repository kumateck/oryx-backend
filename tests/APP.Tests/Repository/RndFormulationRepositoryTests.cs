using APP.Mapper;
using APP.Repository;
using AutoMapper;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.RndFormulations;
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

public class RndFormulationRepositoryTests
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

    private static RndFormulationRepository CreateRepository(ApplicationDbContext context) =>
        new(context, CreateMapper());

    private static async Task<(Guid projectId, Guid materialId)> SeedProjectAndMaterial(ApplicationDbContext context)
    {
        var project = new RndProject
        {
            Id = Guid.NewGuid(),
            Code = "RND-2026-0001",
            Title = "Test project",
            DepartmentId = Guid.NewGuid(),
            RequestedById = Guid.NewGuid(),
            Status = RndProjectStatus.InDevelopment,
            Approved = true,
        };
        var material = new Material { Id = Guid.NewGuid() };
        context.AddRange(project, material);
        await context.SaveChangesAsync();
        return (project.Id, material.Id);
    }

    private static CreateRndFormulationRequest BuildRequest(Guid materialId) =>
        new()
        {
            Items =
            [
                new CreateRndFormulationItemRequest
                {
                    MaterialId = materialId,
                    Order = 1,
                    BaseQuantity = 10,
                    PrescribedQuantity = 10,
                    Percentage = 50,
                },
            ],
        };

    [Fact]
    public async Task CreateFormulation_starts_at_version_1_and_draft()
    {
        await using var context = CreateContext();
        var (projectId, materialId) = await SeedProjectAndMaterial(context);
        var repository = CreateRepository(context);

        var result = await repository.CreateFormulation(projectId, BuildRequest(materialId), Guid.NewGuid());

        Assert.True(result.IsSuccess);
        var formulation = await context.RndFormulations.SingleAsync(f => f.Id == result.Value);
        Assert.Equal(1, formulation.Version);
        Assert.Equal(RndFormulationStatus.Draft, formulation.Status);
    }

    [Fact]
    public async Task CreateFormulation_rejects_pending_project()
    {
        await using var context = CreateContext();
        var (projectId, materialId) = await SeedProjectAndMaterial(context);
        var project = await context.RndProjects.FindAsync(projectId);
        project!.Approved = false;
        await context.SaveChangesAsync();

        var result = await CreateRepository(context)
            .CreateFormulation(projectId, BuildRequest(materialId), Guid.NewGuid());

        Assert.True(result.IsFailure);
        Assert.Equal("Approval.Required", result.Error.Code);
        Assert.Empty(context.RndFormulations);
    }

    [Fact]
    public async Task CreateFormulation_rejects_nonexistent_material()
    {
        await using var context = CreateContext();
        var (projectId, _) = await SeedProjectAndMaterial(context);
        var repository = CreateRepository(context);

        var result = await repository.CreateFormulation(projectId, BuildRequest(Guid.NewGuid()), Guid.NewGuid());

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task CreateNewVersion_increments_version_and_supersedes_predecessor()
    {
        await using var context = CreateContext();
        var (projectId, materialId) = await SeedProjectAndMaterial(context);
        var repository = CreateRepository(context);
        var first = await repository.CreateFormulation(projectId, BuildRequest(materialId), Guid.NewGuid());

        var second = await repository.CreateNewVersion(first.Value, BuildRequest(materialId), Guid.NewGuid());

        Assert.True(second.IsSuccess);
        var predecessor = await context.RndFormulations.SingleAsync(f => f.Id == first.Value);
        var newVersion = await context.RndFormulations.SingleAsync(f => f.Id == second.Value);
        Assert.Equal(RndFormulationStatus.Superseded, predecessor.Status);
        Assert.Equal(2, newVersion.Version);
        Assert.Equal(RndFormulationStatus.Draft, newVersion.Status);
    }

    [Fact]
    public async Task UpdateFormulation_fails_once_out_of_draft()
    {
        await using var context = CreateContext();
        var (projectId, materialId) = await SeedProjectAndMaterial(context);
        var repository = CreateRepository(context);
        var created = await repository.CreateFormulation(projectId, BuildRequest(materialId), Guid.NewGuid());
        await repository.UpdateStatus(created.Value, RndFormulationStatus.InReview, Guid.NewGuid());

        var result = await repository.UpdateFormulation(created.Value, BuildRequest(materialId), Guid.NewGuid());

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task UpdateStatus_rejects_skipping_straight_to_approved()
    {
        await using var context = CreateContext();
        var (projectId, materialId) = await SeedProjectAndMaterial(context);
        var repository = CreateRepository(context);
        var created = await repository.CreateFormulation(projectId, BuildRequest(materialId), Guid.NewGuid());

        var result = await repository.UpdateStatus(created.Value, RndFormulationStatus.Approved, Guid.NewGuid());

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task UpdateStatus_allows_draft_to_in_review_to_approved()
    {
        await using var context = CreateContext();
        var (projectId, materialId) = await SeedProjectAndMaterial(context);
        var repository = CreateRepository(context);
        var created = await repository.CreateFormulation(projectId, BuildRequest(materialId), Guid.NewGuid());

        var toReview = await repository.UpdateStatus(created.Value, RndFormulationStatus.InReview, Guid.NewGuid());
        var toApproved = await repository.UpdateStatus(created.Value, RndFormulationStatus.Approved, Guid.NewGuid());

        Assert.True(toReview.IsSuccess);
        Assert.True(toApproved.IsSuccess);
        var formulation = await context.RndFormulations.SingleAsync(f => f.Id == created.Value);
        Assert.Equal(RndFormulationStatus.Approved, formulation.Status);
    }
}
