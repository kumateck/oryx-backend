using APP.Extensions;
using APP.Services.FullProcedures;
using APP.Utils;
using DOMAIN.Entities.FullProcedures;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/template-areas")]
[Authorize]
public sealed class TemplateAreaController(ITemplateAreaService service) : ControllerBase
{
    [HttpGet("catalog")]
    [Authorize(FullProcedurePermissionKeys.CanViewQuestionTemplates)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(TemplateAreaCatalogDto))]
    public async Task<IResult> GetCatalog(CancellationToken cancellationToken)
    {
        var result = await service.GetCatalogAsync(cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpGet]
    [Authorize(FullProcedurePermissionKeys.CanViewQuestionTemplates)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IReadOnlyList<TemplateAreaDto>))]
    public async Task<IResult> List(
        [FromQuery] bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        if (!TryActor(out _, out var roleIds)) return TypedResults.Unauthorized();
        var result = await service.ListAsync(roleIds, includeInactive, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpGet("{id:guid}")]
    [Authorize(FullProcedurePermissionKeys.CanViewQuestionTemplates)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(TemplateAreaDto))]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> Get(Guid id, CancellationToken cancellationToken)
    {
        if (!TryActor(out _, out var roleIds)) return TypedResults.Unauthorized();
        var result = await service.GetAsync(id, roleIds, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPost]
    [Authorize(FullProcedurePermissionKeys.CanManageTemplateAreas)]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(TemplateAreaDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IResult> Create(
        [FromBody] TemplateAreaDraftRequest request, CancellationToken cancellationToken)
    {
        if (!TryActor(out var actorId, out var roleIds)) return TypedResults.Unauthorized();
        var result = await service.CreateAsync(request, actorId, roleIds, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Created($"/api/v1/template-areas/{result.Value.Id}", result.Value)
            : result.ToProblemDetails();
    }

    [HttpPut("{id:guid}")]
    [Authorize(FullProcedurePermissionKeys.CanManageTemplateAreas)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(TemplateAreaDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IResult> Update(
        Guid id, [FromBody] UpdateTemplateAreaRequest request, CancellationToken cancellationToken)
    {
        if (!TryActor(out var actorId, out var roleIds)) return TypedResults.Unauthorized();
        var result = await service.UpdateAsync(id, request, actorId, roleIds, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPut("{id:guid}/active")]
    [Authorize(FullProcedurePermissionKeys.CanManageTemplateAreas)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(TemplateAreaDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IResult> SetActive(
        Guid id, [FromBody] ChangeTemplateAreaActiveRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryActor(out var actorId, out var roleIds)) return TypedResults.Unauthorized();
        var result = await service.SetActiveAsync(id, request, actorId, roleIds, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private bool TryActor(out Guid actorId, out IReadOnlyCollection<Guid> roleIds)
    {
        roleIds = HttpContext.Items["Roles"] as List<Guid> ?? [];
        return Guid.TryParse(HttpContext.Items["Sub"] as string, out actorId);
    }
}
