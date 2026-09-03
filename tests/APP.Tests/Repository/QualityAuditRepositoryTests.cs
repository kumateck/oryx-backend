using APP.Mapper;
using APP.Repository;
using AutoMapper;
using DOMAIN.Entities.QualityAudits;
using INFRASTRUCTURE.Context;
using Microsoft.AspNetCore.Http;
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

file class NullHttpContextAccessor : IHttpContextAccessor
{
    public HttpContext HttpContext { get; set; }
}

public class QualityAuditRepositoryTests
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

    private static QualityAuditRepository CreateRepository(ApplicationDbContext context) =>
        new(context, CreateMapper(), null!, new NullHttpContextAccessor());

    private static async Task<QualityAudit> SeedAudit(ApplicationDbContext context, AuditStatus status)
    {
        var audit = new QualityAudit
        {
            Id = Guid.NewGuid(),
            AuditNumber = "AUD-2026-0001",
            Type = AuditType.InternalSelfInspection,
            FocusArea = AuditFocus.Process,
            Title = "Manufacturing floor self-inspection",
            Scope = "Production area",
            Status = status,
            ScheduledStartDate = DateTime.UtcNow,
            ScheduledEndDate = DateTime.UtcNow.AddDays(1),
            LeadAuditorId = Guid.NewGuid(),
        };
        context.QualityAudits.Add(audit);
        await context.SaveChangesAsync();
        return audit;
    }

    [Fact]
    public async Task CreateAudit_generates_sequential_audit_number()
    {
        await using var context = CreateContext();
        var repository = CreateRepository(context);

        var firstResult = await repository.CreateAudit(
            new CreateQualityAuditRequest
            {
                Type = AuditType.InternalSelfInspection,
                FocusArea = AuditFocus.System,
                Title = "First audit",
                Scope = "Warehouse",
                ScheduledStartDate = DateTime.UtcNow,
                ScheduledEndDate = DateTime.UtcNow.AddDays(1),
                LeadAuditorId = Guid.NewGuid(),
            },
            Guid.NewGuid()
        );
        var secondResult = await repository.CreateAudit(
            new CreateQualityAuditRequest
            {
                Type = AuditType.InternalSelfInspection,
                FocusArea = AuditFocus.System,
                Title = "Second audit",
                Scope = "Warehouse",
                ScheduledStartDate = DateTime.UtcNow,
                ScheduledEndDate = DateTime.UtcNow.AddDays(1),
                LeadAuditorId = Guid.NewGuid(),
            },
            Guid.NewGuid()
        );

        Assert.True(firstResult.IsSuccess);
        Assert.True(secondResult.IsSuccess);
        var first = await context.QualityAudits.FirstAsync(a => a.Id == firstResult.Value);
        var second = await context.QualityAudits.FirstAsync(a => a.Id == secondResult.Value);
        Assert.NotEqual(first.AuditNumber, second.AuditNumber);
    }

    [Fact]
    public async Task StartAudit_fails_when_not_planned()
    {
        await using var context = CreateContext();
        var audit = await SeedAudit(context, AuditStatus.InProgress);
        var repository = CreateRepository(context);

        var result = await repository.StartAudit(audit.Id, Guid.NewGuid());

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task SubmitForClosure_fails_while_a_corrective_action_is_open()
    {
        await using var context = CreateContext();
        var audit = await SeedAudit(context, AuditStatus.InProgress);
        var finding = new AuditFinding
        {
            Id = Guid.NewGuid(),
            QualityAuditId = audit.Id,
            Title = "Calibration overdue",
            Description = "Balance #4 calibration expired",
            Severity = FindingSeverity.Major,
            Status = FindingStatus.CapaRaised,
            RaisedAt = DateTime.UtcNow,
        };
        context.AuditFindings.Add(finding);
        context.AuditCorrectiveActions.Add(
            new AuditCorrectiveAction
            {
                Id = Guid.NewGuid(),
                AuditFindingId = finding.Id,
                RootCauseAnalysis = "Calibration schedule missed",
                CorrectiveActions = "Recalibrate balance",
                ResponsiblePersonId = Guid.NewGuid(),
                DueDate = DateTime.UtcNow.AddDays(7),
                Status = CapaStatus.Open,
            }
        );
        await context.SaveChangesAsync();

        var repository = CreateRepository(context);
        var result = await repository.SubmitForClosure(
            audit.Id,
            new SubmitAuditForClosureRequest { ClosingMeetingNotes = "Discussed findings" },
            Guid.NewGuid()
        );

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task VerifyCorrectiveActionEffectiveness_effective_closes_capa_and_finding()
    {
        await using var context = CreateContext();
        var audit = await SeedAudit(context, AuditStatus.InProgress);
        var finding = new AuditFinding
        {
            Id = Guid.NewGuid(),
            QualityAuditId = audit.Id,
            Title = "Calibration overdue",
            Description = "Balance #4 calibration expired",
            Severity = FindingSeverity.Major,
            Status = FindingStatus.CapaRaised,
            RaisedAt = DateTime.UtcNow,
        };
        context.AuditFindings.Add(finding);
        var capa = new AuditCorrectiveAction
        {
            Id = Guid.NewGuid(),
            AuditFindingId = finding.Id,
            RootCauseAnalysis = "Calibration schedule missed",
            CorrectiveActions = "Recalibrate balance",
            ResponsiblePersonId = Guid.NewGuid(),
            DueDate = DateTime.UtcNow.AddDays(7),
            Status = CapaStatus.PendingVerification,
        };
        context.AuditCorrectiveActions.Add(capa);
        await context.SaveChangesAsync();

        var repository = CreateRepository(context);
        var result = await repository.VerifyCorrectiveActionEffectiveness(
            capa.Id,
            new VerifyCorrectiveActionEffectivenessRequest { Effective = true, EffectivenessCheckNotes = "Recalibrated and verified" },
            Guid.NewGuid()
        );

        Assert.True(result.IsSuccess);
        var updatedCapa = await context.AuditCorrectiveActions.FirstAsync(c => c.Id == capa.Id);
        var updatedFinding = await context.AuditFindings.FirstAsync(f => f.Id == finding.Id);
        Assert.Equal(CapaStatus.Closed, updatedCapa.Status);
        Assert.Equal(FindingStatus.Closed, updatedFinding.Status);
    }

    [Fact]
    public async Task RaiseCorrectiveAction_fails_when_one_already_exists_for_the_finding()
    {
        await using var context = CreateContext();
        var audit = await SeedAudit(context, AuditStatus.InProgress);
        var finding = new AuditFinding
        {
            Id = Guid.NewGuid(),
            QualityAuditId = audit.Id,
            Title = "Missing line clearance record",
            Description = "No line clearance record found for batch",
            Severity = FindingSeverity.Critical,
            Status = FindingStatus.CapaRaised,
            RaisedAt = DateTime.UtcNow,
        };
        context.AuditFindings.Add(finding);
        context.AuditCorrectiveActions.Add(
            new AuditCorrectiveAction
            {
                Id = Guid.NewGuid(),
                AuditFindingId = finding.Id,
                RootCauseAnalysis = "Operator skipped step",
                CorrectiveActions = "Retrain operator",
                ResponsiblePersonId = Guid.NewGuid(),
                DueDate = DateTime.UtcNow.AddDays(7),
                Status = CapaStatus.Open,
            }
        );
        await context.SaveChangesAsync();

        var repository = CreateRepository(context);
        var result = await repository.RaiseCorrectiveAction(
            finding.Id,
            new RaiseCorrectiveActionRequest
            {
                RootCauseAnalysis = "Duplicate",
                CorrectiveActions = "Duplicate",
                ResponsiblePersonId = Guid.NewGuid(),
                DueDate = DateTime.UtcNow.AddDays(7),
            },
            Guid.NewGuid()
        );

        Assert.False(result.IsSuccess);
    }
}
