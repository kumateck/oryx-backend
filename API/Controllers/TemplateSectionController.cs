using APP.Extensions;
using APP.Services.FullProcedures;
using APP.Utils;
using DOMAIN.Entities.FullProcedures;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/template-sections")]
[Authorize]
public sealed class TemplateSectionController(ITemplateSectionService service) : ControllerBase
{
    [HttpGet("area/{areaId:guid}")]
    [Authorize(FullProcedurePermissionKeys.CanViewQuestionTemplates)]
    public async Task<IResult> List(Guid areaId, CancellationToken cancellationToken)
    {
        if (!TryActor(out _, out var roles)) return TypedResults.Unauthorized();
        var result = await service.ListAsync(areaId, roles, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpGet("{sectionId:guid}")]
    [Authorize(FullProcedurePermissionKeys.CanViewQuestionTemplates)]
    public async Task<IResult> Get(Guid sectionId, CancellationToken cancellationToken)
    {
        if (!TryActor(out _, out var roles)) return TypedResults.Unauthorized();
        var result = await service.GetAsync(sectionId, roles, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPost]
    [Authorize(FullProcedurePermissionKeys.CanManageFormTemplateRevision)]
    public async Task<IResult> Create(
        [FromBody] CreateTemplateSectionRequest request, CancellationToken cancellationToken)
    {
        if (!TryActor(out var actor, out var roles)) return TypedResults.Unauthorized();
        var result = await service.CreateAsync(
            request, actor, roles, Guid.NewGuid(), cancellationToken);
        return result.IsSuccess
            ? TypedResults.Created($"/api/v1/template-sections/{result.Value.TemplateSectionId}",
                result.Value)
            : result.ToProblemDetails();
    }

    [HttpPost("{sectionId:guid}/revisions")]
    [Authorize(FullProcedurePermissionKeys.CanManageFormTemplateRevision)]
    public async Task<IResult> CreateRevision(
        Guid sectionId, [FromBody] CreateTemplateSectionRevisionRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryActor(out var actor, out var roles)) return TypedResults.Unauthorized();
        var result = await service.CreateRevisionAsync(
            sectionId, request, actor, roles, Guid.NewGuid(), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPut("revisions/{revisionId:guid}")]
    [Authorize(FullProcedurePermissionKeys.CanManageFormTemplateRevision)]
    public async Task<IResult> UpdateDraft(
        Guid revisionId, [FromBody] UpdateTemplateSectionRevisionRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryActor(out var actor, out var roles)) return TypedResults.Unauthorized();
        var result = await service.UpdateDraftAsync(
            revisionId, request, actor, roles, Guid.NewGuid(), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPost("revisions/{revisionId:guid}/submit-review")]
    [Authorize(FullProcedurePermissionKeys.CanManageFormTemplateRevision)]
    public Task<IResult> SubmitReview(Guid revisionId,
        [FromBody] TemplateSectionTransitionRequest request,
        CancellationToken cancellationToken) => Transition(
            (actor, roles, correlation) => service.SubmitForReviewAsync(
                revisionId, request, actor, roles, correlation, cancellationToken));

    [HttpPost("revisions/{revisionId:guid}/record-review")]
    [Authorize(FullProcedurePermissionKeys.CanReviewTemplateRevision)]
    public Task<IResult> RecordReview(Guid revisionId,
        [FromBody] TemplateSectionTransitionRequest request,
        CancellationToken cancellationToken) => Transition(
            (actor, roles, correlation) => service.RecordReviewAsync(
                revisionId, request, actor, roles, correlation, cancellationToken));

    [HttpPost("revisions/{revisionId:guid}/return-draft")]
    [Authorize(FullProcedurePermissionKeys.CanReviewTemplateRevision)]
    public Task<IResult> ReturnToDraft(Guid revisionId,
        [FromBody] TemplateSectionTransitionRequest request,
        CancellationToken cancellationToken) => Transition(
            (actor, roles, correlation) => service.ReturnToDraftAsync(
                revisionId, request, actor, roles, correlation, cancellationToken));

    [HttpPost("revisions/{revisionId:guid}/publish")]
    [Authorize(FullProcedurePermissionKeys.CanPublishTemplateRevision)]
    public Task<IResult> Publish(Guid revisionId,
        [FromBody] TemplateSectionTransitionRequest request,
        CancellationToken cancellationToken) => Transition(
            (actor, roles, correlation) => service.PublishAsync(
                revisionId, request, actor, roles, correlation, cancellationToken));

    [HttpPost("revisions/{revisionId:guid}/retire")]
    [Authorize(FullProcedurePermissionKeys.CanPublishTemplateRevision)]
    public Task<IResult> Retire(Guid revisionId,
        [FromBody] TemplateSectionTransitionRequest request,
        CancellationToken cancellationToken) => Transition(
            (actor, roles, correlation) => service.RetireAsync(
                revisionId, request, actor, roles, correlation, cancellationToken));

    private async Task<IResult> Transition(
        Func<Guid, IReadOnlyCollection<Guid>, Guid,
            Task<SHARED.Result<TemplateSectionRevisionDto>>> command)
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
