using System.Text.Json;
using APP.Repository;
using DOMAIN.Entities.Configurations;
using DOMAIN.Entities.Forms;
using DOMAIN.Entities.QualityRoutines;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Repository;

file class RoutineCurrentUser : ICurrentUserService
{
    public Guid? UserId => null;
    public Guid? DepartmentId => null;
    public string DepartmentType => string.Empty;
}

public class RoutineQcRepositoryTests
{
    private static ApplicationDbContext Context() => new(
        new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options,
        new RoutineCurrentUser());

    private static RoutineQcRepository Repository(ApplicationDbContext context) =>
        new(context, null!, new ConfigurationRepository(context, null!));

    private static async Task SeedRoutineCodeConfig(ApplicationDbContext context)
    {
        context.Configurations.Add(new Configuration
        {
            Id = Guid.NewGuid(), ModelType = nameof(RoutineExecution),
            Prefix = "RUT", NamingType = NamingType.Series,
            MinimumNameLength = 1, MaximumNameLength = 20
        });
        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task Emergency_execution_has_no_schedule_and_requires_reason()
    {
        await using var context = Context();
        await SeedRoutineCodeConfig(context);
        var repository = Repository(context);
        var invalid = await repository.CreateExecution(new CreateRoutineExecutionRequest
        {
            Origin = RoutineOrigin.Emergency, Type = RoutineType.Environmental,
            RoutineDate = DateTime.UtcNow, EmergencyTrigger = "OOS"
        }, Guid.NewGuid());
        Assert.True(invalid.IsFailure);

        var valid = await repository.CreateExecution(new CreateRoutineExecutionRequest
        {
            Origin = RoutineOrigin.Emergency, Type = RoutineType.Environmental,
            RoutineDate = DateTime.UtcNow, EmergencyTrigger = "OOS",
            EmergencyReason = "Investigate airborne count"
        }, Guid.NewGuid());
        Assert.True(valid.IsSuccess);
        var run = await context.RoutineExecutions.SingleAsync();
        Assert.Null(run.Cadence);
        Assert.Null(run.PeriodStart);
        Assert.Null(run.RoutineDefinitionId);
        Assert.StartsWith("RUT-", run.RoutineCode);
    }

    [Fact]
    public async Task Scheduled_period_uses_calendar_cadence_and_can_repeat()
    {
        await using var context = Context();
        await SeedRoutineCodeConfig(context);
        var repository = Repository(context);
        var definition = await repository.CreateDefinition(
            new CreateRoutineDefinitionRequest
            {
                Name = "Monthly water", Type = RoutineType.Water,
                Cadence = RoutineCadence.Monthly
            }, Guid.NewGuid());
        Assert.True(definition.IsSuccess);
        var request = new CreateRoutineExecutionRequest
        {
            Origin = RoutineOrigin.Scheduled, Type = RoutineType.Water,
            RoutineDefinitionId = definition.Value,
            RoutineDate = new DateTime(2026, 9, 15, 0, 0, 0, DateTimeKind.Utc),
            PeriodStart = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
            PeriodEnd = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc)
        };
        var first = await repository.CreateExecution(request, Guid.NewGuid());
        var second = await repository.CreateExecution(request, Guid.NewGuid());
        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.NotEqual(first.Value, second.Value);
        Assert.Equal(2, await context.RoutineExecutions.CountAsync());

        request.PeriodEnd = request.PeriodStart.Value.AddMonths(3);
        Assert.True((await repository.CreateExecution(request, Guid.NewGuid())).IsFailure);
    }

    [Fact]
    public async Task Certificate_requires_both_approved_water_tracks_and_selected_results()
    {
        await using var context = Context();
        var repository = Repository(context);
        var run = new RoutineExecution
        {
            Id = Guid.NewGuid(), RoutineCode = "RUT/WATER/TEST",
            Type = RoutineType.Water, Origin = RoutineOrigin.Emergency,
            RoutineDate = DateTime.UtcNow, Status = RoutineStatus.InProgress
        };
        var sample = new RoutineSample
        {
            Id = Guid.NewGuid(), RoutineExecution = run,
            SamplingPoint = "SP1", CollectedAt = DateTime.UtcNow
        };
        var chemicalField = Guid.NewGuid();
        var microbialField = Guid.NewGuid();
        var chemical = Track(sample, AnalysisType.Chemical, chemicalField, "pH");
        var microbial = Track(sample, AnalysisType.Microbial, microbialField, "Microbial count");
        chemical.Response.Approved = true;
        context.RoutineSamples.Add(sample);
        context.RoutineTracks.AddRange(chemical, microbial);
        await context.SaveChangesAsync();

        var pending = await repository.GenerateCertificate(sample.Id, Guid.NewGuid());
        Assert.True(pending.IsFailure);
        microbial.Response.Approved = true;
        var issued = await repository.GenerateCertificate(sample.Id, Guid.NewGuid());
        Assert.True(issued.IsSuccess);
        var certificate = await context.RoutineCertificates.SingleAsync();
        Assert.True(certificate.Combined);
        using var rows = JsonDocument.Parse(certificate.RowsJson);
        Assert.Equal(2, rows.RootElement.GetArrayLength());
        Assert.Equal("pH", rows.RootElement[0].GetProperty("DisplayLabel").GetString());
        Assert.True((await repository.GenerateCertificate(sample.Id, Guid.NewGuid())).IsFailure);
    }

    [Fact]
    public async Task Environmental_certificate_covers_every_monitored_area_once()
    {
        await using var context = Context();
        var repository = Repository(context);
        var run = new RoutineExecution
        {
            Id = Guid.NewGuid(), RoutineCode = "RUT/ENV/TEST",
            Type = RoutineType.Environmental, Origin = RoutineOrigin.Scheduled,
            RoutineDate = DateTime.UtcNow, Status = RoutineStatus.InProgress
        };
        var first = new RoutineSample
        {
            Id = Guid.NewGuid(), RoutineExecution = run,
            AreaName = "Filling Room", CollectedAt = DateTime.UtcNow
        };
        var second = new RoutineSample
        {
            Id = Guid.NewGuid(), RoutineExecution = run,
            AreaName = "Dispensing Booth", CollectedAt = DateTime.UtcNow
        };
        var firstTrack = Track(first, AnalysisType.Microbial, Guid.NewGuid(), "Airborne viables");
        var secondTrack = Track(second, AnalysisType.Microbial, Guid.NewGuid(), "Airborne viables");
        firstTrack.Response.Approved = true;
        context.RoutineSamples.AddRange(first, second);
        context.RoutineTracks.AddRange(firstTrack, secondTrack);
        await context.SaveChangesAsync();

        Assert.True((await repository.GenerateCertificate(first.Id, Guid.NewGuid())).IsFailure);
        secondTrack.Response.Approved = true;
        var issued = await repository.GenerateCertificate(first.Id, Guid.NewGuid());
        Assert.True(issued.IsSuccess);
        var certificate = await context.RoutineCertificates.SingleAsync();
        Assert.False(certificate.Combined);
        Assert.Null(certificate.RoutineSampleId);
        using var rows = JsonDocument.Parse(certificate.RowsJson);
        Assert.Equal(2, rows.RootElement.GetArrayLength());
        var identities = rows.RootElement.EnumerateArray()
            .Select(row => row.GetProperty("sampleIdentity").GetString()).ToList();
        Assert.Contains("Filling Room", identities);
        Assert.Contains("Dispensing Booth", identities);
        Assert.True((await repository.GenerateCertificate(second.Id, Guid.NewGuid())).IsFailure);
    }

    private static RoutineTrack Track(RoutineSample sample, AnalysisType type,
        Guid fieldId, string label)
    {
        var track = new RoutineTrack
        {
            Id = Guid.NewGuid(), RoutineSample = sample,
            AnalysisType = type, RoutineArdId = Guid.NewGuid(),
            FormId = Guid.NewGuid(), CoaItemsSnapshotJson = JsonSerializer.Serialize(
                new[] { new { FormFieldId = fieldId, DisplayLabel = label,
                    GroupName = "", SpecificationText = "Limit", Unit = "",
                    Reference = "" } })
        };
        track.Response = new Response
        {
            Id = Guid.NewGuid(), FormId = track.FormId, RoutineTrack = track,
            FormResponses = [new FormResponse
            {
                Id = Guid.NewGuid(), FormFieldId = fieldId,
                Value = "Complies", Complies = true
            }]
        };
        return track;
    }
}
