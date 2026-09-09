using System.Text.Json;
using APP.Services.Formulas;
using DOMAIN.Entities.Forms;
using DOMAIN.Entities.Formulas;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED.Services.Identity;
using Xunit;

#nullable enable

namespace APP.Tests.Repository;

file sealed class EvidenceTestUser(Guid? userId) : ICurrentUserService
{
    public Guid? UserId => userId;
    public Guid? DepartmentId => null;
    public string DepartmentType => string.Empty;
}

public class FormulaMigrationEvidenceServiceTests
{
    private const string Checksum =
        "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";

    [Fact]
    public async Task RecordDryRun_PersistsImmutableEvidenceWithoutChangingLegacySource()
    {
        await using var context = CreateContext();
        var question = AddFormula(context, "{\"type\":\"0\",\"expression\":\":x+1\"}");
        await context.SaveChangesAsync();
        var inventory = new FormulaMigrationInventoryService(context);
        var discovery = await inventory.DryRunAsync(Request());
        var source = Assert.Single(discovery.Items);
        var decision = new FormulaMigrationDecision(question.Id, source.QuestionOptionId,
            source.LegacyPath, source.SourceHash, FormulaMigrationClass.Exact,
            source.SourceHash, "CreateApprovedRevision", "QA-BATCH-001",
            FormulaMigrationApprovalScope.Batch);
        var request = Request(discovery.SourceFingerprint, [decision]);
        var service = new FormulaMigrationEvidenceService(context, inventory,
            new EvidenceTestUser(Guid.NewGuid()));

        var first = await service.RecordDryRunAsync(request);
        var repeated = await service.RecordDryRunAsync(request);

        Assert.False(first.AlreadyRecorded);
        Assert.True(repeated.AlreadyRecorded);
        Assert.Equal(first.RunId, repeated.RunId);
        Assert.Single(context.FormulaMigrationRuns);
        var run = Assert.Single(context.FormulaMigrationRuns);
        Assert.Equal(1, run.TotalCount);
        Assert.Equal(1, run.SucceededCount);
        Assert.Equal(0, run.FailedCount);
        var item = Assert.Single(context.FormulaMigrationItems);
        Assert.Equal(FormulaMigrationItemStatus.Pending, item.Status);
        Assert.Equal(FormulaMigrationClass.Exact, item.MigrationClass);
        var artifact = Assert.Single(context.LegacyFormulaArtifacts);
        using var payload = JsonDocument.Parse(artifact.OriginalPayload);
        Assert.Equal("{\"type\":\"0\",\"expression\":\":x+1\"}",
            payload.RootElement.GetProperty("raw").GetString());
        Assert.Equal("{\"type\":\"0\",\"expression\":\":x+1\"}",
            context.QuestionOptions.IgnoreQueryFilters().Single().Name);
        Assert.Equal(4, context.FormulaReconciliationResults.Count());
    }

    [Fact]
    public async Task RecordDryRun_RepresentsMissingClassificationWithoutMislabelingIt()
    {
        await using var context = CreateContext();
        AddFormula(context, "not-valid-json");
        await context.SaveChangesAsync();
        var inventory = new FormulaMigrationInventoryService(context);
        var discovery = await inventory.DryRunAsync(Request());
        var service = new FormulaMigrationEvidenceService(context, inventory,
            new EvidenceTestUser(Guid.NewGuid()));

        var receipt = await service.RecordDryRunAsync(
            Request(discovery.SourceFingerprint));

        Assert.False(receipt.Report.CanApply);
        var item = Assert.Single(context.FormulaMigrationItems);
        Assert.Null(item.MigrationClass);
        Assert.Equal(FormulaMigrationItemStatus.Failed, item.Status);
        Assert.Contains("MISSING_DECISION", item.Error);
    }

    [Fact]
    public async Task RecordDryRun_RejectsAnonymousEvidenceWrites()
    {
        await using var context = CreateContext();
        var inventory = new FormulaMigrationInventoryService(context);
        var service = new FormulaMigrationEvidenceService(context, inventory,
            new EvidenceTestUser(null));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.RecordDryRunAsync(Request()));
        Assert.Empty(context.FormulaMigrationRuns);
    }

    [Fact]
    public async Task RecordDryRun_RejectsDifferentDecisionsForRecordedRelease()
    {
        await using var context = CreateContext();
        var question = AddFormula(context, "{\"expression\":\":x+1\"}");
        await context.SaveChangesAsync();
        var inventory = new FormulaMigrationInventoryService(context);
        var discovery = await inventory.DryRunAsync(Request());
        var source = Assert.Single(discovery.Items);
        var exact = Decision(question.Id, source, FormulaMigrationClass.Exact, 'b');
        var corrected = Decision(question.Id, source, FormulaMigrationClass.Corrective, 'c');
        var service = new FormulaMigrationEvidenceService(context, inventory,
            new EvidenceTestUser(Guid.NewGuid()));

        await service.RecordDryRunAsync(Request(discovery.SourceFingerprint, [exact]));

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.RecordDryRunAsync(
            Request(discovery.SourceFingerprint, [corrected])));
        Assert.Single(context.FormulaMigrationRuns);
    }

    private static FormulaMigrationDecision Decision(Guid questionId,
        FormulaMigrationDryRunItem item, FormulaMigrationClass migrationClass,
        char hashCharacter) => new(questionId, item.QuestionOptionId, item.LegacyPath,
        item.SourceHash, migrationClass, new string(hashCharacter, 64),
        "CreateApprovedRevision", "QA-001",
        migrationClass == FormulaMigrationClass.Corrective
            ? FormulaMigrationApprovalScope.Individual
            : FormulaMigrationApprovalScope.Batch);

    private static Question AddFormula(ApplicationDbContext context, string payload)
    {
        var question = new Question
        {
            Id = Guid.NewGuid(),
            Type = QuestionType.Formula,
            Validation = QuestionValidationType.None
        };
        context.Questions.Add(question);
        context.QuestionOptions.Add(new QuestionOption
        {
            Id = Guid.NewGuid(),
            QuestionId = question.Id,
            Name = payload
        });
        return question;
    }

    private static FormulaMigrationDryRunRequest Request(string? fingerprint = null,
        IReadOnlyList<FormulaMigrationDecision>? decisions = null) =>
        new("formula-v1-evidence-test", Checksum, "test-build", fingerprint,
            decisions ?? []);

    private static ApplicationDbContext CreateContext() => new(
        new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options,
        new EvidenceTestUser(null)
    );
}
