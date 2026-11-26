using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Materials.Batch;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/material-stock-trace")]
public sealed class MaterialStockTraceController(IMaterialRepository repository) : ControllerBase
{
    [HttpGet("raw/{materialId:guid}", Name = "GetRawMaterialStockTrace")]
    [Authorize(PermissionKeys.CanViewRawMaterialStockTrace)]
    [ProducesResponseType(typeof(MaterialStockTraceDto), StatusCodes.Status200OK)]
    public Task<IResult> Raw(Guid materialId, [FromQuery] Guid departmentId, [FromQuery] DateTime? since = null) =>
        Read(materialId, MaterialKind.Raw, departmentId, since);

    [HttpGet("packaging/{materialId:guid}", Name = "GetPackagingMaterialStockTrace")]
    [Authorize(PermissionKeys.CanViewPackagingMaterialStockTrace)]
    [ProducesResponseType(typeof(MaterialStockTraceDto), StatusCodes.Status200OK)]
    public Task<IResult> Packaging(Guid materialId, [FromQuery] Guid departmentId, [FromQuery] DateTime? since = null) =>
        Read(materialId, MaterialKind.Package, departmentId, since);

    [HttpGet("raw/{materialId:guid}/print", Name = "GetRawMaterialStockTracePrint")]
    [Authorize(PermissionKeys.CanPrintRawMaterialStockTrace)]
    [ProducesResponseType(typeof(MaterialStockTraceDto), StatusCodes.Status200OK)]
    public Task<IResult> PrintRaw(Guid materialId, [FromQuery] Guid departmentId, [FromQuery] DateTime? since = null) =>
        Read(materialId, MaterialKind.Raw, departmentId, since, forPrint: true);

    [HttpGet("packaging/{materialId:guid}/print", Name = "GetPackagingMaterialStockTracePrint")]
    [Authorize(PermissionKeys.CanPrintPackagingMaterialStockTrace)]
    [ProducesResponseType(typeof(MaterialStockTraceDto), StatusCodes.Status200OK)]
    public Task<IResult> PrintPackaging(Guid materialId, [FromQuery] Guid departmentId, [FromQuery] DateTime? since = null) =>
        Read(materialId, MaterialKind.Package, departmentId, since, forPrint: true);

    private async Task<IResult> Read(Guid materialId, MaterialKind kind, Guid departmentId, DateTime? since, bool forPrint = false)
    {
        if (!Guid.TryParse(HttpContext.Items["Sub"] as string, out var actor)) return TypedResults.Unauthorized();
        var result = await repository.GetMaterialStockTrace(materialId, kind, departmentId, since, actor, forPrint);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
}
