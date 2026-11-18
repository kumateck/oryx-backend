using APP.Extensions;
using APP.Repository;
using APP.Utils;
using DOMAIN.Entities.Products.Production;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

/// <summary>Batches whose production has started but whose BMR is not issued: the warehouse prepares their reserved materials.</summary>
[ApiController, Route("api/v{version:apiVersion}/upcoming-batch-issues"), Tags("ProductionSchedule")]
[Authorize(PermissionKeys.CanViewUpcomingBatchIssues)]
public class UpcomingBatchIssueController(UpcomingBatchIssueRepository repository, APP.IRepository.IRequisitionRepository requisitions) : ControllerBase
{
    [HttpGet(Name = "ListUpcomingBatchIssues")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UpcomingBatchIssuePage))]
    public async Task<IResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string searchQuery = null)
    {
        if (!Guid.TryParse(HttpContext.Items["Sub"] as string, out var actor)) return TypedResults.Unauthorized();
        var result = await repository.List(actor, page, pageSize, searchQuery);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Batches in other stores that expire before the lots reserved for this started batch, so the warehouse can ask
    /// for swaps while it prepares, before the stock is requested.
    /// </summary>
    [HttpGet("{productionScheduleProductId:guid}/alternative-batches", Name = "ListUpcomingBatchAlternatives")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<DOMAIN.Entities.Requisitions.MaterialAlternativeBatchesDto>))]
    public async Task<IResult> Alternatives(Guid productionScheduleProductId)
    {
        if (!Guid.TryParse(HttpContext.Items["Sub"] as string, out var actor)) return TypedResults.Unauthorized();
        var result = await requisitions.GetAlternativeBatchesForProductionBatch(productionScheduleProductId, actor);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
}
