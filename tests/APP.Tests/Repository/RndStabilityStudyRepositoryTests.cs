using APP.Mapper;
using APP.Repository;
using AutoMapper;
using DOMAIN.Entities.RndFormulations;
using DOMAIN.Entities.RndProjects;
using DOMAIN.Entities.RndStabilityStudies;
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

public class RndStabilityStudyRepositoryTests
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

    private static RndStabilityStudyRepository CreateRepository(ApplicationDbContext context) =>
        new(context, CreateMapper());

    private static async Task<(
        RndProject projectA,
        RndTrialBatch trialBatchA,
        RndProject projectB,
        RndTrialBatch trialBatchB,
        Guid chamberId
    )> SeedFixture(ApplicationDbContext context, bool projectAApproved = true)
    {
        var projectA = new RndProject
        {
            Id = Guid.NewGuid(),
            Code = "RND-2026-0001",
            Title = "Project A",
            DepartmentId = Guid.NewGuid(),
            RequestedById = Guid.NewGuid(),
            Status = RndProjectStatus.InDevelopment,
            Approved = projectAApproved,
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
        var formulationB = new RndFormulation
        {
            Id = Guid.NewGuid(),
            RndProjectId = projectB.Id,
            Version = 1,
            Status = RndFormulationStatus.Draft,
        };
        var trialBatchA = new RndTrialBatch
        {
            Id = Guid.NewGuid(),
            RndProjectId = projectA.Id,
            RndFormulationId = formulationA.Id,
            BatchCode = "TRB-2026-0001",
            ScaleType = RndTrialBatchScaleType.LabScale,
            BatchSize = 1,
            Status = RndTrialBatchStatus.Completed,
        };
        var trialBatchB = new RndTrialBatch
        {
            Id = Guid.NewGuid(),
            RndProjectId = projectB.Id,
            RndFormulationId = formulationB.Id,
            BatchCode = "TRB-2026-0002",
            ScaleType = RndTrialBatchScaleType.LabScale,
            BatchSize = 1,
            Status = RndTrialBatchStatus.Completed,
        };
        var chamber = new RndStabilityChamber
        {
            Id = Guid.NewGuid(),
            Code = "CH-01",
            Name = "Long-term chamber 1",
            ConditionType = "Long-term 25C/60%RH",
            TargetTemperature = 25,
            TargetHumidity = 60,
        };
        context.AddRange(projectA, projectB, formulationA, formulationB, trialBatchA, trialBatchB, chamber);
        await context.SaveChangesAsync();
        return (projectA, trialBatchA, projectB, trialBatchB, chamber.Id);
    }

    private static CreateRndStabilityStudyRequest BuildRequest(Guid trialBatchId, Guid chamberId) =>
        new()
        {
            RndTrialBatchId = trialBatchId,
            RndStabilityChamberId = chamberId,
            StartDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            PullPointTimePointsMonths = [0, 3, 6, 12],
        };

    [Fact]
    public async Task CreateStudy_rejects_pending_project()
    {
        await using var context = CreateContext();
        var (projectA, trialBatchA, _, _, chamberId) = await SeedFixture(context, projectAApproved: false);
        var repository = CreateRepository(context);

        var result = await repository.CreateStudy(
            projectA.Id,
            BuildRequest(trialBatchA.Id, chamberId),
            Guid.NewGuid()
        );

        Assert.True(result.IsFailure);
        Assert.Equal("Approval.Required", result.Error.Code);
    }

    [Fact]
    public async Task CreateStudy_rejects_trial_batch_from_a_different_project()
    {
        await using var context = CreateContext();
        var (projectA, _, _, trialBatchB, chamberId) = await SeedFixture(context);
        var repository = CreateRepository(context);

        var result = await repository.CreateStudy(
            projectA.Id,
            BuildRequest(trialBatchB.Id, chamberId),
            Guid.NewGuid()
        );

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task CreateStudy_rejects_nonexistent_chamber()
    {
        await using var context = CreateContext();
        var (projectA, trialBatchA, _, _, _) = await SeedFixture(context);
        var repository = CreateRepository(context);

        var result = await repository.CreateStudy(
            projectA.Id,
            BuildRequest(trialBatchA.Id, Guid.NewGuid()),
            Guid.NewGuid()
        );

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task CreateStudy_rejects_empty_pull_point_list()
    {
        await using var context = CreateContext();
        var (projectA, trialBatchA, _, _, chamberId) = await SeedFixture(context);
        var repository = CreateRepository(context);
        var request = BuildRequest(trialBatchA.Id, chamberId);
        request.PullPointTimePointsMonths = [];

        var result = await repository.CreateStudy(projectA.Id, request, Guid.NewGuid());

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task CreateStudy_generates_pull_points_with_correct_due_dates()
    {
        await using var context = CreateContext();
        var (projectA, trialBatchA, _, _, chamberId) = await SeedFixture(context);
        var repository = CreateRepository(context);

        var result = await repository.CreateStudy(
            projectA.Id,
            BuildRequest(trialBatchA.Id, chamberId),
            Guid.NewGuid()
        );

        Assert.True(result.IsSuccess);
        var study = await context
            .RndStabilityStudies.Include(s => s.PullPoints)
            .SingleAsync(s => s.Id == result.Value);
        Assert.Equal(RndStabilityStudyStatus.Active, study.Status);
        Assert.Equal(4, study.PullPoints.Count);
        var sixMonthPoint = study.PullPoints.Single(p => p.TimePointMonths == 6);
        Assert.Equal(new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc), sixMonthPoint.DueDate);
        Assert.All(study.PullPoints, p => Assert.Equal(RndStabilityPullPointStatus.Scheduled, p.Status));
    }

    [Fact]
    public async Task RecordPullPointResult_marks_reported_and_stamps_puller()
    {
        await using var context = CreateContext();
        var (projectA, trialBatchA, _, _, chamberId) = await SeedFixture(context);
        var repository = CreateRepository(context);
        var created = await repository.CreateStudy(
            projectA.Id,
            BuildRequest(trialBatchA.Id, chamberId),
            Guid.NewGuid()
        );
        var study = await context
            .RndStabilityStudies.Include(s => s.PullPoints)
            .SingleAsync(s => s.Id == created.Value);
        var pullPoint = study.PullPoints.First(p => p.TimePointMonths == 0);
        var pullerId = Guid.NewGuid();

        var result = await repository.RecordPullPointResult(
            pullPoint.Id,
            new RecordPullPointResultRequest { ResultsSummary = "Within specification." },
            pullerId
        );

        Assert.True(result.IsSuccess);
        var updated = await context.RndStabilityPullPoints.SingleAsync(p => p.Id == pullPoint.Id);
        Assert.Equal(RndStabilityPullPointStatus.Reported, updated.Status);
        Assert.Equal(pullerId, updated.PulledById);
        Assert.NotNull(updated.PulledAt);
        Assert.Equal("Within specification.", updated.ResultsSummary);
    }

    [Fact]
    public async Task RecordPullPointResult_rejects_already_reported()
    {
        await using var context = CreateContext();
        var (projectA, trialBatchA, _, _, chamberId) = await SeedFixture(context);
        var repository = CreateRepository(context);
        var created = await repository.CreateStudy(
            projectA.Id,
            BuildRequest(trialBatchA.Id, chamberId),
            Guid.NewGuid()
        );
        var study = await context
            .RndStabilityStudies.Include(s => s.PullPoints)
            .SingleAsync(s => s.Id == created.Value);
        var pullPoint = study.PullPoints.First(p => p.TimePointMonths == 0);
        await repository.RecordPullPointResult(
            pullPoint.Id,
            new RecordPullPointResultRequest { ResultsSummary = "First result." },
            Guid.NewGuid()
        );

        var result = await repository.RecordPullPointResult(
            pullPoint.Id,
            new RecordPullPointResultRequest { ResultsSummary = "Second attempt." },
            Guid.NewGuid()
        );

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task UpdateStudyStatus_rejects_transition_once_terminated()
    {
        await using var context = CreateContext();
        var (projectA, trialBatchA, _, _, chamberId) = await SeedFixture(context);
        var repository = CreateRepository(context);
        var created = await repository.CreateStudy(
            projectA.Id,
            BuildRequest(trialBatchA.Id, chamberId),
            Guid.NewGuid()
        );
        await repository.UpdateStudyStatus(
            created.Value,
            new UpdateRndStabilityStudyStatusRequest { Status = RndStabilityStudyStatus.Terminated },
            Guid.NewGuid()
        );

        var result = await repository.UpdateStudyStatus(
            created.Value,
            new UpdateRndStabilityStudyStatusRequest { Status = RndStabilityStudyStatus.Completed },
            Guid.NewGuid()
        );

        Assert.True(result.IsFailure);
    }
}
