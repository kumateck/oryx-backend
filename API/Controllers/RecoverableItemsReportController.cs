using APP.Extensions;
using APP.IRepository;
using APP.Services.Stores;
using DOMAIN.Entities.RecoverableItemsReports;
using INFRASTRUCTURE.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers;

/// <summary>
/// Returns of recoverable store items. Access follows the item's store and its return-item keys.
/// </summary>
[ApiController]
[Route("v{version:apiVersion}/recoverable-items-reports")]
[Route("api/v{version:apiVersion}/recoverable-items-reports")]
[Authorize]
public class RecoverableItemsReportController(IRecoverableItemReportRepository repository, ApplicationDbContext context) : ControllerBase
{
    /// <summary>
    /// Records a return of recoverable stock issued against a stock requisition item, and puts the stock back.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IResult> CreateRecoverableItemReport([FromBody] CreateRecoverableItemReportRequest request)
    {
        if (Actor() is not { } actor) return TypedResults.Unauthorized();
        var item = await context.Items.AsNoTracking().FirstOrDefaultAsync(i => i.Id == request.ItemId);
        if (item == null) return TypedResults.NotFound();
        if (!await StoreWorkflowAccess.Allows(context, actor, item.Store, StoreWorkflow.ReturnItems, StoreWorkflowAction.Create)) return TypedResults.Forbid();
        var result = await repository.CreateRecoverableItemReport(request, actor);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Lists the returns of the stores whose returns the user may read, newest first.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<RecoverableItemReportDto>))]
    public async Task<IResult> GetRecoverableItemReports()
    {
        var readable = await StoreWorkflowAccess.Readable(context, Actor(), StoreWorkflow.ReturnItems);
        if (readable.Count == 0) return TypedResults.Forbid();
        var result = await repository.GetRecoverableItemReport(readable);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private Guid? Actor() => Guid.TryParse(HttpContext.Items["Sub"] as string, out var id) ? id : null;
}
