using APP.Extensions;
using APP.Services.Production;
using APP.Utils;
using DOMAIN.Entities.Products.Production;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace API.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/production-schedule/{batchId:guid}/issued-material-repair")]
[Authorize(PermissionKeys.CanViewPlannedProducts)]
public sealed class IssuedMaterialRepairController(IssuedMaterialRepairService service) : ControllerBase
{
    [HttpGet("{kind}", Name = "PreviewIssuedMaterialRepair")]
    [ProducesResponseType(typeof(IssuedMaterialRepairPreviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IResult> Preview(Guid batchId, IssuedMaterialRepairKind kind)
    {
        var result = await service.Preview(batchId, kind);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPost("{kind}", Name = "ApplyIssuedMaterialRepair")]
    [ProducesResponseType(typeof(IssuedMaterialRepairResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IResult> Apply(Guid batchId, IssuedMaterialRepairKind kind, [FromBody] ApplyIssuedMaterialRepairRequest request)
    {
        try
        {
            var result = await service.Apply(batchId, kind, request);
            return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
        }
        catch (DbUpdateConcurrencyException) { return Changed(); }
        catch (DbUpdateException error) when (error.InnerException is PostgresException { SqlState: "40001" or "40P01" or "55P03" or "23505" }) { return Changed(); }
        catch (PostgresException error) when (error.SqlState is "40001" or "40P01" or "55P03" or "23505") { return Changed(); }
    }

    private static IResult Changed() => SHARED.Result.Failure(SHARED.Error.Conflict("Repair.ConcurrentChange",
        "The stock or repair changed during this action. Reload the preview before trying again.")).ToProblemDetails();
}
