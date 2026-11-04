using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using DOMAIN.Entities.Products.Production;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace API.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/production-schedule/{scheduleId:guid}/product/{productId:guid}/repair/{kind}")]
[Authorize(PermissionKeys.CanViewProductSchedules)]
[Authorize(PermissionKeys.CanViewPlannedProducts)]
public sealed class ProductionScheduleBulkRepairController(IProductionScheduleRepository repository) : ControllerBase
{
    [HttpGet(Name = "PreviewScheduleProductRepair")]
    [ProducesResponseType(typeof(ScheduleRepairPreview), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IResult> Preview(Guid scheduleId, Guid productId, ScheduleRepairKind kind)
    {
        if (!Guid.TryParse(HttpContext.Items["Sub"]?.ToString(), out var actor)) return TypedResults.Unauthorized();
        var result = await repository.PreviewScheduleRepair(scheduleId, productId, actor, kind);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPost(Name = "ApplyScheduleProductRepair")]
    [ProducesResponseType(typeof(ScheduleRepairResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IResult> Apply(Guid scheduleId, Guid productId, ScheduleRepairKind kind, [FromBody] ApplyScheduleRepair request)
    {
        if (!Guid.TryParse(HttpContext.Items["Sub"]?.ToString(), out var actor)) return TypedResults.Unauthorized();
        try
        {
            var result = await repository.ApplyScheduleRepair(scheduleId, productId, actor, kind, request);
            return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
        }
        catch (DbUpdateException) { return Busy(); }
        catch (PostgresException) { return Busy(); }
    }

    private static IResult Busy() => SHARED.Result.Failure(SHARED.Error.Conflict("Repair.Busy",
        "Production data changed or is busy. Reload the preview before trying again.")).ToProblemDetails();
}
