using APP.Extensions;
using APP.Services.FullProcedures;
using APP.Utils;
using DOMAIN.Entities.FullProcedures;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/template-sharing")]
[Authorize]
public sealed class TemplateSharingController(ITemplateSharingService service) : ControllerBase
{
    [HttpGet("area/{areaId:guid}")]
    [Authorize(FullProcedurePermissionKeys.CanViewQuestionTemplates)]
    public async Task<IResult> List(Guid areaId, CancellationToken token)
    {
        if (!TryActor(out _, out var roles)) return TypedResults.Unauthorized();
        var result = await service.ListAsync(areaId, roles, token);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpGet("usage/{kind}/{definitionId:guid}/{revisionId:guid}")]
    [Authorize(FullProcedurePermissionKeys.CanViewQuestionTemplates)]
    public async Task<IResult> Usage(TemplateRevisionKind kind, Guid definitionId,
        Guid revisionId, CancellationToken token)
    {
        if (!TryActor(out _, out var roles)) return TypedResults.Unauthorized();
        var result = await service.GetUsageAsync(kind, definitionId, revisionId, roles, token);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPost("requests")]
    [Authorize(FullProcedurePermissionKeys.CanShareTemplateRevision)]
    public async Task<IResult> RequestGrant([FromBody] RequestTemplateSharingGrantRequest request,
        CancellationToken token)
    {
        if (!TryActor(out var actor, out var roles)) return TypedResults.Unauthorized();
        var result = await service.RequestAsync(request, actor, roles, Guid.NewGuid(), token);
        return result.IsSuccess
            ? TypedResults.Created($"/api/v1/template-sharing/{result.Value.Id}", result.Value)
            : result.ToProblemDetails();
    }

    [HttpPost("{grantId:guid}/approve")]
    [Authorize(FullProcedurePermissionKeys.CanShareTemplateRevision)]
    public Task<IResult> Approve(Guid grantId,
        [FromBody] DecideTemplateSharingGrantRequest request, CancellationToken token) =>
        Decide((actor, roles, correlation) => service.ApproveAsync(
            grantId, request, actor, roles, correlation, token));

    [HttpPost("{grantId:guid}/reject")]
    [Authorize(FullProcedurePermissionKeys.CanShareTemplateRevision)]
    public Task<IResult> Reject(Guid grantId,
        [FromBody] DecideTemplateSharingGrantRequest request, CancellationToken token) =>
        Decide((actor, roles, correlation) => service.RejectAsync(
            grantId, request, actor, roles, correlation, token));

    [HttpPost("{grantId:guid}/revoke")]
    [Authorize(FullProcedurePermissionKeys.CanShareTemplateRevision)]
    public Task<IResult> Revoke(Guid grantId,
        [FromBody] DecideTemplateSharingGrantRequest request, CancellationToken token) =>
        Decide((actor, roles, correlation) => service.RevokeAsync(
            grantId, request, actor, roles, correlation, token));

    private async Task<IResult> Decide(Func<Guid, IReadOnlyCollection<Guid>, Guid,
        Task<SHARED.Result<TemplateSharingGrantDto>>> command)
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
