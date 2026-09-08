using APP.Services.Formulas;
using Xunit;

namespace APP.Tests.Repository;

public class FormulaMigrationPackageTests
{
    private const string Definition =
        "{\"formulaLanguageVersion\":\"oryx-formula-v1\",\"numericPolicyVersion\":\"oryx-decimal-v1-approved\",\"results\":[],\"variables\":[]}";
    private const string ReportHash =
        "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";

    [Fact]
    public void Seal_IsDeterministicAndOfflineValidationAcceptsThePackage()
    {
        var draft = Draft();

        var first = FormulaMigrationPackage.Seal(
            draft, "validated://formula-v1/release-1", ReportHash);
        var second = FormulaMigrationPackage.Seal(
            draft, "validated://formula-v1/release-1", ReportHash);
        var receipt = FormulaMigrationPackage.ValidateOffline(first, ReportHash);

        Assert.Equal(first.ManifestHash, second.ManifestHash);
        Assert.Equal(1, receipt.TargetCount);
        Assert.Equal(1, receipt.EvidenceCount);
        Assert.Equal(0, receipt.KeyMappingCount);
    }

    [Fact]
    public void ValidateOffline_RejectsTamperingAndWrongReportContent()
    {
        var package = FormulaMigrationPackage.Seal(
            Draft(), "validated://formula-v1/release-1", ReportHash);

        Assert.Throws<InvalidOperationException>(() =>
            FormulaMigrationPackage.ValidateOffline(package with
            {
                Targets = [package.Targets.Single() with { Name = "Tampered" }]
            }));
        Assert.Throws<InvalidOperationException>(() =>
            FormulaMigrationPackage.ValidateOffline(package with
            {
                SignedReportHash = new string('c', 64)
            }));
        Assert.Throws<InvalidOperationException>(() =>
            FormulaMigrationPackage.ValidateOffline(package with
            {
                SignedReportLocation = "validated://different-report"
            }));
        Assert.Throws<InvalidOperationException>(() =>
            FormulaMigrationPackage.ValidateOffline(package, new string('c', 64)));
    }

    private static FormulaMigrationApplyDraft Draft()
    {
        var reviewer = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var approver = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var definitionHash = FormulaCanonicalJson.HashJson(
            "oryx:formula-definition:v1", Definition,
            FormulaMigrationApplyManifest.DefinitionByteLimit);
        var target = new FormulaMigrationTargetPackage(
            "assay-result", "Assay result", null, 1, Definition, "[]",
            definitionHash, "oryx-formula-v1", "oryx-decimal-v1-approved",
            reviewer, approver, "Golden corpus reviewed.", "Migration approved.");
        var evidence = new FormulaMigrationDecisionEvidence(
            Guid.Parse("33333333-3333-3333-3333-333333333333"),
            DOMAIN.Entities.Formulas.FormulaMigrationApprovalScope.Batch,
            "QA-001", reviewer, []);
        return new FormulaMigrationApplyDraft(
            Guid.Parse("44444444-4444-4444-4444-444444444444"), [target], [evidence]);
    }
}
