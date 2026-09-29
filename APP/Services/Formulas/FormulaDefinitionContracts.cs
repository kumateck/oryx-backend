using System.Text.Json;
using SHARED;

#nullable enable

namespace APP.Services.Formulas;

public sealed record FormulaRevisionDraftRequest(
    string DefinitionHash,
    string FormulaLanguageVersion,
    string NumericPolicyVersion,
    string PresentationPreset,
    JsonElement Definition,
    JsonElement TestCases,
    string? AuthoringPayload = null);

public sealed record FormulaRevisionTransitionRequest(
    string ExpectedDefinitionHash,
    string Reason);

public sealed record FormulaRevisionDto(
    Guid Id,
    Guid FormulaDefinitionId,
    Guid QuestionId,
    int Revision,
    string DefinitionHash,
    string FormulaLanguageVersion,
    string NumericPolicyVersion,
    string PresentationPreset,
    string? AuthoringPayload,
    string? AuthoringPayloadHash,
    int Status,
    string StatusName,
    JsonElement Definition,
    JsonElement TestCases,
    Guid? CreatedById,
    Guid? ReviewedById,
    DateTime? ReviewedAt,
    Guid? ApprovedById,
    DateTime? ApprovedAt,
    DateTime? EffectiveAt);

public sealed record FormulaRevisionQueueDto(
    Guid RevisionId,
    Guid QuestionId,
    string QuestionLabel,
    string? QuestionReference,
    int Revision,
    string DefinitionHash,
    int Status,
    string StatusName,
    Guid? CreatedById,
    Guid? ReviewedById,
    DateTime? ReviewedAt,
    DateTime CreatedAt);

public sealed record FormulaDefinitionValidationDto(
    FormulaRevisionDto Revision,
    FormulaServiceResponse Validation);

public interface IFormulaDefinitionService
{
    Task<Result<IReadOnlyList<FormulaRevisionDto>>> GetByQuestionAsync(
        Guid questionId,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<FormulaRevisionQueueDto>>> GetReviewQueueAsync(
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<FormulaRevisionQueueDto>>> GetApprovalQueueAsync(
        CancellationToken cancellationToken = default);

    Task<Result<FormulaRevisionDto>> CreateDraftAsync(
        Guid questionId,
        FormulaRevisionDraftRequest request,
        Guid actorId,
        Guid correlationId,
        CancellationToken cancellationToken = default);

    Task<Result<FormulaRevisionDto>> UpdateDraftAsync(
        Guid revisionId,
        FormulaRevisionDraftRequest request,
        Guid actorId,
        Guid correlationId,
        CancellationToken cancellationToken = default);

    Task<Result<FormulaDefinitionValidationDto>> ValidateAsync(
        Guid revisionId,
        Guid actorId,
        Guid correlationId,
        CancellationToken cancellationToken = default);

    Task<Result<FormulaRevisionDto>> SubmitForReviewAsync(
        Guid revisionId,
        FormulaRevisionTransitionRequest request,
        Guid actorId,
        Guid correlationId,
        CancellationToken cancellationToken = default);

    Task<Result<FormulaRevisionDto>> RecordReviewAsync(
        Guid revisionId,
        FormulaRevisionTransitionRequest request,
        Guid actorId,
        Guid correlationId,
        CancellationToken cancellationToken = default);

    Task<Result<FormulaRevisionDto>> ApproveAsync(
        Guid revisionId,
        FormulaRevisionTransitionRequest request,
        Guid actorId,
        Guid correlationId,
        CancellationToken cancellationToken = default);
}

public static class FormulaDefinitionErrors
{
    public static readonly Error QuestionNotFound = Error.NotFound(
        "FormulaDefinition.QuestionNotFound", "The formula question was not found.");
    public static readonly Error NotFound = Error.NotFound(
        "FormulaDefinition.NotFound", "The formula revision was not found.");
    public static readonly Error InvalidDraft = Error.Validation(
        "FormulaDefinition.InvalidDraft", "The formula draft is invalid.");
    public static readonly Error Conflict = Error.Conflict(
        "FormulaDefinition.Conflict", "The formula revision changed or is not editable.");
    public static readonly Error RevisionInReview = Error.Conflict(
        "FormulaDefinition.RevisionInReview",
        "This formula revision is in review and cannot be edited. Complete the review or create a new draft.");
    public static readonly Error RevisionNotEditable = Error.Conflict(
        "FormulaDefinition.RevisionNotEditable",
        "This formula revision is no longer editable. Reload the question and create a new draft revision.");
    public static readonly Error ValidationFailed = Error.Validation(
        "FormulaDefinition.ValidationFailed", "The formula definition did not pass validation.");
    public static readonly Error InvalidApprovalConfiguration = Error.Validation(
        "FormulaDefinition.InvalidApprovalConfiguration",
        "A configured formula workflow must have exactly two stages: review and approval.");
    public static readonly Error NotAssigned = Error.Forbidden(
        "FormulaDefinition.NotAssigned", "You are not assigned to this formula approval stage.");
    public static readonly Error SegregationOfDuties = Error.Conflict(
        "FormulaDefinition.SegregationOfDuties",
        "Formula authoring, review, and approval must be performed by different users.");
}
