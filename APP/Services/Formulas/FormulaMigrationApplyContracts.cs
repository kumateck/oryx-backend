using DOMAIN.Entities.Formulas;

#nullable enable

namespace APP.Services.Formulas;

public sealed record FormulaMigrationTargetPackage(
    string DefinitionKey,
    string Name,
    string? PresentationPreset,
    int Revision,
    string DefinitionJson,
    string TestCasesJson,
    string DefinitionHash,
    string FormulaLanguageVersion,
    string NumericPolicyVersion,
    Guid ReviewedById,
    Guid ApprovedById,
    string ReviewReason,
    string ApprovalReason
);

public sealed record FormulaMigrationKeyMappingPackage(
    string KeyKind,
    string LegacyPath,
    string LegacyKey,
    string CanonicalKey,
    string NormalizationReason
);

public sealed record FormulaMigrationDecisionEvidence(
    Guid LegacyArtifactId,
    FormulaMigrationApprovalScope ApprovalScope,
    string ApprovalReference,
    Guid ReviewedById,
    IReadOnlyList<FormulaMigrationKeyMappingPackage> KeyMappings
);

public sealed record FormulaMigrationApplyRequest(
    Guid DryRunId,
    string SignedReportLocation,
    string SignedReportHash,
    string ManifestHash,
    IReadOnlyList<FormulaMigrationTargetPackage> Targets,
    IReadOnlyList<FormulaMigrationDecisionEvidence> DecisionEvidence
);

public sealed record FormulaMigrationApplyDraft(
    Guid DryRunId,
    IReadOnlyList<FormulaMigrationTargetPackage> Targets,
    IReadOnlyList<FormulaMigrationDecisionEvidence> DecisionEvidence
);

public sealed record FormulaMigrationPackageValidationReceipt(
    string ManifestHash,
    int TargetCount,
    int EvidenceCount,
    int KeyMappingCount
);

public sealed record FormulaMigrationApplyReceipt(
    Guid RunId,
    bool AlreadyApplied,
    int DefinitionsCreated,
    int RevisionsCreated,
    int QuestionLinksCreated,
    int KeyMappingsCreated
);

public interface IFormulaMigrationApplyService
{
    Task<FormulaMigrationApplyReceipt> ApplyApprovedDefinitionsAsync(
        FormulaMigrationApplyRequest request,
        CancellationToken cancellationToken = default
    );
}
