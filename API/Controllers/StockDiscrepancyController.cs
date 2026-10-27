using APP.Extensions;
using APP.IRepository;
using APP.Services.Materials;
using APP.Utils;
using DOMAIN.Entities.StockAdjustments;
using INFRASTRUCTURE.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers;

[Route("api/v{version:apiVersion}/stock-discrepancy")]
[ApiController]
[Authorize]
public class StockDiscrepancyController(ApplicationDbContext context,
    IApprovalRepository approvals) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Guid))]
    [Authorize(PermissionKeys.CanRecordStockDiscrepancy)]
    public async Task<IResult> Create([FromBody] CreateStockDiscrepancy request)
    {
        if (!Guid.TryParse(HttpContext.Items["Sub"] as string, out var actorId))
            return TypedResults.Unauthorized();
        var result = await StockDiscrepancyService.Create(context, approvals, request, actorId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<StockDiscrepancyDto>))]
    [Authorize(PermissionKeys.CanViewStockDiscrepancy)]
    public async Task<IResult> List()
    {
        var items = await StockDiscrepancyService.Query(context)
            .OrderByDescending(item => item.CreatedAt).ToListAsync();
        return TypedResults.Ok(items);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(StockDiscrepancyDto))]
    [Authorize(PermissionKeys.CanViewStockDiscrepancy)]
    public async Task<IResult> Detail(Guid id)
    {
        var item = await StockDiscrepancyService.Query(context)
            .SingleOrDefaultAsync(item => item.Id == id);
        return item is null ? TypedResults.NotFound() : TypedResults.Ok(item);
    }

    [HttpPost("{id:guid}/reconcile")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [Authorize(PermissionKeys.CanReconcileStockDiscrepancy)]
    public async Task<IResult> Reconcile(Guid id)
    {
        if (!Guid.TryParse(HttpContext.Items["Sub"] as string, out var actorId))
            return TypedResults.Unauthorized();
        var result = await StockDiscrepancyService.Reconcile(context, id, actorId);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
}
