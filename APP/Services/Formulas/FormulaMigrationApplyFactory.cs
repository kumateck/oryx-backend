using DOMAIN.Entities.Formulas;

namespace APP.Services.Formulas;

internal static class FormulaMigrationApplyFactory
{
    public static FormulaRevision CreateDraft(ValidatedFormulaTarget target,
        string evidenceHash) => new()
    {
        Id = target.RevisionId,
        FormulaDefinitionId = target.DefinitionId,
        Revision = target.Source.Revision,
        DefinitionJson = target.CanonicalDefinition,
        TestCasesJson = target.CanonicalTestCases,
        DefinitionHash = target.Source.DefinitionHash,
        ReleaseEvidenceHash = evidenceHash,
        FormulaLanguageVersion = target.Source.FormulaLanguageVersion,
        NumericPolicyVersion = target.Source.NumericPolicyVersion,
        Status = FormulaRevisionStatus.Draft
    };

    public static FormulaRevisionAudit CreateAudit(Guid correlationId,
        FormulaRevision revision, FormulaRevisionStatus prior, FormulaRevisionStatus current,
        Guid actorId, string reason, string action, DateTime occurredAt) => new()
    {
        Id = FormulaMigrationHashing.DeterministicId(correlationId.ToString("N"),
            "revision-audit", $"{revision.Id:N}:{(int)current}"),
        FormulaRevisionId = revision.Id,
        PriorStatus = prior,
        NewStatus = current,
        Action = action,
        Reason = reason,
        DefinitionHash = revision.DefinitionHash,
        ActorId = actorId,
        OccurredAt = occurredAt,
        CorrelationId = correlationId
    };

    public static Guid MappingId(Guid artifactId,
        FormulaMigrationKeyMappingPackage mapping) =>
        FormulaMigrationHashing.DeterministicId(artifactId.ToString("N"), "legacy-key",
            $"{mapping.KeyKind}:{mapping.LegacyPath}:{mapping.LegacyKey}");

    public static void ValidateExistingRevision(FormulaRevision revision,
        ValidatedFormulaTarget target, string evidenceHash)
    {
        if (revision.DeletedAt.HasValue || revision.Status != FormulaRevisionStatus.Approved ||
            revision.FormulaDefinitionId != target.DefinitionId ||
            revision.Revision != target.Source.Revision ||
            revision.DefinitionJson != target.CanonicalDefinition ||
            revision.TestCasesJson != target.CanonicalTestCases ||
            revision.DefinitionHash != target.Source.DefinitionHash ||
            revision.ReleaseEvidenceHash != evidenceHash ||
            revision.FormulaLanguageVersion != target.Source.FormulaLanguageVersion ||
            revision.NumericPolicyVersion != target.Source.NumericPolicyVersion)
            throw new InvalidOperationException(
                $"Existing formula revision differs for hash {target.Source.DefinitionHash}.");
    }

    public static void ValidateExistingMapping(LegacyKeyMapping actual,
        FormulaMigrationKeyMappingPackage expected,
        FormulaMigrationDecisionEvidence evidence)
    {
        if (actual.KeyKind != expected.KeyKind || actual.LegacyPath != expected.LegacyPath ||
            actual.LegacyKey != expected.LegacyKey ||
            actual.CanonicalKey != expected.CanonicalKey ||
            actual.NormalizationReason != expected.NormalizationReason ||
            actual.ReviewedById != evidence.ReviewedById ||
            actual.ApprovalReference != evidence.ApprovalReference)
            throw new InvalidOperationException("Existing legacy key mapping differs.");
    }
}
