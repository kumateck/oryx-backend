using APP.Extensions;
using APP.Services.FullProcedures;
using APP.Utils;
using DOMAIN.Entities.FullProcedures;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/template-adoptions")]
[Authorize]
public sealed class TemplateAdoptionController(ITemplateAdoptionService service) : ControllerBase
{
    [HttpGet("{adoptionId:guid}")]
    [Authorize(FullProcedurePermissionKeys.CanViewQuestionTemplates)]
    public async Task<IResult> Get(Guid adoptionId, CancellationToken token)
    {
        if (!TryActor(out _, out var roles)) return TypedResults.Unauthorized();
        var result = await service.GetAsync(adoptionId, roles, token);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPost("from-grant/{grantId:guid}")]
    [Authorize(FullProcedurePermissionKeys.CanShareTemplateRevision)]
    public async Task<IResult> Adopt(Guid grantId,
        [FromBody] AdoptTemplateRevisionRequest request, CancellationToken token)
    {
        if (!TryActor(out var actor, out var roles)) return TypedResults.Unauthorized();
        var result = await service.AdoptAsync(
            grantId, request, actor, roles, Guid.NewGuid(), token);
        return result.IsSuccess
            ? TypedResults.Created($"/api/v1/template-adoptions/{result.Value.Id}", result.Value)
            : result.ToProblemDetails();
    }

    private bool TryActor(out Guid actorId, out IReadOnlyCollection<Guid> roleIds)
    {
        roleIds = HttpContext.Items["Roles"] as List<Guid> ?? [];
        return Guid.TryParse(HttpContext.Items["Sub"] as string, out actorId);
    }
}
