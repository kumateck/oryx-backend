using APP.Services.Formulas;
using DOMAIN.Entities.Forms;
using DOMAIN.Entities.Formulas;
using DOMAIN.Entities.Users;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Repository;

internal sealed class ApplyTestUser(Guid? userId) : ICurrentUserService
{
    public Guid? UserId => userId;
    public Guid? DepartmentId => null;
    public string DepartmentType => string.Empty;
}

internal sealed record ApplyTestSetup(
    ApplicationDbContext Context,
    FormulaMigrationInventoryService Inventory,
    Guid ActorId,
    Guid ReviewerId,
    Guid ApproverId,
    QuestionOption Option,
    FormulaMigrationApplyRequest Request
) : IAsyncDisposable
{
    public ValueTask DisposeAsync() => Context.DisposeAsync();
}

internal static class FormulaMigrationApplyTestFixture
{
    private const string Checksum =
        "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
    private const string Definition =
        "{\"formulaLanguageVersion\":\"oryx-formula-v1\",\"numericPolicyVersion\":\"oryx-decimal-v1-approved\",\"results\":[],\"variables\":[]}";

    public static async Task<ApplyTestSetup> CreateAsync(
        FormulaMigrationClass migrationClass = FormulaMigrationClass.Exact)
    {
        var actorId = Guid.NewGuid();
        var reviewerId = Guid.NewGuid();
        var approverId = Guid.NewGuid();
        var currentUser = new ApplyTestUser(actorId);
        var context = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options,
            currentUser);
        context.Users.AddRange(User(actorId), User(reviewerId), User(approverId));
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
        var inventory = new FormulaMigrationInventoryService(context);
        var discovery = await inventory.DryRunAsync(
            new FormulaMigrationDryRunRequest("formula-v1-apply-test", Checksum,
                "test-build", null, []));
        var source = Assert.Single(discovery.Items);
        var targetHash = FormulaCanonicalJson.HashJson(
            "oryx:formula-definition:v1", Definition,
            FormulaMigrationApplyManifest.DefinitionByteLimit);
        var approvalScope = migrationClass == FormulaMigrationClass.Corrective
            ? FormulaMigrationApprovalScope.Individual
            : FormulaMigrationApprovalScope.Batch;
        var decision = new FormulaMigrationDecision(question.Id, option.Id,
            source.LegacyPath, source.SourceHash, migrationClass, targetHash,
            "CreateApprovedRevision", "QA-APPROVAL-001", approvalScope);
        var dryRequest = new FormulaMigrationDryRunRequest("formula-v1-apply-test",
            Checksum, "test-build", discovery.SourceFingerprint, [decision]);
        var evidenceService = new FormulaMigrationEvidenceService(
            context, inventory, currentUser);
        var dryRun = await evidenceService.RecordDryRunAsync(dryRequest);
        var target = new FormulaMigrationTargetPackage("assay-result", "Assay result",
            null, 1, Definition, "[]", targetHash, "oryx-formula-v1",
            "oryx-decimal-v1-approved", reviewerId, approverId,
            "Reviewed against approved golden corpus.",
            "Approved for controlled migration.");
        var evidence = new FormulaMigrationDecisionEvidence(source.LegacyArtifactId,
            approvalScope, "QA-APPROVAL-001", reviewerId, []);
        var unsigned = new FormulaMigrationApplyRequest(dryRun.RunId,
            "validated://formula-v1-apply-test", new string('b', 64),
            new string('0', 64), [target], [evidence]);
        var request = unsigned with
        {
            ManifestHash = FormulaMigrationApplyManifest.ComputeHash(unsigned)
        };
        return new ApplyTestSetup(context, inventory, actorId, reviewerId,
            approverId, option, request);
    }

    public static FormulaMigrationApplyService Service(ApplyTestSetup setup) => new(
        setup.Context, setup.Inventory, new ApplyTestUser(setup.ActorId));

    private static User User(Guid id) => new()
    {
        Id = id,
        UserName = $"user-{id:N}",
        FirstName = "Formula",
        LastName = "Reviewer",
        Title = string.Empty,
        Avatar = string.Empty,
        Signature = string.Empty
    };
}
