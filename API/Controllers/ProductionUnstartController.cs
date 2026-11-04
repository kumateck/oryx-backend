using APP.Extensions;
using APP.Services.Production;
using APP.Utils;
using DOMAIN.Entities.Products.Production;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SHARED;

namespace API.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/production-schedule/activity/{activityId:guid}/unstart")]
[Authorize(PermissionKeys.CanViewPlannedProducts)]
[Authorize(PermissionKeys.CanUnstartProduction)]
public sealed class ProductionUnstartController(ProductionUnstartService service) : ControllerBase
{
    [HttpGet(Name = "PreviewProductionUnstart")]
    [ProducesResponseType(typeof(ProductionUnstartPreviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> Preview(Guid activityId)
    {
        var result = await service.Preview(activityId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPost(Name = "ApplyProductionUnstart")]
    [ProducesResponseType(typeof(ProductionUnstartResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IResult> Apply(Guid activityId, [FromBody] ProductionUnstartRequest request)
    {
        try
        {
            var result = await service.Apply(activityId, request);
            return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
        }
        catch (DbUpdateConcurrencyException) { return Changed(); }
        catch (DbUpdateException error) when (error.InnerException is PostgresException { SqlState: "40001" or "40P01" or "55P03" or "23505" or "23514" }) { return Changed(); }
        catch (PostgresException error) when (error.SqlState is "40001" or "40P01" or "55P03" or "23505" or "23514") { return Changed(); }
    }

    private static IResult Changed() => Result.Failure(Error.Conflict("Unstart.ConcurrentChange",
        "The run or documents changed during this repair. Reload the preview before trying again.")).ToProblemDetails();
}
