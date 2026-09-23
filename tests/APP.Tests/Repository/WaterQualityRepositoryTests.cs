using APP.Repository;
using DOMAIN.Entities.QualityRoutines;
using DOMAIN.Entities.RndTrialBatches;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Repository;

file class WaterCurrentUser : ICurrentUserService
{
    public Guid? UserId => null;
    public Guid? DepartmentId => null;
    public string DepartmentType => string.Empty;
}

public class WaterQualityRepositoryTests
{
    private static ApplicationDbContext Context() => new(
        new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options,
        new WaterCurrentUser());

    [Fact]
    public async Task Approved_water_period_covers_RnD_use_and_hold_marks_it_affected()
    {
        await using var context = Context();
        var now = DateTime.UtcNow;
        var run = new RoutineExecution
        {
            Id = Guid.NewGuid(), RoutineCode = "RUT/WATER/COVERAGE",
            Type = RoutineType.Water, Origin = RoutineOrigin.Scheduled,
            RoutineDate = now, Status = RoutineStatus.Approved
        };
        var sample = new RoutineSample
        {
            Id = Guid.NewGuid(), RoutineExecution = run,
            SamplingPoint = "SP1", CollectedAt = now
        };
        var certificate = new RoutineCertificate
        {
            Id = Guid.NewGuid(), RoutineExecution = run,
            RoutineSample = sample, CertificateCode = "COA/WATER/TEST",
            Combined = true, RowsJson = "[]", IssuedAt = now,
            IssuedById = Guid.NewGuid()
        };
        var rndBatch = new RndTrialBatch
        {
            Id = Guid.NewGuid(), RndProjectId = Guid.NewGuid(),
            RndFormulationId = Guid.NewGuid(), BatchCode = "TRB-TEST"
        };
        context.AddRange(run, sample, certificate, rndBatch);
        await context.SaveChangesAsync();
        var repository = new WaterQualityRepository(context);
        var actor = Guid.NewGuid();

        var activated = await repository.ActivatePeriod(
            new ActivateWaterQualityPeriodRequest
            {
                RoutineCertificateId = certificate.Id,
                ValidFrom = now, ValidUntil = now.AddDays(30)
            }, actor);
        Assert.True(activated.IsSuccess);
        var use = await repository.RecordUse(new RecordWaterUseRequest
        {
            WaterQualityPeriodId = activated.Value,
            UsedAt = now.AddHours(1), RndTrialBatchId = rndBatch.Id
        }, actor);
        Assert.True(use.IsSuccess);

        var held = await repository.HoldPeriod(activated.Value,
            new HoldWaterQualityPeriodRequest
            {
                Reason = "Investigation following an adverse result"
            }, actor);
        Assert.True(held.IsSuccess);
        Assert.Equal(1, held.Value);
        Assert.Equal(WaterQualityPeriodStatus.Held,
            (await context.WaterQualityPeriods.SingleAsync()).Status);
        Assert.Equal(WaterUseStatus.Held,
            (await context.WaterUseRecords.SingleAsync()).Status);
    }

    [Fact]
    public async Task Retrospective_coverage_requires_a_reason()
    {
        await using var context = Context();
        var now = DateTime.UtcNow;
        var run = new RoutineExecution
        {
            Id = Guid.NewGuid(), RoutineCode = "RUT/WATER/RETRO",
            Type = RoutineType.Water, RoutineDate = now,
            Status = RoutineStatus.Approved
        };
        var sample = new RoutineSample
        {
            Id = Guid.NewGuid(), RoutineExecution = run,
            SamplingPoint = "SP2", CollectedAt = now.AddDays(-2)
        };
        var certificate = new RoutineCertificate
        {
            Id = Guid.NewGuid(), RoutineExecution = run,
            RoutineSample = sample, CertificateCode = "COA/WATER/RETRO",
            RowsJson = "[]", IssuedAt = now, IssuedById = Guid.NewGuid()
        };
        context.AddRange(run, sample, certificate);
        await context.SaveChangesAsync();

        var result = await new WaterQualityRepository(context).ActivatePeriod(
            new ActivateWaterQualityPeriodRequest
            {
                RoutineCertificateId = certificate.Id,
                ValidFrom = now.AddDays(-1), ValidUntil = now.AddDays(29)
            }, Guid.NewGuid());
        Assert.True(result.IsFailure);
        Assert.Equal("WaterQualityPeriod.RetrospectiveReason", result.Error.Code);
    }
}
