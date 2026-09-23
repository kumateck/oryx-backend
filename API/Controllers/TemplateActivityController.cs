using APP.Extensions;
using APP.Services.FullProcedures;
using APP.Utils;
using DOMAIN.Entities.FullProcedures;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/template-activities")]
[Authorize]
public sealed class TemplateActivityController(ITemplateActivityService service) : ControllerBase
{
    [HttpGet("area/{areaId:guid}")]
    [Authorize(FullProcedurePermissionKeys.CanViewQuestionTemplates)]
    public async Task<IResult> List(Guid areaId, CancellationToken token)
    {
        if (!TryActor(out _, out var roles)) return TypedResults.Unauthorized();
        var result = await service.ListAsync(areaId, roles, token);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpGet("{activityId:guid}")]
    [Authorize(FullProcedurePermissionKeys.CanViewQuestionTemplates)]
    public async Task<IResult> Get(Guid activityId, CancellationToken token)
    {
        if (!TryActor(out _, out var roles)) return TypedResults.Unauthorized();
        var result = await service.GetAsync(activityId, roles, token);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPost]
    [Authorize(FullProcedurePermissionKeys.CanManageActivityTemplateRevision)]
    public async Task<IResult> Create(
        [FromBody] CreateTemplateActivityRequest request, CancellationToken token)
    {
        if (!TryActor(out var actor, out var roles)) return TypedResults.Unauthorized();
        var result = await service.CreateAsync(request, actor, roles, Guid.NewGuid(), token);
        return result.IsSuccess
            ? TypedResults.Created($"/api/v1/template-activities/{result.Value.TemplateActivityId}",
                result.Value) : result.ToProblemDetails();
    }

    [HttpPost("{activityId:guid}/revisions")]
    [Authorize(FullProcedurePermissionKeys.CanManageActivityTemplateRevision)]
    public async Task<IResult> CreateRevision(Guid activityId,
        [FromBody] CreateTemplateActivityRevisionRequest request, CancellationToken token)
    {
        if (!TryActor(out var actor, out var roles)) return TypedResults.Unauthorized();
        var result = await service.CreateRevisionAsync(
            activityId, request, actor, roles, Guid.NewGuid(), token);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPut("revisions/{revisionId:guid}")]
    [Authorize(FullProcedurePermissionKeys.CanManageActivityTemplateRevision)]
    public async Task<IResult> UpdateDraft(Guid revisionId,
        [FromBody] UpdateTemplateActivityRevisionRequest request, CancellationToken token)
    {
        if (!TryActor(out var actor, out var roles)) return TypedResults.Unauthorized();
        var result = await service.UpdateDraftAsync(
            revisionId, request, actor, roles, Guid.NewGuid(), token);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPost("revisions/{revisionId:guid}/submit-review")]
    [Authorize(FullProcedurePermissionKeys.CanManageActivityTemplateRevision)]
    public Task<IResult> SubmitReview(Guid revisionId,
        [FromBody] TemplateActivityTransitionRequest request, CancellationToken token) =>
        Transition((actor, roles, correlation) => service.SubmitForReviewAsync(
            revisionId, request, actor, roles, correlation, token));

    [HttpPost("revisions/{revisionId:guid}/record-review")]
    [Authorize(FullProcedurePermissionKeys.CanReviewTemplateRevision)]
    public Task<IResult> RecordReview(Guid revisionId,
        [FromBody] TemplateActivityTransitionRequest request, CancellationToken token) =>
        Transition((actor, roles, correlation) => service.RecordReviewAsync(
            revisionId, request, actor, roles, correlation, token));

    [HttpPost("revisions/{revisionId:guid}/return-draft")]
    [Authorize(FullProcedurePermissionKeys.CanReviewTemplateRevision)]
    public Task<IResult> ReturnToDraft(Guid revisionId,
        [FromBody] TemplateActivityTransitionRequest request, CancellationToken token) =>
        Transition((actor, roles, correlation) => service.ReturnToDraftAsync(
            revisionId, request, actor, roles, correlation, token));

    [HttpPost("revisions/{revisionId:guid}/publish")]
    [Authorize(FullProcedurePermissionKeys.CanPublishTemplateRevision)]
    public Task<IResult> Publish(Guid revisionId,
        [FromBody] TemplateActivityTransitionRequest request, CancellationToken token) =>
        Transition((actor, roles, correlation) => service.PublishAsync(
            revisionId, request, actor, roles, correlation, token));

    [HttpPost("revisions/{revisionId:guid}/retire")]
    [Authorize(FullProcedurePermissionKeys.CanPublishTemplateRevision)]
    public Task<IResult> Retire(Guid revisionId,
        [FromBody] TemplateActivityTransitionRequest request, CancellationToken token) =>
        Transition((actor, roles, correlation) => service.RetireAsync(
            revisionId, request, actor, roles, correlation, token));

    private async Task<IResult> Transition(Func<Guid, IReadOnlyCollection<Guid>, Guid,
        Task<SHARED.Result<TemplateActivityRevisionDto>>> command)
    {
        if (!TryActor(out var actor, out var roles)) return TypedResults.Unauthorized();
        var result = await command(actor, roles, Guid.NewGuid());
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private bool TryActor(out Guid actorId, out IReadOnlyCollection<Guid> roleIds)
    {
        roleIds = HttpContext.Items["Roles"] as List<Guid> ?? [];
        return Guid.TryParse(HttpContext.Items["Sub"] as string, out actorId);
    }
}
