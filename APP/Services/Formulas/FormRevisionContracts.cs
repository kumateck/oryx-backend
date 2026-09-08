using System.Text.Json;
using SHARED;

#nullable enable

namespace APP.Services.Formulas;

public sealed record FormulaPlacementDraftRequest(
    Guid FormFieldId,
    string PlacementKey,
    Guid FormulaRevisionId,
    string ConfigurationHash,
    JsonElement Bindings,
    JsonElement ResultTargets,
    JsonElement DisplayPolicy,
    string? MethodReference);

public sealed record FormRevisionDraftRequest(
    IReadOnlyList<FormulaPlacementDraftRequest> FormulaPlacements);

public sealed record FormRevisionTransitionRequest(
    string ExpectedContentHash,
    string Reason);

public sealed record FormRevisionDto(
    Guid Id,
    Guid FormId,
    int Sequence,
    int Status,
    string StatusName,
    string ContentHash,
    Guid? CreatedById,
    Guid? ReviewedById,
    DateTime? ReviewedAt,
    Guid? ApprovedById,
    DateTime? ApprovedAt,
    IReadOnlyList<FormRevisionPlacementDto> FormulaPlacements);

public sealed record FormRevisionPlacementDto(
    Guid FormFieldRevisionId,
    Guid FormFieldId,
    Guid QuestionId,
    string PlacementKey,
    Guid FormulaRevisionId,
    string DefinitionHash,
    string ConfigurationHash,
    JsonElement Bindings,
    string? MethodReference);

public interface IFormRevisionService
{
    Task<Result<IReadOnlyList<FormRevisionDto>>> GetAsync(
        Guid formId, CancellationToken cancellationToken = default);
    Task<Result<FormRevisionDto>> CreateDraftAsync(
        Guid formId, FormRevisionDraftRequest request, Guid actorId,
        Guid correlationId, CancellationToken cancellationToken = default);
    Task<Result<FormRevisionDto>> UpdateDraftAsync(
        Guid id, FormRevisionDraftRequest request, Guid actorId,
        Guid correlationId, CancellationToken cancellationToken = default);
    Task<Result<FormRevisionDto>> SubmitForReviewAsync(
        Guid id, FormRevisionTransitionRequest request, Guid actorId,
        Guid correlationId, CancellationToken cancellationToken = default);
    Task<Result<FormRevisionDto>> RecordReviewAsync(
        Guid id, FormRevisionTransitionRequest request, Guid actorId,
        Guid correlationId, CancellationToken cancellationToken = default);
    Task<Result<FormRevisionDto>> ApproveAsync(
        Guid id, FormRevisionTransitionRequest request, Guid actorId,
        Guid correlationId, CancellationToken cancellationToken = default);
}

public static class FormRevisionErrors
{
    public static readonly Error NotFound = Error.NotFound(
        "FormRevision.NotFound", "The form revision was not found.");
    public static readonly Error Invalid = Error.Validation(
        "FormRevision.Invalid", "The form formula configuration is invalid.");
    public static readonly Error Conflict = Error.Conflict(
        "FormRevision.Conflict", "The form revision changed or is not in the required state.");
    public static readonly Error SegregationOfDuties = Error.Conflict(
        "FormRevision.SegregationOfDuties",
        "Template authoring, review, and approval must use different users.");
}
