using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using DOMAIN.Entities.Materials;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Tags("Material")]
[Route("api/v{version:apiVersion}/material")]
public sealed class MaterialDensityController(IMaterialRepository repository) : ControllerBase
{
    [HttpDelete("raw/{materialId:guid}/density", Name = "ClearRawMaterialDensity")]
    [Authorize(PermissionKeys.CanEditRawMaterialDensity)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IResult> ClearRaw(Guid materialId) => Clear(materialId, MaterialKind.Raw);

    [HttpDelete("packaging/{materialId:guid}/density", Name = "ClearPackagingMaterialDensity")]
    [Authorize(PermissionKeys.CanEditPackagingMaterialDensity)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IResult> ClearPackaging(Guid materialId) => Clear(materialId, MaterialKind.Package);

    private async Task<IResult> Clear(Guid materialId, MaterialKind kind)
    {
        if (!Guid.TryParse(HttpContext.Items["Sub"]?.ToString(), out var actor)) return TypedResults.Unauthorized();
        var result = await repository.ClearDensity(materialId, kind, actor);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
}
