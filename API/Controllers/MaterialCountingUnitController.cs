using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using DOMAIN.Entities.Materials;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

/// <summary>Changes the unit the caller's department counts a material in, with a preview that must be confirmed.</summary>
[ApiController]
[Tags("Material")]
[Route("api/v{version:apiVersion}/material")]
public sealed class MaterialCountingUnitController(IMaterialRepository repository) : ControllerBase
{
    [HttpGet("raw/{materialId:guid}/counting-unit/preview", Name = "PreviewRawMaterialCountingUnit")]
    [Authorize(PermissionKeys.CanChangeRawMaterialCountingUnit)]
    [ProducesResponseType(typeof(MaterialCountingUnitPreview), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IResult> PreviewRaw(Guid materialId, [FromQuery] Guid unitId) => Preview(materialId, MaterialKind.Raw, unitId);

    [HttpGet("packaging/{materialId:guid}/counting-unit/preview", Name = "PreviewPackagingMaterialCountingUnit")]
    [Authorize(PermissionKeys.CanChangePackagingMaterialCountingUnit)]
    [ProducesResponseType(typeof(MaterialCountingUnitPreview), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IResult> PreviewPackaging(Guid materialId, [FromQuery] Guid unitId) => Preview(materialId, MaterialKind.Package, unitId);

    [HttpPost("raw/{materialId:guid}/counting-unit", Name = "ChangeRawMaterialCountingUnit")]
    [Authorize(PermissionKeys.CanChangeRawMaterialCountingUnit)]
    [ProducesResponseType(typeof(MaterialCountingUnitResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<IResult> ChangeRaw(Guid materialId, [FromBody] ChangeMaterialCountingUnit request) => Change(materialId, MaterialKind.Raw, request);

    [HttpPost("packaging/{materialId:guid}/counting-unit", Name = "ChangePackagingMaterialCountingUnit")]
    [Authorize(PermissionKeys.CanChangePackagingMaterialCountingUnit)]
    [ProducesResponseType(typeof(MaterialCountingUnitResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<IResult> ChangePackaging(Guid materialId, [FromBody] ChangeMaterialCountingUnit request) => Change(materialId, MaterialKind.Package, request);

    private async Task<IResult> Preview(Guid materialId, MaterialKind kind, Guid unitId)
    {
        if (!Guid.TryParse(HttpContext.Items["Sub"]?.ToString(), out var actor)) return TypedResults.Unauthorized();
        var result = await repository.PreviewCountingUnitChange(materialId, kind, unitId, actor);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private async Task<IResult> Change(Guid materialId, MaterialKind kind, ChangeMaterialCountingUnit request)
    {
        if (!Guid.TryParse(HttpContext.Items["Sub"]?.ToString(), out var actor)) return TypedResults.Unauthorized();
        var result = await repository.ChangeCountingUnit(materialId, kind, request, actor);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
}
