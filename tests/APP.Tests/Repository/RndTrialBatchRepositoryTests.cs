using APP.Mapper;
using APP.Repository;
using AutoMapper;
using DOMAIN.Entities.RndFormulations;
using DOMAIN.Entities.RndProjects;
using DOMAIN.Entities.QualityRoutines;
using DOMAIN.Entities.RndTrialBatches;
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

public class RndTrialBatchRepositoryTests
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

    private static RndTrialBatchRepository CreateRepository(ApplicationDbContext context) =>
        new(context, CreateMapper());

    private static async Task<(RndProject projectA, RndFormulation formulationA, RndProject projectB)> SeedTwoProjects(
        ApplicationDbContext context
    )
    {
        var projectA = new RndProject
        {
            Id = Guid.NewGuid(),
            Code = "RND-2026-0001",
            Title = "Project A",
            DepartmentId = Guid.NewGuid(),
            RequestedById = Guid.NewGuid(),
            Status = RndProjectStatus.InDevelopment,
            Approved = true,
        };
        var projectB = new RndProject
        {
            Id = Guid.NewGuid(),
            Code = "RND-2026-0002",
            Title = "Project B",
            DepartmentId = Guid.NewGuid(),
            RequestedById = Guid.NewGuid(),
            Status = RndProjectStatus.InDevelopment,
            Approved = true,
        };
        var formulationA = new RndFormulation
        {
            Id = Guid.NewGuid(),
            RndProjectId = projectA.Id,
            Version = 1,
            Status = RndFormulationStatus.Draft,
        };
        context.AddRange(projectA, projectB, formulationA);
        await context.SaveChangesAsync();
        return (projectA, formulationA, projectB);
    }

    [Fact]
    public async Task CreateTrialBatch_rejects_formulation_from_a_different_project()
    {
        await using var context = CreateContext();
        var (_, formulationA, projectB) = await SeedTwoProjects(context);
        var repository = CreateRepository(context);

        var result = await repository.CreateTrialBatch(
            projectB.Id,
            new CreateRndTrialBatchRequest { RndFormulationId = formulationA.Id, BatchSize = 1 },
            Guid.NewGuid()
        );

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task CreateTrialBatch_rejects_pending_project()
    {
        await using var context = CreateContext();
        var (project, formulation, _) = await SeedTwoProjects(context);
        project.Approved = false;
        await context.SaveChangesAsync();

        var result = await CreateRepository(context).CreateTrialBatch(
            project.Id,
            new CreateRndTrialBatchRequest { RndFormulationId = formulation.Id, BatchSize = 1 },
            Guid.NewGuid()
        );

        Assert.True(result.IsFailure);
        Assert.Equal("Approval.Required", result.Error.Code);
        Assert.Empty(context.RndTrialBatches);
    }

    [Fact]
    public async Task CreateTrialBatch_generates_sequential_code()
    {
        await using var context = CreateContext();
        var (projectA, formulationA, _) = await SeedTwoProjects(context);
        var repository = CreateRepository(context);

        var first = await repository.CreateTrialBatch(
            projectA.Id,
            new CreateRndTrialBatchRequest { RndFormulationId = formulationA.Id, BatchSize = 1 },
            Guid.NewGuid()
        );
        var second = await repository.CreateTrialBatch(
            projectA.Id,
            new CreateRndTrialBatchRequest { RndFormulationId = formulationA.Id, BatchSize = 1 },
            Guid.NewGuid()
        );

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        var firstBatch = await context.RndTrialBatches.SingleAsync(b => b.Id == first.Value);
        var secondBatch = await context.RndTrialBatches.SingleAsync(b => b.Id == second.Value);
        var year = DateTime.UtcNow.Year;
        Assert.Equal($"TRB-{year}-0001", firstBatch.BatchCode);
        Assert.Equal($"TRB-{year}-0002", secondBatch.BatchCode);
        Assert.Equal(RndTrialBatchStatus.Planned, firstBatch.Status);
    }

    [Fact]
    public async Task Complete_waits_for_linked_routine_quality_work()
    {
        await using var context = CreateContext();
        var (project, formulation, _) = await SeedTwoProjects(context);
        var repository = CreateRepository(context);
        var created = await repository.CreateTrialBatch(project.Id,
            new CreateRndTrialBatchRequest
            {
                RndFormulationId = formulation.Id, BatchSize = 1
            }, Guid.NewGuid());
        var routine = new RoutineExecution
        {
            Id = Guid.NewGuid(), RoutineCode = "RUT/RND/TEST",
            Type = RoutineType.Water, Origin = RoutineOrigin.Emergency,
            RoutineDate = DateTime.UtcNow, RndTrialBatchId = created.Value,
            Status = RoutineStatus.InProgress
        };
        context.RoutineExecutions.Add(routine);
        await context.SaveChangesAsync();

        var pending = await repository.UpdateStatus(created.Value,
            new UpdateRndTrialBatchStatusRequest
            {
                Status = RndTrialBatchStatus.Completed
            }, Guid.NewGuid());
        Assert.True(pending.IsFailure);
        Assert.Equal("RndTrialBatch.RoutineQualityPending", pending.Error.Code);

        routine.Status = RoutineStatus.Approved;
        await context.SaveChangesAsync();
        var completed = await repository.UpdateStatus(created.Value,
            new UpdateRndTrialBatchStatusRequest
            {
                Status = RndTrialBatchStatus.Completed
            }, Guid.NewGuid());
        Assert.True(completed.IsSuccess);
    }

    [Fact]
    public async Task UpdateStatus_rejects_transition_once_completed()
    {
        await using var context = CreateContext();
        var (projectA, formulationA, _) = await SeedTwoProjects(context);
        var repository = CreateRepository(context);
        var created = await repository.CreateTrialBatch(
            projectA.Id,
            new CreateRndTrialBatchRequest { RndFormulationId = formulationA.Id, BatchSize = 1 },
            Guid.NewGuid()
        );
        await repository.UpdateStatus(
            created.Value,
            new UpdateRndTrialBatchStatusRequest { Status = RndTrialBatchStatus.Completed },
            Guid.NewGuid()
        );

        var result = await repository.UpdateStatus(
            created.Value,
            new UpdateRndTrialBatchStatusRequest { Status = RndTrialBatchStatus.InProgress },
            Guid.NewGuid()
        );

        Assert.True(result.IsFailure);
    }
}
