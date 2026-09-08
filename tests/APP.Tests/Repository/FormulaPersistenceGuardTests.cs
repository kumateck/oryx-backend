using DOMAIN.Entities.Formulas;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Repository;

file class FormulaGuardCurrentUser : ICurrentUserService
{
    public Guid? UserId => null;
    public Guid? DepartmentId => null;
    public string DepartmentType => string.Empty;
}

public class FormulaPersistenceGuardTests
{
    [Fact]
    public async Task ReviewedRevision_RejectsExecutableContentMutation()
    {
        await using var context = CreateContext();
        var revision = CreateRevision(FormulaRevisionStatus.Draft);
        context.FormulaRevisions.Add(revision);
        await context.SaveChangesAsync();
        revision.Status = FormulaRevisionStatus.InReview;
        context.FormulaRevisionAudits.Add(CreateAudit(revision));
        await context.SaveChangesAsync();

        revision.DefinitionJson = "{\"changed\":true}";

        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => context.SaveChangesAsync()
        );
        Assert.Contains("immutable", error.Message);
    }

    [Fact]
    public async Task Revision_MustBeCreatedAsDraft()
    {
        await using var context = CreateContext();
        context.FormulaRevisions.Add(CreateRevision(FormulaRevisionStatus.Approved));

        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => context.SaveChangesAsync()
        );
        Assert.Contains("created as Draft", error.Message);
    }

    [Fact]
    public async Task ApprovalTransition_RequiresApprovalMetadata()
    {
        await using var context = CreateContext();
        var revision = CreateRevision(FormulaRevisionStatus.Draft);
        context.FormulaRevisions.Add(revision);
        await context.SaveChangesAsync();
        revision.Status = FormulaRevisionStatus.InReview;
        context.FormulaRevisionAudits.Add(CreateAudit(revision));
        await context.SaveChangesAsync();

        revision.Status = FormulaRevisionStatus.Approved;
        var audit = CreateAudit(revision);
        audit.PriorStatus = FormulaRevisionStatus.InReview;
        audit.NewStatus = FormulaRevisionStatus.Approved;
        context.FormulaRevisionAudits.Add(audit);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => context.SaveChangesAsync()
        );
        Assert.Contains("requires approver", error.Message);
    }

    [Fact]
    public async Task ReviewedFormRevision_RejectsContentMutation()
    {
        await using var context = CreateContext();
        var revision = new FormRevision
        {
            Id = Guid.NewGuid(),
            FormId = Guid.NewGuid(),
            Sequence = 1,
            Status = FormRevisionStatus.Draft,
            ContentHash = new string('a', 64)
        };
        context.FormRevisions.Add(revision);
        await context.SaveChangesAsync();
        revision.Status = FormRevisionStatus.InReview;
        context.FormRevisionAudits.Add(CreateFormAudit(
            revision, FormRevisionStatus.Draft, FormRevisionStatus.InReview));
        await context.SaveChangesAsync();
        revision.ContentHash = new string('b', 64);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => context.SaveChangesAsync()
        );
        Assert.Contains("immutable", error.Message);
    }

    [Fact]
    public async Task StatusTransition_RequiresMatchingAuditEvidence()
    {
        await using var context = CreateContext();
        var revision = CreateRevision(FormulaRevisionStatus.Draft);
        context.FormulaRevisions.Add(revision);
        await context.SaveChangesAsync();

        revision.Status = FormulaRevisionStatus.InReview;

        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => context.SaveChangesAsync()
        );
        Assert.Contains("audit record", error.Message);
    }

    [Fact]
    public async Task StatusTransition_WithMatchingAuditEvidence_IsAccepted()
    {
        await using var context = CreateContext();
        var revision = CreateRevision(FormulaRevisionStatus.Draft);
        context.FormulaRevisions.Add(revision);
        await context.SaveChangesAsync();

        revision.Status = FormulaRevisionStatus.InReview;
        context.FormulaRevisionAudits.Add(CreateAudit(revision));

        await context.SaveChangesAsync();
        Assert.Equal(FormulaRevisionStatus.InReview, revision.Status);
    }

    [Fact]
    public async Task IllegalStatusTransition_IsRejectedEvenWithAudit()
    {
        await using var context = CreateContext();
        var revision = CreateRevision(FormulaRevisionStatus.Draft);
        context.FormulaRevisions.Add(revision);
        await context.SaveChangesAsync();

        revision.Status = FormulaRevisionStatus.Approved;
        var audit = CreateAudit(revision);
        audit.NewStatus = FormulaRevisionStatus.Approved;
        context.FormulaRevisionAudits.Add(audit);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => context.SaveChangesAsync()
        );
        Assert.Contains("Illegal", error.Message);
    }

    [Fact]
    public async Task ExecutionEvidence_CannotBeUpdatedOrDeleted()
    {
        await using var context = CreateContext();
        var execution = new FormulaExecution
        {
            Id = Guid.NewGuid(),
            ResponseFormulaSnapshotId = Guid.NewGuid(),
            IdempotencyKey = "attempt-1",
            EngineVersion = "v1",
            EngineBuildHash = new string('c', 64),
            FormulaLanguageVersion = "oryx-formula-v1",
            NumericPolicyVersion = "oryx-decimal-v1-draft",
            InputHash = new string('d', 64),
            ResolvedInputsJson = "{}",
            CalculationTraceJson = "{}"
        };
        context.FormulaExecutions.Add(execution);
        await context.SaveChangesAsync();

        execution.Status = FormulaExecutionStatus.EngineError;
        var updateError = await Assert.ThrowsAsync<InvalidOperationException>(
            () => context.SaveChangesAsync()
        );
        Assert.Contains("append-only", updateError.Message);

        context.Entry(execution).State = EntityState.Deleted;
        var deleteError = await Assert.ThrowsAsync<InvalidOperationException>(
            () => context.SaveChangesAsync()
        );
        Assert.Contains("append-only", deleteError.Message);
    }

    [Fact]
    public async Task SnapshotGeneration_RequiresAValidPredecessorShape()
    {
        await using var context = CreateContext();
        context.ResponseFormulaSnapshots.Add(new ResponseFormulaSnapshot
        {
            Id = Guid.NewGuid(),
            ResponseId = Guid.NewGuid(),
            PlacementKey = "assay",
            Sequence = 2,
            DefinitionHash = new string('a', 64),
            ConfigurationHash = new string('b', 64),
            ExecutableDefinitionJson = "{}",
            BindingsJson = "[]",
            TableShapeJson = "{}",
            CalculationPolicyJson = "{}",
            DisplayPolicyJson = "{}"
        });

        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => context.SaveChangesAsync()
        );
        Assert.Contains("predecessor", error.Message);
    }

    private static FormulaRevision CreateRevision(FormulaRevisionStatus status) => new()
    {
        Id = Guid.NewGuid(),
        FormulaDefinitionId = Guid.NewGuid(),
        Revision = 1,
        DefinitionJson = "{}",
        TestCasesJson = "[]",
        DefinitionHash = new string('a', 64),
        ReleaseEvidenceHash = new string('b', 64),
        FormulaLanguageVersion = "oryx-formula-v1",
        NumericPolicyVersion = "oryx-decimal-v1-draft",
        Status = status
    };

    private static FormulaRevisionAudit CreateAudit(FormulaRevision revision) => new()
    {
        Id = Guid.NewGuid(),
        FormulaRevisionId = revision.Id,
        PriorStatus = FormulaRevisionStatus.Draft,
        NewStatus = FormulaRevisionStatus.InReview,
        Action = "SubmitReview",
        Reason = "Controlled test transition",
        DefinitionHash = revision.DefinitionHash,
        ActorId = Guid.NewGuid(),
        OccurredAt = DateTime.UtcNow,
        CorrelationId = Guid.NewGuid()
    };

    private static FormRevisionAudit CreateFormAudit(
        FormRevision revision, FormRevisionStatus prior, FormRevisionStatus current) => new()
    {
        Id = Guid.NewGuid(), FormRevisionId = revision.Id, PriorStatus = prior,
        NewStatus = current, Action = "Test", Reason = "Controlled test transition.",
        ContentHash = revision.ContentHash, ActorId = Guid.NewGuid(),
        OccurredAt = DateTime.UtcNow, CorrelationId = Guid.NewGuid()
    };

    private static ApplicationDbContext CreateContext() => new(
        new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options,
        new FormulaGuardCurrentUser()
    );
}
