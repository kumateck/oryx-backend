using APP.Extensions;
using APP.Services.Formulas;
using APP.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/form-revisions")]
[Authorize]
public sealed class FormRevisionController(IFormRevisionService revisions) : ControllerBase
{
    [HttpGet("forms/{formId:guid}")]
    [Authorize(PermissionKeys.CanViewTemplate)]
    public async Task<IResult> Get(
        Guid formId, CancellationToken cancellationToken)
    {
        var result = await revisions.GetAsync(formId, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPost("forms/{formId:guid}")]
    [Authorize(PermissionKeys.CanEditTemplate)]
    public async Task<IResult> CreateDraft(
        Guid formId, FormRevisionDraftRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryActor(out var actorId)) return TypedResults.Unauthorized();
        var result = await revisions.CreateDraftAsync(
            formId, request, actorId, CorrelationId(), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPut("{id:guid}")]
    [Authorize(PermissionKeys.CanEditTemplate)]
    public async Task<IResult> UpdateDraft(
        Guid id, FormRevisionDraftRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryActor(out var actorId)) return TypedResults.Unauthorized();
        var result = await revisions.UpdateDraftAsync(
            id, request, actorId, CorrelationId(), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPost("{id:guid}/submit-review")]
    [Authorize(PermissionKeys.CanEditTemplate)]
    public Task<IResult> SubmitReview(
        Guid id, FormRevisionTransitionRequest request,
        CancellationToken cancellationToken) => Transition(
        id, request, revisions.SubmitForReviewAsync, cancellationToken);

    [HttpPost("{id:guid}/record-review")]
    [Authorize(PermissionKeys.CanReviewFormRevision)]
    public Task<IResult> RecordReview(
        Guid id, FormRevisionTransitionRequest request,
        CancellationToken cancellationToken) => Transition(
        id, request, revisions.RecordReviewAsync, cancellationToken);

    [HttpPost("{id:guid}/approve")]
    [Authorize(PermissionKeys.CanApproveFormRevision)]
    public Task<IResult> Approve(
        Guid id, FormRevisionTransitionRequest request,
        CancellationToken cancellationToken) => Transition(
        id, request, revisions.ApproveAsync, cancellationToken);

    private async Task<IResult> Transition(
        Guid id,
        FormRevisionTransitionRequest request,
        Func<Guid, FormRevisionTransitionRequest, Guid, Guid,
            CancellationToken, Task<SHARED.Result<FormRevisionDto>>> action,
        CancellationToken cancellationToken)
    {
        if (!TryActor(out var actorId)) return TypedResults.Unauthorized();
        var result = await action(id, request, actorId, CorrelationId(), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private bool TryActor(out Guid actorId) =>
        Guid.TryParse(HttpContext.Items["Sub"] as string, out actorId);

    private Guid CorrelationId() =>
        Guid.TryParse(Request.Headers["X-Correlation-ID"].FirstOrDefault(), out var value)
            ? value
            : Guid.NewGuid();
}
