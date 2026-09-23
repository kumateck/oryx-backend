using APP.Extensions;
using APP.Services.FullProcedures;
using APP.Utils;
using DOMAIN.Entities.FullProcedures;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/template-workflows")]
[Authorize]
public sealed class TemplateWorkflowController(ITemplateWorkflowService service) : ControllerBase
{
    [HttpGet("area/{areaId:guid}")]
    [Authorize(FullProcedurePermissionKeys.CanViewQuestionTemplates)]
    public async Task<IResult> List(Guid areaId, CancellationToken token)
    {
        if (!TryActor(out _, out var roles)) return TypedResults.Unauthorized();
        var result = await service.ListAsync(areaId, roles, token);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpGet("{workflowId:guid}")]
    [Authorize(FullProcedurePermissionKeys.CanViewQuestionTemplates)]
    public async Task<IResult> Get(Guid workflowId, CancellationToken token)
    {
        if (!TryActor(out _, out var roles)) return TypedResults.Unauthorized();
        var result = await service.GetAsync(workflowId, roles, token);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPost]
    [Authorize(FullProcedurePermissionKeys.CanManageWorkflowTemplateRevision)]
    public async Task<IResult> Create(
        [FromBody] CreateTemplateWorkflowRequest request, CancellationToken token)
    {
        if (!TryActor(out var actor, out var roles)) return TypedResults.Unauthorized();
        var result = await service.CreateAsync(request, actor, roles, Guid.NewGuid(), token);
        return result.IsSuccess
            ? TypedResults.Created($"/api/v1/template-workflows/{result.Value.TemplateWorkflowId}",
                result.Value) : result.ToProblemDetails();
    }

    [HttpPost("{workflowId:guid}/revisions")]
    [Authorize(FullProcedurePermissionKeys.CanManageWorkflowTemplateRevision)]
    public async Task<IResult> CreateRevision(Guid workflowId,
        [FromBody] CreateTemplateWorkflowRevisionRequest request, CancellationToken token)
    {
        if (!TryActor(out var actor, out var roles)) return TypedResults.Unauthorized();
        var result = await service.CreateRevisionAsync(
            workflowId, request, actor, roles, Guid.NewGuid(), token);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPut("revisions/{revisionId:guid}")]
    [Authorize(FullProcedurePermissionKeys.CanManageWorkflowTemplateRevision)]
    public async Task<IResult> UpdateDraft(Guid revisionId,
        [FromBody] UpdateTemplateWorkflowRevisionRequest request, CancellationToken token)
    {
        if (!TryActor(out var actor, out var roles)) return TypedResults.Unauthorized();
        var result = await service.UpdateDraftAsync(
            revisionId, request, actor, roles, Guid.NewGuid(), token);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPut("revisions/{revisionId:guid}/layout")]
    [Authorize(FullProcedurePermissionKeys.CanManageWorkflowTemplateRevision)]
    public async Task<IResult> UpdateLayout(Guid revisionId,
        [FromBody] UpdateTemplateWorkflowLayoutRequest request, CancellationToken token)
    {
        if (!TryActor(out var actor, out var roles)) return TypedResults.Unauthorized();
        var result = await service.UpdateLayoutAsync(revisionId, request, actor, roles, token);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPost("revisions/{revisionId:guid}/submit-review")]
    [Authorize(FullProcedurePermissionKeys.CanManageWorkflowTemplateRevision)]
    public Task<IResult> SubmitReview(Guid revisionId,
        [FromBody] TemplateWorkflowTransitionRequest request, CancellationToken token) =>
        Transition((actor, roles, correlation) => service.SubmitForReviewAsync(
            revisionId, request, actor, roles, correlation, token));

    [HttpPost("revisions/{revisionId:guid}/record-review")]
    [Authorize(FullProcedurePermissionKeys.CanReviewTemplateRevision)]
    public Task<IResult> RecordReview(Guid revisionId,
        [FromBody] TemplateWorkflowTransitionRequest request, CancellationToken token) =>
        Transition((actor, roles, correlation) => service.RecordReviewAsync(
            revisionId, request, actor, roles, correlation, token));

    [HttpPost("revisions/{revisionId:guid}/return-draft")]
    [Authorize(FullProcedurePermissionKeys.CanReviewTemplateRevision)]
    public Task<IResult> ReturnToDraft(Guid revisionId,
        [FromBody] TemplateWorkflowTransitionRequest request, CancellationToken token) =>
        Transition((actor, roles, correlation) => service.ReturnToDraftAsync(
            revisionId, request, actor, roles, correlation, token));

    [HttpPost("revisions/{revisionId:guid}/publish")]
    [Authorize(FullProcedurePermissionKeys.CanPublishTemplateRevision)]
    public Task<IResult> Publish(Guid revisionId,
        [FromBody] TemplateWorkflowTransitionRequest request, CancellationToken token) =>
        Transition((actor, roles, correlation) => service.PublishAsync(
            revisionId, request, actor, roles, correlation, token));

    [HttpPost("revisions/{revisionId:guid}/retire")]
    [Authorize(FullProcedurePermissionKeys.CanPublishTemplateRevision)]
    public Task<IResult> Retire(Guid revisionId,
        [FromBody] TemplateWorkflowTransitionRequest request, CancellationToken token) =>
        Transition((actor, roles, correlation) => service.RetireAsync(
            revisionId, request, actor, roles, correlation, token));

    private async Task<IResult> Transition(Func<Guid, IReadOnlyCollection<Guid>, Guid,
        Task<SHARED.Result<TemplateWorkflowRevisionDto>>> command)
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
