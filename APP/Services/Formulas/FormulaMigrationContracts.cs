using DOMAIN.Entities.Formulas;

#nullable enable

namespace APP.Services.Formulas;

public sealed record FormulaMigrationDecision(
    Guid QuestionId,
    Guid? QuestionOptionId,
    string LegacyPath,
    string SourceHash,
    FormulaMigrationClass MigrationClass,
    string? TargetDefinitionHash,
    string Action,
    string? ApprovalReference,
    FormulaMigrationApprovalScope? ApprovalScope = null
);

public sealed record FormulaMigrationDryRunRequest(
    string ReleaseId,
    string CorpusChecksum,
    string CodeVersion,
    string? ExpectedSourceFingerprint,
    IReadOnlyList<FormulaMigrationDecision> Decisions
);

public sealed record FormulaMigrationDryRunItem(
    Guid LegacyArtifactId,
    Guid QuestionId,
    Guid? QuestionOptionId,
    string LegacyPath,
    string SourceHash,
    bool IsActive,
    int ActivePlacements,
    int ResponseRows,
    FormulaMigrationClass? MigrationClass,
    string? TargetDefinitionHash,
    Guid? TargetRevisionId,
    string? Action,
    string? ApprovalReference,
    FormulaMigrationApprovalScope? ApprovalScope,
    bool Ready,
    IReadOnlyList<string> Diagnostics
);

public sealed record FormulaMigrationControl(
    string Name,
    string Expected,
    string Actual,
    bool Passed
);

public sealed record FormulaMigrationDryRunReport(
    string ReleaseId,
    string SourceFingerprint,
    string CorpusChecksum,
    string CodeVersion,
    bool? FingerprintMatches,
    bool CanApply,
    IReadOnlyList<FormulaMigrationDryRunItem> Items,
    IReadOnlyList<FormulaMigrationControl> Controls
);

public interface IFormulaMigrationInventoryService
{
    Task<FormulaMigrationDryRunReport> DryRunAsync(
        FormulaMigrationDryRunRequest request,
        CancellationToken cancellationToken = default
    );
}
