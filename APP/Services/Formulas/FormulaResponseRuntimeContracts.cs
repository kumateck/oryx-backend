using SHARED;
using DOMAIN.Entities.Formulas;

#nullable enable

namespace APP.Services.Formulas;

public sealed record FormulaSnapshotDto(
    Guid Id,
    Guid ResponseId,
    string PlacementKey,
    int Sequence,
    Guid FormulaRevisionId,
    string DefinitionHash,
    string ConfigurationHash,
    DateTime CapturedAt);

public sealed record FormulaExecutionDto(
    Guid Id,
    Guid ResponseFormulaSnapshotId,
    string PlacementKey,
    int Status,
    string StatusName,
    int Authority,
    string AuthorityName,
    string EngineVersion,
    string EngineBuildHash,
    string InputHash,
    string? ResultHash,
    string RawResultsJson,
    string RoundedResultsJson,
    string DisplayResultsJson,
    DateTime ExecutedAt);

public sealed record FormulaEvaluationRequest(string IdempotencyKey);

public interface IFormulaResponseRuntimeService
{
    Task<Result<IReadOnlyList<FormulaSnapshotDto>>> EnsureInitialSnapshotsAsync(
        Guid responseId,
        Guid actorId,
        CancellationToken cancellationToken = default);

    Task<Result<FormulaExecutionDto>> EvaluateStoredInputsAsync(
        Guid responseId,
        string placementKey,
        string idempotencyKey,
        Guid actorId,
        FormulaExecutionTrigger trigger = FormulaExecutionTrigger.DraftEdit,
        CancellationToken cancellationToken = default);
}

public sealed record FormulaSubmissionSetDto(
    Guid Id,
    Guid ResponseId,
    int Sequence,
    string SetHash,
    DateTime SubmittedAt,
    IReadOnlyList<Guid> ExecutionIds);

public interface IFormulaSubmissionService
{
    Task<Result<FormulaSubmissionSetDto?>> FinalizeAsync(
        Guid responseId,
        Guid actorId,
        CancellationToken cancellationToken = default);
}

public static class FormulaResponseRuntimeErrors
{
    public static readonly Error NotFound = Error.NotFound(
        "FormulaRuntime.NotFound", "The formula response placement was not found.");
    public static readonly Error ConfigurationUnavailable = Error.Conflict(
        "FormulaRuntime.ConfigurationUnavailable",
        "The response does not have an approved formula configuration.");
    public static readonly Error BindingInvalid = Error.Validation(
        "FormulaRuntime.BindingInvalid",
        "The stored formula inputs do not match the approved variable bindings.");
    public static readonly Error InvalidIdempotencyKey = Error.Validation(
        "FormulaRuntime.InvalidIdempotencyKey",
        "A valid formula evaluation idempotency key is required.");
    public static readonly Error Unauthorized = Error.Validation(
        "FormulaRuntime.Unauthorized",
        "The user is not authorized to evaluate this response placement.");
    public static readonly Error SubmissionBlocked = Error.Conflict(
        "FormulaRuntime.SubmissionBlocked",
        "Every formula must have a valid authoritative execution before submission.");
}
