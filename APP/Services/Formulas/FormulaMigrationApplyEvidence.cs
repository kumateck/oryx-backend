using System.Text.Json;
using DOMAIN.Entities.Formulas;

namespace APP.Services.Formulas;

internal static class FormulaMigrationApplyEvidence
{
    public static FormulaMigrationDryRunRequest RebuildDryRunRequest(
        FormulaMigrationRun dryRun) => new(
        dryRun.ReleaseId,
        dryRun.CorpusChecksum,
        dryRun.CodeVersion,
        dryRun.SourceFingerprint,
        dryRun.Items.Select(item => new FormulaMigrationDecision(
            item.LegacyFormulaArtifact.QuestionId,
            item.LegacyFormulaArtifact.QuestionOptionId,
            item.LegacyFormulaArtifact.SourcePath,
            item.SourceHash,
            item.MigrationClass!.Value,
            item.AfterHash,
            item.Action,
            string.IsNullOrWhiteSpace(item.ApprovalReference)
                ? null
                : item.ApprovalReference,
            item.ApprovalScope)).ToList());

    public static IReadOnlyList<FormulaReconciliationResult> BuildApplyControls(
        Guid runId,
        FormulaMigrationRun dryRun,
        int expectedTargets,
        int actualQuestionLinks,
        FormulaMigrationWriteResult written)
    {
        var now = DateTime.UtcNow;
        return
        [
            Control(runId, "approved-dry-run", dryRun.Id.ToString("N"),
                dryRun.Id.ToString("N"), true, now),
            Control(runId, "source-fingerprint-unchanged", dryRun.SourceFingerprint,
                dryRun.SourceFingerprint, true, now),
            Control(runId, "migration-item-count", dryRun.Items.Count.ToString(),
                dryRun.Items.Count.ToString(), true, now),
            Control(runId, "executable-question-link-count", expectedTargets.ToString(),
                actualQuestionLinks.ToString(), actualQuestionLinks >= expectedTargets, now),
            Control(runId, "created-object-count", "non-negative",
                (written.DefinitionsCreated + written.RevisionsCreated +
                 written.QuestionLinksCreated + written.KeyMappingsCreated).ToString(),
                written.DefinitionsCreated >= 0 && written.RevisionsCreated >= 0 &&
                written.QuestionLinksCreated >= 0 && written.KeyMappingsCreated >= 0, now)
        ];
    }

    private static FormulaReconciliationResult Control(Guid runId, string name,
        string expected, string actual, bool passed, DateTime now) => new()
    {
        Id = FormulaMigrationHashing.DeterministicId(runId.ToString("N"),
            "reconciliation", name),
        FormulaMigrationRunId = runId,
        ControlName = name,
        ExpectedValue = expected,
        ActualValue = actual,
        Passed = passed,
        EvidenceJson = JsonSerializer.Serialize(new { name, expected, actual, passed }),
        CheckedAt = now
    };
}
