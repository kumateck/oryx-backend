using APP.Services.Formulas;
using DOMAIN.Entities.Formulas;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace APP.Tests.Repository;

public class FormulaMigrationApplyServiceTests
{
    [Fact]
    public async Task Apply_PersistsApprovedDefinitionsAndAuditWithoutChangingLegacySource()
    {
        await using var setup = await FormulaMigrationApplyTestFixture.CreateAsync();
        var service = FormulaMigrationApplyTestFixture.Service(setup);
        var original = setup.Option.Name;

        var receipt = await service.ApplyApprovedDefinitionsAsync(setup.Request);

        Assert.False(receipt.AlreadyApplied);
        Assert.Equal(1, receipt.DefinitionsCreated);
        Assert.Equal(1, receipt.RevisionsCreated);
        Assert.Equal(1, receipt.QuestionLinksCreated);
        var definition = Assert.Single(setup.Context.FormulaDefinitions);
        Assert.Equal("assay-result", definition.Key);
        var revision = Assert.Single(setup.Context.FormulaRevisions);
        Assert.Equal(FormulaRevisionStatus.Approved, revision.Status);
        Assert.Equal(setup.Request.SignedReportHash, revision.ReleaseEvidenceHash);
        Assert.Equal(2, setup.Context.FormulaRevisionAudits.Count());
        Assert.Single(setup.Context.QuestionFormulaDefinitions);
        Assert.Equal(original, setup.Context.QuestionOptions.Single().Name);
        var run = setup.Context.FormulaMigrationRuns.Single(item =>
            item.Mode == FormulaMigrationRunMode.Apply);
        Assert.Equal(FormulaMigrationRunStatus.Completed, run.Status);
        Assert.Equal(5, setup.Context.FormulaReconciliationResults.Count(item =>
            item.FormulaMigrationRunId == run.Id));
    }

    [Fact]
    public async Task Apply_IsIdempotentForTheSameSignedManifest()
    {
        await using var setup = await FormulaMigrationApplyTestFixture.CreateAsync();
        var service = FormulaMigrationApplyTestFixture.Service(setup);

        var first = await service.ApplyApprovedDefinitionsAsync(setup.Request);
        var repeated = await service.ApplyApprovedDefinitionsAsync(setup.Request);

        Assert.False(first.AlreadyApplied);
        Assert.True(repeated.AlreadyApplied);
        Assert.Single(setup.Context.FormulaDefinitions);
        Assert.Single(setup.Context.FormulaRevisions);
        Assert.Equal(2, setup.Context.FormulaRevisionAudits.Count());
    }

    [Fact]
    public async Task Apply_RejectsLegacySourceDriftBeforeWritingTargets()
    {
        await using var setup = await FormulaMigrationApplyTestFixture.CreateAsync();
        setup.Option.Name = "{\"type\":\"0\",\"expression\":\":x+2\"}";
        await setup.Context.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            FormulaMigrationApplyTestFixture.Service(setup)
                .ApplyApprovedDefinitionsAsync(setup.Request));

        Assert.Empty(setup.Context.FormulaDefinitions);
        Assert.DoesNotContain(setup.Context.FormulaMigrationRuns,
            item => item.Mode == FormulaMigrationRunMode.Apply);
    }

    [Fact]
    public async Task Apply_RejectsTamperedDefinitionAndSeparationOfDutiesViolation()
    {
        await using var setup = await FormulaMigrationApplyTestFixture.CreateAsync();
        var target = setup.Request.Targets.Single();
        var tampered = setup.Request with
        {
            Targets = [target with { Name = "Tampered" }]
        };
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            FormulaMigrationApplyTestFixture.Service(setup)
                .ApplyApprovedDefinitionsAsync(tampered));

        var badTarget = target with { ReviewedById = setup.ActorId };
        var unsigned = setup.Request with
        {
            Targets = [badTarget],
            ManifestHash = new string('0', 64)
        };
        var badDuties = unsigned with
        {
            ManifestHash = FormulaMigrationApplyManifest.ComputeHash(unsigned)
        };
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            FormulaMigrationApplyTestFixture.Service(setup)
                .ApplyApprovedDefinitionsAsync(badDuties));
        Assert.Empty(setup.Context.FormulaDefinitions);
    }
}
