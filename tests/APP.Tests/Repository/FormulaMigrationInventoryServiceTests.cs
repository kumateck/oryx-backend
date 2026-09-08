using APP.Services.Formulas;
using DOMAIN.Entities.Forms;
using DOMAIN.Entities.Formulas;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED.Services.Identity;
using Xunit;

#nullable enable

namespace APP.Tests.Repository;

file sealed class FormulaMigrationTestUser : ICurrentUserService
{
    public Guid? UserId => null;
    public Guid? DepartmentId => null;
    public string DepartmentType => string.Empty;
}

public class FormulaMigrationInventoryServiceTests
{
    private const string Checksum =
        "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";

    [Fact]
    public async Task DryRun_IsDeterministicAndNeverWritesMigrationRows()
    {
        await using var context = CreateContext();
        var questionId = Guid.NewGuid();
        var optionId = Guid.NewGuid();
        context.Questions.Add(new Question
        {
            Id = questionId,
            Type = QuestionType.Formula,
            Validation = QuestionValidationType.None
        });
        context.QuestionOptions.Add(new QuestionOption
        {
            Id = optionId,
            QuestionId = questionId,
            Name = "{\"type\":\"0\",\"expression\":\":x+1\"}"
        });
        await context.SaveChangesAsync();
        var service = new FormulaMigrationInventoryService(context);

        var discovery = await service.DryRunAsync(Request());
        var artifact = Assert.Single(discovery.Items);
        Assert.Null(discovery.FingerprintMatches);
        Assert.False(discovery.CanApply);
        Assert.Contains("MISSING_DECISION", artifact.Diagnostics);

        var decision = new FormulaMigrationDecision(
            questionId,
            optionId,
            artifact.LegacyPath,
            artifact.SourceHash,
            FormulaMigrationClass.Exact,
            artifact.SourceHash,
            "CreateApprovedRevision",
            "QA-BATCH-001",
            FormulaMigrationApprovalScope.Batch
        );
        var verified = await service.DryRunAsync(Request(discovery.SourceFingerprint, [decision]));
        var repeated = await service.DryRunAsync(Request(discovery.SourceFingerprint, [decision]));

        Assert.True(verified.FingerprintMatches);
        Assert.True(verified.CanApply);
        Assert.Equal(verified.Items[0].LegacyArtifactId, repeated.Items[0].LegacyArtifactId);
        Assert.Equal(verified.Items[0].TargetRevisionId, repeated.Items[0].TargetRevisionId);
        Assert.Empty(context.FormulaMigrationRuns);
        Assert.Empty(context.LegacyFormulaArtifacts);
    }

    [Fact]
    public async Task DryRun_DetectsSourceDrift()
    {
        await using var context = CreateContext();
        var question = new Question
        {
            Id = Guid.NewGuid(),
            Type = QuestionType.Formula,
            Validation = QuestionValidationType.None
        };
        var option = new QuestionOption
        {
            Id = Guid.NewGuid(),
            QuestionId = question.Id,
            Name = "{\"type\":\"0\",\"expression\":\":x+1\"}"
        };
        context.AddRange(question, option);
        await context.SaveChangesAsync();
        var service = new FormulaMigrationInventoryService(context);
        var baseline = await service.DryRunAsync(Request());
        var item = Assert.Single(baseline.Items);
        var decision = new FormulaMigrationDecision(question.Id, option.Id, item.LegacyPath,
            item.SourceHash, FormulaMigrationClass.Exact, item.SourceHash,
            "CreateApprovedRevision", "QA-001", FormulaMigrationApprovalScope.Batch);

        option.Name = "{\"type\":\"0\",\"expression\":\":x+2\"}";
        await context.SaveChangesAsync();
        var drifted = await service.DryRunAsync(Request(baseline.SourceFingerprint, [decision]));

        Assert.False(drifted.FingerprintMatches);
        Assert.False(drifted.CanApply);
        Assert.Contains("SOURCE_HASH_MISMATCH", drifted.Items[0].Diagnostics);
    }

    [Fact]
    public async Task UnrecoverableActivePlacement_BlocksApplyReadiness()
    {
        await using var context = CreateContext();
        var questionId = Guid.NewGuid();
        var formId = Guid.NewGuid();
        var sectionId = Guid.NewGuid();
        context.Questions.Add(new Question
        {
            Id = questionId,
            Type = QuestionType.Formula,
            Validation = QuestionValidationType.None
        });
        context.Forms.Add(new Form { Id = formId, Type = FormType.Default });
        context.FormSections.Add(new FormSection { Id = sectionId, FormId = formId });
        context.FormFields.Add(new FormField
        {
            Id = Guid.NewGuid(),
            FormSectionId = sectionId,
            QuestionId = questionId
        });
        await context.SaveChangesAsync();
        var service = new FormulaMigrationInventoryService(context);
        var discovery = await service.DryRunAsync(Request());
        var item = Assert.Single(discovery.Items);
        var decision = new FormulaMigrationDecision(questionId, null, item.LegacyPath,
            item.SourceHash, FormulaMigrationClass.Unrecoverable, null, "BlockAndReauthor", null);

        var report = await service.DryRunAsync(Request(discovery.SourceFingerprint, [decision]));

        Assert.False(report.CanApply);
        Assert.Equal(1, report.Items[0].ActivePlacements);
        Assert.Contains("ACTIVE_PLACEMENT_UNRECOVERABLE", report.Items[0].Diagnostics);
    }

    [Fact]
    public async Task DuplicateManifestDecision_IsRejected()
    {
        await using var context = CreateContext();
        var service = new FormulaMigrationInventoryService(context);
        var decision = new FormulaMigrationDecision(Guid.NewGuid(), null, "same", Checksum,
            FormulaMigrationClass.Unrecoverable, null, "Block", null);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.DryRunAsync(Request(null, [decision, decision])));
    }

    [Fact]
    public async Task SharedTargetDefinitionHash_DeduplicatesProposedRevisionId()
    {
        await using var context = CreateContext();
        for (var index = 1; index <= 2; index++)
        {
            var questionId = Guid.NewGuid();
            context.Questions.Add(new Question
            {
                Id = questionId,
                Type = QuestionType.Formula,
                Validation = QuestionValidationType.None
            });
            context.QuestionOptions.Add(new QuestionOption
            {
                Id = Guid.NewGuid(),
                QuestionId = questionId,
                Name = $"{{\"type\":\"0\",\"expression\":\":x+{index}\"}}"
            });
        }
        await context.SaveChangesAsync();
        var service = new FormulaMigrationInventoryService(context);
        var discovery = await service.DryRunAsync(Request());
        var sharedTargetHash = new string('b', 64);
        var decisions = discovery.Items.Select(item => new FormulaMigrationDecision(
            item.QuestionId, item.QuestionOptionId, item.LegacyPath, item.SourceHash,
            FormulaMigrationClass.Normalized, sharedTargetHash, "ReuseCanonicalRevision",
            "QA-BATCH-002", FormulaMigrationApprovalScope.Batch
        )).ToList();

        var report = await service.DryRunAsync(
            Request(discovery.SourceFingerprint, decisions)
        );

        Assert.True(report.CanApply);
        Assert.Equal(report.Items[0].TargetRevisionId, report.Items[1].TargetRevisionId);
    }

    [Fact]
    public async Task InvalidManifestMetadata_IsRejectedBeforeDatabaseAccess()
    {
        await using var context = CreateContext();
        var service = new FormulaMigrationInventoryService(context);
        var uppercaseChecksum = Checksum.ToUpperInvariant();

        var checksumError = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.DryRunAsync(new FormulaMigrationDryRunRequest(
                "formula-v1", uppercaseChecksum, "test", null, [])));
        var releaseError = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.DryRunAsync(new FormulaMigrationDryRunRequest(
                new string('x', 101), Checksum, "test", null, [])));

        Assert.Contains("lowercase SHA-256", checksumError.Message);
        Assert.Contains("100 characters", releaseError.Message);
    }

    private static FormulaMigrationDryRunRequest Request(
        string? fingerprint = null,
        IReadOnlyList<FormulaMigrationDecision>? decisions = null
    ) => new("formula-v1-test", Checksum, "test-build", fingerprint, decisions ?? []);

    private static ApplicationDbContext CreateContext() => new(
        new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options,
        new FormulaMigrationTestUser()
    );
}
