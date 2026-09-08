using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using DOMAIN.Entities.RndTrialBatches;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/rnd/projects/{rndProjectId:guid}/trial-batches")]
[Authorize]
public class RndTrialBatchController(IRndTrialBatchRepository repository) : ControllerBase
{
    /// <summary>
    /// Creates a new lab/pilot/exhibit-scale trial batch against a formulation.
    /// </summary>
    [HttpPost]
    [Authorize(PermissionKeys.CanCreateRndTrialBatch)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> CreateTrialBatch(
        [FromRoute] Guid rndProjectId,
        [FromBody] CreateRndTrialBatchRequest request
    )
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId is null) return TypedResults.Unauthorized();

        var result = await repository.CreateTrialBatch(rndProjectId, request, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Updates a trial batch's status (Planned to InProgress to Completed/Aborted).
    /// </summary>
    [HttpPut("{trialBatchId:guid}/status")]
    [Authorize(PermissionKeys.CanEditRndTrialBatch)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> UpdateStatus(
        [FromRoute] Guid trialBatchId,
        [FromBody] UpdateRndTrialBatchStatusRequest request
    )
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId is null) return TypedResults.Unauthorized();

        var result = await repository.UpdateStatus(trialBatchId, request, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a trial batch by its ID.
    /// </summary>
    [HttpGet("{trialBatchId:guid}")]
    [Authorize(PermissionKeys.CanViewRndTrialBatches)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(RndTrialBatchDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetTrialBatch([FromRoute] Guid trialBatchId)
    {
        var result = await repository.GetTrialBatch(trialBatchId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves the paginated list of trial batches for an R&amp;D project.
    /// </summary>
    [HttpGet]
    [Authorize(PermissionKeys.CanViewRndTrialBatches)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<RndTrialBatchDto>>))]
    public async Task<IResult> GetTrialBatchesForProject(
        [FromRoute] Guid rndProjectId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10
    )
    {
        var result = await repository.GetTrialBatchesForProject(rndProjectId, page, pageSize);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
}
