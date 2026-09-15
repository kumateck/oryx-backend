using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using DOMAIN.Entities.RndFormulations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/rnd/projects/{rndProjectId:guid}/formulations")]
[Authorize]
public class RndFormulationController(
    IRndFormulationRepository repository,
    IApprovalRepository approvalRepository
) : ControllerBase
{
    /// <summary>
    /// Creates the first version of a formulation for an R&amp;D project.
    /// </summary>
    [HttpPost]
    [Authorize(PermissionKeys.CanCreateRndFormulation)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> CreateFormulation(
        [FromRoute] Guid rndProjectId,
        [FromBody] CreateRndFormulationRequest request
    )
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId is null) return TypedResults.Unauthorized();

        var result = await repository.CreateFormulation(rndProjectId, request, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Creates a new version of an existing formulation, superseding it.
    /// </summary>
    [HttpPost("{formulationId:guid}/new-version")]
    [Authorize(PermissionKeys.CanCreateRndFormulation)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> CreateNewVersion(
        [FromRoute] Guid formulationId,
        [FromBody] CreateRndFormulationRequest request
    )
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId is null) return TypedResults.Unauthorized();

        var result = await repository.CreateNewVersion(formulationId, request, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Updates a formulation's items while it is still Draft.
    /// </summary>
    [HttpPut("{formulationId:guid}")]
    [Authorize(PermissionKeys.CanEditRndFormulation)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> UpdateFormulation(
        [FromRoute] Guid formulationId,
        [FromBody] CreateRndFormulationRequest request
    )
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId is null) return TypedResults.Unauthorized();

        var result = await repository.UpdateFormulation(formulationId, request, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Moves a formulation to the next status (Draft to InReview to Approved).
    /// </summary>
    [HttpPut("{formulationId:guid}/status")]
    [Authorize(PermissionKeys.CanEditRndFormulation)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> UpdateStatus([FromRoute] Guid formulationId, [FromBody] RndFormulationStatus status)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId is null) return TypedResults.Unauthorized();

        var result = await repository.UpdateStatus(formulationId, status, Guid.Parse(userId));
        if (result.IsSuccess && status == RndFormulationStatus.InReview)
            await approvalRepository.CreateInitialApprovalsAsync(nameof(RndFormulation), formulationId);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a formulation by its ID.
    /// </summary>
    [HttpGet("{formulationId:guid}")]
    [Authorize(PermissionKeys.CanViewRndFormulations)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(RndFormulationDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetFormulation([FromRoute] Guid formulationId)
    {
        var result = await repository.GetFormulation(formulationId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves the paginated formulation history for an R&amp;D project, newest version first.
    /// </summary>
    [HttpGet]
    [Authorize(PermissionKeys.CanViewRndFormulations)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<RndFormulationDto>>))]
    public async Task<IResult> GetFormulationsForProject(
        [FromRoute] Guid rndProjectId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10
    )
    {
        var result = await repository.GetFormulationsForProject(rndProjectId, page, pageSize);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
}
