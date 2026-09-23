using APP.Extensions;
using APP.Services.FullProcedures;
using APP.Utils;
using DOMAIN.Entities.FullProcedures;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/template-questions")]
[Authorize]
public sealed class TemplateQuestionController(ITemplateQuestionService service) : ControllerBase
{
    [HttpGet("area/{areaId:guid}")]
    [Authorize(FullProcedurePermissionKeys.CanViewQuestionTemplates)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IReadOnlyList<TemplateQuestionDto>))]
    public async Task<IResult> List(Guid areaId, CancellationToken cancellationToken)
    {
        if (!TryActor(out _, out var roleIds)) return TypedResults.Unauthorized();
        var result = await service.ListAsync(areaId, roleIds, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpGet("{questionId:guid}")]
    [Authorize(FullProcedurePermissionKeys.CanViewQuestionTemplates)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(TemplateQuestionDetailDto))]
    public async Task<IResult> Get(Guid questionId, CancellationToken cancellationToken)
    {
        if (!TryActor(out _, out var roleIds)) return TypedResults.Unauthorized();
        var result = await service.GetAsync(questionId, roleIds, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPost]
    [Authorize(FullProcedurePermissionKeys.CanManageQuestionRevision)]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(TemplateQuestionRevisionDto))]
    public async Task<IResult> Create(
        [FromBody] CreateTemplateQuestionRequest request, CancellationToken cancellationToken)
    {
        if (!TryActor(out var actorId, out var roleIds)) return TypedResults.Unauthorized();
        var result = await service.CreateAsync(
            request, actorId, roleIds, Guid.NewGuid(), cancellationToken);
        return result.IsSuccess
            ? TypedResults.Created($"/api/v1/template-questions/{result.Value.TemplateQuestionId}",
                result.Value)
            : result.ToProblemDetails();
    }

    [HttpPost("{questionId:guid}/revisions")]
    [Authorize(FullProcedurePermissionKeys.CanManageQuestionRevision)]
    public async Task<IResult> CreateRevision(
        Guid questionId, [FromBody] CreateTemplateQuestionRevisionRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryActor(out var actorId, out var roleIds)) return TypedResults.Unauthorized();
        var result = await service.CreateRevisionAsync(
            questionId, request, actorId, roleIds, Guid.NewGuid(), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPut("revisions/{revisionId:guid}")]
    [Authorize(FullProcedurePermissionKeys.CanManageQuestionRevision)]
    public async Task<IResult> UpdateDraft(
        Guid revisionId, [FromBody] UpdateTemplateQuestionRevisionRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryActor(out var actorId, out var roleIds)) return TypedResults.Unauthorized();
        var result = await service.UpdateDraftAsync(
            revisionId, request, actorId, roleIds, Guid.NewGuid(), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPost("revisions/{revisionId:guid}/submit-review")]
    [Authorize(FullProcedurePermissionKeys.CanManageQuestionRevision)]
    public Task<IResult> SubmitReview(
        Guid revisionId, [FromBody] TemplateQuestionTransitionRequest request,
        CancellationToken cancellationToken) => Transition(
            (actor, roles, correlation) => service.SubmitForReviewAsync(
                revisionId, request, actor, roles, correlation, cancellationToken));

    [HttpPost("revisions/{revisionId:guid}/record-review")]
    [Authorize(FullProcedurePermissionKeys.CanReviewTemplateRevision)]
    public Task<IResult> RecordReview(
        Guid revisionId, [FromBody] TemplateQuestionTransitionRequest request,
        CancellationToken cancellationToken) => Transition(
            (actor, roles, correlation) => service.RecordReviewAsync(
                revisionId, request, actor, roles, correlation, cancellationToken));

    [HttpPost("revisions/{revisionId:guid}/publish")]
    [Authorize(FullProcedurePermissionKeys.CanPublishTemplateRevision)]
    public Task<IResult> Publish(
        Guid revisionId, [FromBody] TemplateQuestionTransitionRequest request,
        CancellationToken cancellationToken) => Transition(
            (actor, roles, correlation) => service.PublishAsync(
                revisionId, request, actor, roles, correlation, cancellationToken));

    [HttpPost("revisions/{revisionId:guid}/return-draft")]
    [Authorize(FullProcedurePermissionKeys.CanReviewTemplateRevision)]
    public Task<IResult> ReturnToDraft(
        Guid revisionId, [FromBody] TemplateQuestionTransitionRequest request,
        CancellationToken cancellationToken) => Transition(
            (actor, roles, correlation) => service.ReturnToDraftAsync(
                revisionId, request, actor, roles, correlation, cancellationToken));

    [HttpPost("revisions/{revisionId:guid}/retire")]
    [Authorize(FullProcedurePermissionKeys.CanPublishTemplateRevision)]
    public Task<IResult> Retire(
        Guid revisionId, [FromBody] TemplateQuestionTransitionRequest request,
        CancellationToken cancellationToken) => Transition(
            (actor, roles, correlation) => service.RetireAsync(
                revisionId, request, actor, roles, correlation, cancellationToken));

    private async Task<IResult> Transition(
        Func<Guid, IReadOnlyCollection<Guid>, Guid,
            Task<SHARED.Result<TemplateQuestionRevisionDto>>> command)
    {
        if (!TryActor(out var actorId, out var roleIds)) return TypedResults.Unauthorized();
        var result = await command(actorId, roleIds, Guid.NewGuid());
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private bool TryActor(out Guid actorId, out IReadOnlyCollection<Guid> roleIds)
    {
        roleIds = HttpContext.Items["Roles"] as List<Guid> ?? [];
        return Guid.TryParse(HttpContext.Items["Sub"] as string, out actorId);
    }
}
