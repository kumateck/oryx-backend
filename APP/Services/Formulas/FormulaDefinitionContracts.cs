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
    JsonElement TestCases);

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

public sealed record FormulaDefinitionValidationDto(
    FormulaRevisionDto Revision,
    FormulaServiceResponse Validation);

public interface IFormulaDefinitionService
{
    Task<Result<IReadOnlyList<FormulaRevisionDto>>> GetByQuestionAsync(
        Guid questionId,
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
    public static readonly Error NotFound = Error.NotFound(
        "FormulaDefinition.NotFound", "The formula revision was not found.");
    public static readonly Error InvalidDraft = Error.Validation(
        "FormulaDefinition.InvalidDraft", "The formula draft is invalid.");
    public static readonly Error Conflict = Error.Conflict(
        "FormulaDefinition.Conflict", "The formula revision changed or is not editable.");
    public static readonly Error ValidationFailed = Error.Validation(
        "FormulaDefinition.ValidationFailed", "The formula definition did not pass validation.");
    public static readonly Error SegregationOfDuties = Error.Conflict(
        "FormulaDefinition.SegregationOfDuties",
        "Formula authoring, review, and approval must be performed by different users.");
}
