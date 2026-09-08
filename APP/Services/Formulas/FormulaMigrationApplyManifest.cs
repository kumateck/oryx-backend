using System.Text.Json;

namespace APP.Services.Formulas;

public static class FormulaMigrationApplyManifest
{
    public const int DefinitionByteLimit = 256 * 1024;
    public const int TestCasesByteLimit = 2 * 1024 * 1024;
    public const int ManifestByteLimit = 16 * 1024 * 1024;
    public const int TargetLimit = 1_000;

    public static string ComputeHash(FormulaMigrationApplyRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var targets = request.Targets.OrderBy(item => item.DefinitionKey,
                StringComparer.Ordinal)
            .ThenBy(item => item.Revision)
            .Select(item => new
            {
                item.DefinitionKey,
                item.Name,
                item.PresentationPreset,
                item.Revision,
                DefinitionJson = FormulaCanonicalJson.Canonicalize(
                    item.DefinitionJson, DefinitionByteLimit),
                TestCasesJson = FormulaCanonicalJson.Canonicalize(
                    item.TestCasesJson, TestCasesByteLimit),
                item.DefinitionHash,
                item.FormulaLanguageVersion,
                item.NumericPolicyVersion,
                ReviewedById = item.ReviewedById.ToString("N"),
                ApprovedById = item.ApprovedById.ToString("N"),
                item.ReviewReason,
                item.ApprovalReason
            });
        var evidence = request.DecisionEvidence
            .OrderBy(item => item.LegacyArtifactId)
            .Select(item => new
            {
                LegacyArtifactId = item.LegacyArtifactId.ToString("N"),
                ApprovalScope = (int)item.ApprovalScope,
                item.ApprovalReference,
                ReviewedById = item.ReviewedById.ToString("N"),
                KeyMappings = item.KeyMappings
                    .OrderBy(mapping => mapping.KeyKind, StringComparer.Ordinal)
                    .ThenBy(mapping => mapping.LegacyPath, StringComparer.Ordinal)
                    .ThenBy(mapping => mapping.LegacyKey, StringComparer.Ordinal)
            });
        var json = JsonSerializer.Serialize(new
        {
            Version = "oryx-formula-migration-apply-v1",
            DryRunId = request.DryRunId.ToString("N"),
            request.SignedReportLocation,
            request.SignedReportHash,
            Targets = targets,
            DecisionEvidence = evidence
        });
        return FormulaCanonicalJson.HashJson(
            "oryx:formula-migration-apply-manifest:v1", json, ManifestByteLimit);
    }
}
