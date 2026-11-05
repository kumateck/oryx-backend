using System.Data;
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
[Route("api/v{version:apiVersion}/production-schedule/{batchId:guid}/reservation-repair")]
[Authorize(PermissionKeys.CanViewPlannedProducts)]
[Authorize(PermissionKeys.CanRepairStockReservations)]
public sealed class ReservationRepairController(ReservationRepairService service) : ControllerBase
{
    [HttpGet(Name = "PreviewReservationRepair")]
    [ProducesResponseType(typeof(ReservationRepairPreviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IResult> Preview(Guid batchId)
    {
        var result = await service.Preview(batchId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPost(Name = "ApplyReservationRepair")]
    [ProducesResponseType(typeof(ReservationRepairResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IResult> Apply(Guid batchId, [FromBody] ReservationRepairRequest request)
    {
        try
        {
            var result = await service.Apply(batchId, request);
            return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
        }
        catch (DBConcurrencyException) { return Changed(); }
        catch (DbUpdateConcurrencyException) { return Changed(); }
        catch (DbUpdateException error) when (error.InnerException is PostgresException { SqlState: "40001" or "40P01" or "55P03" or "23505" or "23514" }) { return Changed(); }
        catch (PostgresException error) when (error.SqlState is "40001" or "40P01" or "55P03" or "23505" or "23514" or "57014") { return Changed(); }
    }

    private static IResult Changed() => SHARED.Result.Failure(SHARED.Error.Conflict("ReservationRepair.ConcurrentChange",
        "The repair could not be completed against the reviewed stock. Reload the preview before trying again.")).ToProblemDetails();
}
