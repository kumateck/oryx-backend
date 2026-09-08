using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using DOMAIN.Entities.RndProjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/rnd/projects")]
[Authorize]
public class RndProjectController(IRndProjectRepository repository) : ControllerBase
{
    /// <summary>
    /// Creates a new R&amp;D project intake record.
    /// </summary>
    [HttpPost]
    [Authorize(PermissionKeys.CanCreateRndProject)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> CreateProject([FromBody] CreateRndProjectRequest request)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId is null) return TypedResults.Unauthorized();

        var result = await repository.CreateProject(request, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Updates an R&amp;D project while it is still in Intake.
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(PermissionKeys.CanEditRndProject)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> UpdateProject([FromRoute] Guid id, [FromBody] UpdateRndProjectRequest request)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId is null) return TypedResults.Unauthorized();

        var result = await repository.UpdateProject(id, request, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Moves an R&amp;D project between non-approval-gated statuses
    /// (FeasibilityReview, TechnologyTransfer, Completed, OnHold, Cancelled).
    /// The Intake to InDevelopment transition happens via the generic approval
    /// endpoints (POST /approval/approve/RndProject/{id}), not here.
    /// </summary>
    [HttpPut("{id:guid}/status")]
    [Authorize(PermissionKeys.CanUpdateRndProjectStatus)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> UpdateStatus([FromRoute] Guid id, [FromBody] UpdateRndProjectStatusRequest request)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId is null) return TypedResults.Unauthorized();

        var result = await repository.UpdateStatus(id, request, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Deletes (soft) an R&amp;D project while it is still in Intake.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(PermissionKeys.CanDeleteRndProject)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> DeleteProject([FromRoute] Guid id)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId is null) return TypedResults.Unauthorized();

        var result = await repository.DeleteProject(id, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves an R&amp;D project by its ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [Authorize(PermissionKeys.CanViewRndProjects)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(RndProjectDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetProject([FromRoute] Guid id)
    {
        var result = await repository.GetProject(id);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a paginated list of R&amp;D projects.
    /// </summary>
    [HttpGet]
    [Authorize(PermissionKeys.CanViewRndProjects)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<RndProjectDto>>))]
    public async Task<IResult> GetProjects(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string searchQuery = null,
        [FromQuery] RndProjectStatus? status = null
    )
    {
        var result = await repository.GetProjects(page, pageSize, searchQuery, status);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
}
