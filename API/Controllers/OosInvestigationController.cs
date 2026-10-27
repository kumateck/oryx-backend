using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using DOMAIN.Entities.OosInvestigations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/qa/oos-investigations")]
[Authorize]
public class OosInvestigationController(IOosInvestigationRepository repository, INFRASTRUCTURE.Context.ApplicationDbContext context) : ControllerBase
{
    /// <summary>
    /// Initiates an OOS investigation upon COA rejection.
    /// </summary>
    [HttpPost("initiate")]
    [Authorize(PermissionKeys.CanInitiateOosInvestigation)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> InitiateOosInvestigation([FromBody] InitiateOosInvestigationRequest request)
    {
        if (!await Allowed(PermissionKeys.CanInitiateOosInvestigation)) return TypedResults.Forbid();
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.InitiateOosInvestigation(request, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Updates the investigation form fields while the investigation is still Initiated.
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(PermissionKeys.CanSubmitOosInvestigation)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> UpdateOosInvestigation([FromRoute] Guid id, [FromBody] UpdateOosInvestigationRequest request)
    {
        if (!await Allowed(PermissionKeys.CanSubmitOosInvestigation)) return TypedResults.Forbid();
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.UpdateOosInvestigation(id, request, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Submits a completed OOS investigation to QA for review.
    /// </summary>
    [HttpPut("{id:guid}/submit")]
    [Authorize(PermissionKeys.CanSubmitOosInvestigation)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> SubmitToQa([FromRoute] Guid id)
    {
        if (!await Allowed(PermissionKeys.CanSubmitOosInvestigation)) return TypedResults.Forbid();
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.SubmitToQa(id, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// QA review and approval or permanent rejection of an OOS investigation.
    /// </summary>
    [HttpPut("{id:guid}/qa-review")]
    [Authorize(PermissionKeys.CanReviewOosInvestigation)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> ReviewByQa([FromRoute] Guid id, [FromBody] ReviewOosInvestigationRequest request)
    {
        if (!await Allowed(PermissionKeys.CanReviewOosInvestigation)) return TypedResults.Forbid();
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.ReviewByQa(id, request, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves details of an OOS investigation by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [Authorize(PermissionKeys.CanViewOosInvestigations)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(OosInvestigationDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetOosInvestigation([FromRoute] Guid id)
    {
        if (!await Allowed(PermissionKeys.CanViewOosInvestigations)) return TypedResults.Forbid();
        var result = await repository.GetOosInvestigation(id);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a paginated list of OOS investigations.
    /// </summary>
    [HttpGet]
    [Authorize(PermissionKeys.CanViewOosInvestigations)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<OosInvestigationDto>>))]
    public async Task<IResult> GetOosInvestigations(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string searchQuery = null,
        [FromQuery] OosInvestigationStatus? status = null
    )
    {
        if (!await Allowed(PermissionKeys.CanViewOosInvestigations)) return TypedResults.Forbid();
        var result = await repository.GetOosInvestigations(page, pageSize, searchQuery, status);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
    private async Task<bool> Allowed(string permission)
    {
        if (!Guid.TryParse(HttpContext.Items["Sub"] as string, out var actor)) return false;
        var grants = await APP.Services.Authorization.CurrentPermissionSet.Load(context, actor);
        return grants.Contains(permission);
    }
}
