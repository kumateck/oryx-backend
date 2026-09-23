using APP.Extensions;
using APP.Services.FullProcedures;
using APP.Utils;
using DOMAIN.Entities.FullProcedures;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/procedures")]
[Authorize]
public sealed class ProcedureController(IProcedureService service) : ControllerBase
{
    [HttpGet("area/{areaId:guid}")]
    [Authorize(FullProcedurePermissionKeys.CanViewProcedures)]
    public async Task<IResult> List(Guid areaId, CancellationToken token)
    {
        if (!TryActor(out _, out var roles)) return TypedResults.Unauthorized();
        var result = await service.ListAsync(areaId, roles, token);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpGet("{definitionId:guid}")]
    [Authorize(FullProcedurePermissionKeys.CanViewProcedures)]
    public async Task<IResult> Get(Guid definitionId, CancellationToken token)
    {
        if (!TryActor(out _, out var roles)) return TypedResults.Unauthorized();
        var result = await service.GetAsync(definitionId, roles, token);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPost]
    [Authorize(FullProcedurePermissionKeys.CanCreateProcedure)]
    public async Task<IResult> Create([FromBody] CreateProcedureRequest request,
        CancellationToken token)
    {
        if (!TryActor(out var actor, out var roles)) return TypedResults.Unauthorized();
        var result = await service.CreateAsync(request, actor, roles, Guid.NewGuid(), token);
        return result.IsSuccess
            ? TypedResults.Created($"/api/v1/procedures/{result.Value.ProcedureDefinitionId}",
                result.Value)
            : result.ToProblemDetails();
    }

    [HttpPost("{definitionId:guid}/revisions")]
    [Authorize(FullProcedurePermissionKeys.CanCreateProcedure)]
    public async Task<IResult> CreateRevision(Guid definitionId,
        [FromBody] CreateProcedureRevisionRequest request, CancellationToken token)
    {
        if (!TryActor(out var actor, out var roles)) return TypedResults.Unauthorized();
        var result = await service.CreateRevisionAsync(definitionId, request, actor, roles,
            Guid.NewGuid(), token);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPut("revisions/{revisionId:guid}")]
    [Authorize(FullProcedurePermissionKeys.CanEditProcedureDraft)]
    public async Task<IResult> UpdateDraft(Guid revisionId,
        [FromBody] UpdateProcedureRevisionRequest request, CancellationToken token)
    {
        if (!TryActor(out var actor, out var roles)) return TypedResults.Unauthorized();
        var result = await service.UpdateDraftAsync(revisionId, request, actor, roles,
            Guid.NewGuid(), token);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPost("revisions/{revisionId:guid}/validate")]
    [Authorize(FullProcedurePermissionKeys.CanValidateProcedureDraft)]
    public async Task<IResult> Validate(Guid revisionId, CancellationToken token)
    {
        if (!TryActor(out _, out var roles)) return TypedResults.Unauthorized();
        var result = await service.ValidateAsync(revisionId, roles, token);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPost("revisions/{revisionId:guid}/submit-review")]
    [Authorize(FullProcedurePermissionKeys.CanSubmitProcedureReview)]
    public Task<IResult> SubmitReview(Guid revisionId,
        [FromBody] ProcedureTransitionRequest request, CancellationToken token) =>
        Transition((actor, roles, correlation) => service.SubmitForReviewAsync(
            revisionId, request, actor, roles, correlation, token));

    [HttpPost("revisions/{revisionId:guid}/record-review")]
    [Authorize(FullProcedurePermissionKeys.CanReviewProcedureRevision)]
    public Task<IResult> RecordReview(Guid revisionId,
        [FromBody] ProcedureTransitionRequest request, CancellationToken token) =>
        Transition((actor, roles, correlation) => service.RecordReviewAsync(
            revisionId, request, actor, roles, correlation, token));

    [HttpPost("revisions/{revisionId:guid}/return-draft")]
    [Authorize(FullProcedurePermissionKeys.CanReviewProcedureRevision)]
    public Task<IResult> ReturnDraft(Guid revisionId,
        [FromBody] ProcedureTransitionRequest request, CancellationToken token) =>
        Transition((actor, roles, correlation) => service.ReturnToDraftAsync(
            revisionId, request, actor, roles, correlation, token));

    [HttpPost("revisions/{revisionId:guid}/approve")]
    [Authorize(FullProcedurePermissionKeys.CanApproveProcedureRevision)]
    public Task<IResult> Approve(Guid revisionId,
        [FromBody] ProcedureTransitionRequest request, CancellationToken token) =>
        Transition((actor, roles, correlation) => service.ApproveAsync(
            revisionId, request, actor, roles, correlation, token));

    [HttpPost("revisions/{revisionId:guid}/retire")]
    [Authorize(FullProcedurePermissionKeys.CanApproveProcedureRevision)]
    public Task<IResult> Retire(Guid revisionId,
        [FromBody] ProcedureTransitionRequest request, CancellationToken token) =>
        Transition((actor, roles, correlation) => service.RetireAsync(
            revisionId, request, actor, roles, correlation, token));

    private async Task<IResult> Transition(Func<Guid, IReadOnlyCollection<Guid>, Guid,
        Task<SHARED.Result<ProcedureRevisionDto>>> command)
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
