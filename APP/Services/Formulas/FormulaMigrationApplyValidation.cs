using System.Text.Json;
using System.Text.RegularExpressions;
using DOMAIN.Entities.Formulas;

namespace APP.Services.Formulas;

internal sealed record ValidatedFormulaTarget(
    FormulaMigrationTargetPackage Source,
    Guid DefinitionId,
    Guid RevisionId,
    string CanonicalDefinition,
    string CanonicalTestCases
);

internal sealed record ValidatedFormulaApplyPackage(
    IReadOnlyDictionary<string, ValidatedFormulaTarget> TargetsByHash,
    IReadOnlyDictionary<Guid, FormulaMigrationDecisionEvidence> EvidenceByArtifact
);

internal static partial class FormulaMigrationApplyValidation
{
    [GeneratedRegex("^[A-Za-z0-9][A-Za-z0-9._-]{0,119}$")]
    private static partial Regex StableKeyPattern();

    public static ValidatedFormulaApplyPackage Validate(
        FormulaMigrationApplyRequest request,
        FormulaMigrationRun dryRun,
        IReadOnlyList<FormulaReconciliationResult> controls,
        Guid actorId)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(dryRun);
        ArgumentNullException.ThrowIfNull(controls);
        ValidateRequestEnvelope(request);
        ValidateDryRun(dryRun, controls);
        var targets = ValidateTargets(request.Targets, actorId);
        var evidence = ValidateEvidence(request.DecisionEvidence);
        ValidateCoverage(dryRun.Items, targets, evidence);
        return new ValidatedFormulaApplyPackage(targets, evidence);
    }

    private static void ValidateRequestEnvelope(FormulaMigrationApplyRequest request)
    {
        if (request.DryRunId == Guid.Empty)
            throw new ArgumentException("A recorded dry-run ID is required.");
        if (string.IsNullOrWhiteSpace(request.SignedReportLocation) ||
            request.SignedReportLocation.Length > 2000)
            throw new ArgumentException(
                "Signed report location must contain 1 to 2000 characters.");
        if (!FormulaMigrationHashing.IsSha256(request.SignedReportHash))
            throw new ArgumentException("Signed report hash must be a lowercase SHA-256.");
        if (!FormulaMigrationHashing.IsSha256(request.ManifestHash))
            throw new ArgumentException("Apply manifest hash must be a lowercase SHA-256.");
        if (request.Targets is null || request.Targets.Count is 0 ||
            request.Targets.Count > FormulaMigrationApplyManifest.TargetLimit)
            throw new ArgumentException(
                $"Apply target count must be between 1 and {FormulaMigrationApplyManifest.TargetLimit}.");
        if (request.DecisionEvidence is null)
            throw new ArgumentException("Decision evidence is required.");
        if (FormulaMigrationApplyManifest.ComputeHash(request) != request.ManifestHash)
            throw new InvalidOperationException(
                "Apply manifest hash does not match the request content.");
    }

    private static void ValidateDryRun(FormulaMigrationRun dryRun,
        IReadOnlyList<FormulaReconciliationResult> controls)
    {
        if (dryRun.Mode != FormulaMigrationRunMode.DryRun ||
            dryRun.Status != FormulaMigrationRunStatus.Completed)
            throw new InvalidOperationException("Apply requires a completed dry-run ledger.");
        if (dryRun.FailedCount != 0 || dryRun.TotalCount != dryRun.Items.Count ||
            controls.Count == 0 || controls.Any(item => !item.Passed))
            throw new InvalidOperationException(
                "Apply requires a complete dry run with every reconciliation control passing.");
        if (dryRun.Items.Any(item => !item.MigrationClass.HasValue ||
            item.Status == FormulaMigrationItemStatus.Failed ||
            !string.IsNullOrWhiteSpace(item.Error)))
            throw new InvalidOperationException(
                "Apply cannot use unclassified or failed dry-run items.");
    }

    private static IReadOnlyDictionary<string, ValidatedFormulaTarget> ValidateTargets(
        IReadOnlyList<FormulaMigrationTargetPackage> targets, Guid? actorId)
    {
        var byHash = new Dictionary<string, ValidatedFormulaTarget>(StringComparer.Ordinal);
        var revisionKeys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var target in targets)
        {
            ValidateTargetMetadata(target, actorId);
            var canonicalDefinition = FormulaCanonicalJson.Canonicalize(target.DefinitionJson,
                FormulaMigrationApplyManifest.DefinitionByteLimit);
            var actualHash = FormulaCanonicalJson.HashCanonical(
                "oryx:formula-definition:v1", canonicalDefinition);
            if (actualHash != target.DefinitionHash)
                throw new InvalidOperationException(
                    $"Definition hash mismatch for {target.DefinitionKey} revision {target.Revision}.");
            ValidateEmbeddedVersions(target, canonicalDefinition);
            var canonicalTests = FormulaCanonicalJson.Canonicalize(target.TestCasesJson,
                FormulaMigrationApplyManifest.TestCasesByteLimit);
            using (var tests = JsonDocument.Parse(canonicalTests))
            {
                if (tests.RootElement.ValueKind != JsonValueKind.Array)
                    throw new ArgumentException("Formula test cases must be a JSON array.");
            }
            var revisionKey = $"{target.DefinitionKey}:{target.Revision}";
            if (!revisionKeys.Add(revisionKey))
                throw new ArgumentException($"Duplicate target revision {revisionKey}.");
            var validated = new ValidatedFormulaTarget(target,
                FormulaMigrationHashing.DeterministicId("canonical-definition-v1",
                    "formula-definition", target.DefinitionKey),
                FormulaMigrationHashing.DeterministicId("canonical-definition-v1",
                    "formula-revision", target.DefinitionHash),
                canonicalDefinition, canonicalTests);
            if (!byHash.TryAdd(target.DefinitionHash, validated))
                throw new ArgumentException(
                    $"Duplicate target definition hash {target.DefinitionHash}.");
        }
        foreach (var group in targets.GroupBy(item => item.DefinitionKey, StringComparer.Ordinal))
        {
            var first = group.First();
            if (group.Any(item => item.Name != first.Name ||
                item.PresentationPreset != first.PresentationPreset))
                throw new ArgumentException(
                    $"Logical definition metadata differs within {group.Key}.");
            var revisions = group.Select(item => item.Revision).Order().ToArray();
            if (!revisions.SequenceEqual(Enumerable.Range(1, revisions.Length)))
                throw new ArgumentException(
                    $"Target revisions for {group.Key} must be contiguous from one.");
        }
        return byHash;
    }

    private static void ValidateTargetMetadata(FormulaMigrationTargetPackage target,
        Guid? actorId)
    {
        if (!StableKeyPattern().IsMatch(target.DefinitionKey))
            throw new ArgumentException("Formula definition key has an invalid format.");
        if (string.IsNullOrWhiteSpace(target.Name) || target.Name.Length > 250)
            throw new ArgumentException("Formula definition name must contain 1 to 250 characters.");
        if (target.PresentationPreset?.Length > 100 || target.Revision < 1)
            throw new ArgumentException("Formula target preset or revision is invalid.");
        if (!FormulaMigrationHashing.IsSha256(target.DefinitionHash))
            throw new ArgumentException("Target definition hash must be a lowercase SHA-256.");
        if (string.IsNullOrWhiteSpace(target.FormulaLanguageVersion) ||
            target.FormulaLanguageVersion.Length > 64 ||
            string.IsNullOrWhiteSpace(target.NumericPolicyVersion) ||
            target.NumericPolicyVersion.Length > 64)
            throw new ArgumentException("Formula language and numeric policy versions are required.");
        if (target.ReviewedById == Guid.Empty || target.ApprovedById == Guid.Empty ||
            target.ReviewedById == target.ApprovedById ||
            (actorId.HasValue && (target.ReviewedById == actorId ||
                target.ApprovedById == actorId)))
            throw new InvalidOperationException(
                "Importer, reviewer, and approver must be three different users.");
        if (string.IsNullOrWhiteSpace(target.ReviewReason) ||
            target.ReviewReason.Length > 4000 ||
            string.IsNullOrWhiteSpace(target.ApprovalReason) ||
            target.ApprovalReason.Length > 4000)
            throw new ArgumentException("Review and approval reasons must contain 1 to 4000 characters.");
    }

    private static void ValidateEmbeddedVersions(FormulaMigrationTargetPackage target,
        string canonicalDefinition)
    {
        using var definition = JsonDocument.Parse(canonicalDefinition);
        if (definition.RootElement.ValueKind != JsonValueKind.Object ||
            !definition.RootElement.TryGetProperty("formulaLanguageVersion", out var language) ||
            !definition.RootElement.TryGetProperty("numericPolicyVersion", out var policy) ||
            language.GetString() != target.FormulaLanguageVersion ||
            policy.GetString() != target.NumericPolicyVersion)
            throw new InvalidOperationException(
                "Target language/policy metadata does not match its canonical definition.");
    }

    private static IReadOnlyDictionary<Guid, FormulaMigrationDecisionEvidence> ValidateEvidence(
        IReadOnlyList<FormulaMigrationDecisionEvidence> evidence)
    {
        var byArtifact = new Dictionary<Guid, FormulaMigrationDecisionEvidence>();
        foreach (var item in evidence)
        {
            if (item.LegacyArtifactId == Guid.Empty || !byArtifact.TryAdd(item.LegacyArtifactId, item))
                throw new ArgumentException("Decision evidence contains an empty or duplicate artifact ID.");
            if (!Enum.IsDefined(item.ApprovalScope))
                throw new ArgumentException("Decision evidence approval scope is invalid.");
            if (string.IsNullOrWhiteSpace(item.ApprovalReference) ||
                item.ApprovalReference.Length > 250 || item.ReviewedById == Guid.Empty)
                throw new ArgumentException("Decision approval and reviewer evidence are required.");
            ValidateMappings(item.KeyMappings);
        }
        return byArtifact;
    }

    private static void ValidateMappings(IReadOnlyList<FormulaMigrationKeyMappingPackage> mappings)
    {
        var keys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var mapping in mappings ?? [])
        {
            if (string.IsNullOrWhiteSpace(mapping.KeyKind) || mapping.KeyKind.Length > 50 ||
                string.IsNullOrWhiteSpace(mapping.LegacyPath) || mapping.LegacyPath.Length > 500 ||
                string.IsNullOrWhiteSpace(mapping.LegacyKey) || mapping.LegacyKey.Length > 120 ||
                string.IsNullOrWhiteSpace(mapping.CanonicalKey) || mapping.CanonicalKey.Length > 120 ||
                string.IsNullOrWhiteSpace(mapping.NormalizationReason) ||
                mapping.NormalizationReason.Length > 2000)
                throw new ArgumentException("Legacy key mapping metadata is incomplete or oversized.");
            if (!keys.Add($"{mapping.KeyKind}\0{mapping.LegacyPath}\0{mapping.LegacyKey}"))
                throw new ArgumentException("Duplicate legacy key mapping in decision evidence.");
        }
    }

    private static void ValidateCoverage(IReadOnlyList<FormulaMigrationItem> items,
        IReadOnlyDictionary<string, ValidatedFormulaTarget> targets,
        IReadOnlyDictionary<Guid, FormulaMigrationDecisionEvidence> evidence)
    {
        var executable = items.Where(item =>
            item.MigrationClass != FormulaMigrationClass.Unrecoverable).ToList();
        var expectedArtifacts = executable.Select(item => item.LegacyFormulaArtifactId).ToHashSet();
        if (!expectedArtifacts.SetEquals(evidence.Keys))
            throw new InvalidOperationException(
                "Apply decision evidence does not exactly cover executable dry-run items.");
        var expectedHashes = executable.Select(item => item.AfterHash!).ToHashSet(StringComparer.Ordinal);
        if (!expectedHashes.SetEquals(targets.Keys))
            throw new InvalidOperationException(
                "Apply targets do not exactly cover dry-run target hashes.");
        foreach (var item in executable)
        {
            var approval = evidence[item.LegacyFormulaArtifactId];
            if (item.ApprovalReference != approval.ApprovalReference ||
                item.ApprovalScope != approval.ApprovalScope)
                throw new InvalidOperationException(
                    "Apply approval evidence differs from the recorded dry run.");
            if (item.MigrationClass == FormulaMigrationClass.Corrective &&
                approval.ApprovalScope != FormulaMigrationApprovalScope.Individual)
                throw new InvalidOperationException(
                    "Corrective migration requires individual approval.");
            if (item.TargetId != targets[item.AfterHash!].RevisionId)
                throw new InvalidOperationException("Recorded target revision ID is inconsistent.");
        }
    }
}
