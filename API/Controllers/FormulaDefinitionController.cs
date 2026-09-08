using APP.Extensions;
using APP.Services.Formulas;
using APP.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/formula-definitions")]
[Authorize]
public sealed class FormulaDefinitionController(
    IFormulaDefinitionService definitions) : ControllerBase
{
    [HttpGet("questions/{questionId:guid}/revisions")]
    [Authorize(PermissionKeys.CanViewQuestions)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IResult> GetQuestionRevisions(
        Guid questionId,
        CancellationToken cancellationToken)
    {
        var result = await definitions.GetByQuestionAsync(questionId, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPost("questions/{questionId:guid}/revisions")]
    [Authorize(PermissionKeys.CanEditQuestions)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(FormulaRevisionDto))]
    public async Task<IResult> CreateDraft(
        Guid questionId,
        [FromBody] FormulaRevisionDraftRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryActor(out var actorId)) return TypedResults.Unauthorized();
        var result = await definitions.CreateDraftAsync(
            questionId, request, actorId, CorrelationId(), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPut("revisions/{revisionId:guid}")]
    [Authorize(PermissionKeys.CanEditQuestions)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(FormulaRevisionDto))]
    public async Task<IResult> UpdateDraft(
        Guid revisionId,
        [FromBody] FormulaRevisionDraftRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryActor(out var actorId)) return TypedResults.Unauthorized();
        var result = await definitions.UpdateDraftAsync(
            revisionId, request, actorId, CorrelationId(), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPost("revisions/{revisionId:guid}/validate")]
    [Authorize(PermissionKeys.CanEditQuestions)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(FormulaDefinitionValidationDto))]
    public async Task<IResult> Validate(
        Guid revisionId,
        CancellationToken cancellationToken)
    {
        if (!TryActor(out var actorId)) return TypedResults.Unauthorized();
        var result = await definitions.ValidateAsync(
            revisionId, actorId, CorrelationId(), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPost("revisions/{revisionId:guid}/submit-review")]
    [Authorize(PermissionKeys.CanEditQuestions)]
    public Task<IResult> SubmitReview(
        Guid revisionId,
        FormulaRevisionTransitionRequest request,
        CancellationToken cancellationToken) => Transition(
        revisionId, request, definitions.SubmitForReviewAsync, cancellationToken);

    [HttpPost("revisions/{revisionId:guid}/record-review")]
    [Authorize(PermissionKeys.CanReviewFormulaRevision)]
    public Task<IResult> RecordReview(
        Guid revisionId,
        FormulaRevisionTransitionRequest request,
        CancellationToken cancellationToken) => Transition(
        revisionId, request, definitions.RecordReviewAsync, cancellationToken);

    [HttpPost("revisions/{revisionId:guid}/approve")]
    [Authorize(PermissionKeys.CanApproveFormulaRevision)]
    public Task<IResult> Approve(
        Guid revisionId,
        FormulaRevisionTransitionRequest request,
        CancellationToken cancellationToken) => Transition(
        revisionId, request, definitions.ApproveAsync, cancellationToken);

    private async Task<IResult> Transition(
        Guid revisionId,
        FormulaRevisionTransitionRequest request,
        Func<Guid, FormulaRevisionTransitionRequest, Guid, Guid,
            CancellationToken, Task<SHARED.Result<FormulaRevisionDto>>> action,
        CancellationToken cancellationToken)
    {
        if (!TryActor(out var actorId)) return TypedResults.Unauthorized();
        var result = await action(
            revisionId, request, actorId, CorrelationId(), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private bool TryActor(out Guid actorId) =>
        Guid.TryParse(HttpContext.Items["Sub"] as string, out actorId);

    private Guid CorrelationId() =>
        Guid.TryParse(Request.Headers["X-Correlation-ID"].FirstOrDefault(), out var value)
            ? value
            : Guid.NewGuid();
}
