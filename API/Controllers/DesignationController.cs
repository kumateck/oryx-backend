using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using DOMAIN.Entities.Designations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Route("api/v{version:apiVersion}/designation")]
[ApiController]
[Authorize]
public class DesignationController(IDesignationRepository repository, INFRASTRUCTURE.Context.ApplicationDbContext context) : ControllerBase
{

    /// <summary>
    /// Creates a designation.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> CreateDesignation([FromBody] CreateDesignationRequest request)
    {
        if (!await HasAccess("CanViewDesignation", "CanCreateDesignation")) return TypedResults.Forbid();

        var result = await repository.CreateDesignation(request);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a paginated list of designations based on search criteria.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<DesignationDto>>))]
    public async Task<IResult> GetDesignations([FromQuery] int page = 1, [FromQuery] int pageSize = 10,
        [FromQuery] string searchQuery = null, [FromQuery] Guid? departmentId = null)
    {
        if (!await HasAccess("CanViewDesignation", "CanViewDesignation")) return TypedResults.Forbid();

        var result = await repository.GetDesignations(page, pageSize, searchQuery, departmentId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
    /// <summary>
    /// Retrieves the details of a specific designation
    /// </summary>

    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(DesignationDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetDesignation([FromRoute] Guid id)
    {
        if (!await HasAccess("CanViewDesignation", "CanViewDesignation")) return TypedResults.Forbid();
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.GetDesignation(id);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpGet("department/{id:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<DesignationDto>))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetDesignationByDepartment([FromRoute] Guid id)
    {
        if (!await HasAccess("CanViewDesignation", "CanViewDesignation")) return TypedResults.Forbid();
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.GetDesignationByDepartment(id);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Updates the details of an existing designation
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent, Type = typeof(DesignationDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> UpdateDesignation([FromRoute] Guid id, [FromBody] CreateDesignationRequest request)
    {
        if (!await HasAccess("CanViewDesignation", "CanEditDesignation")) return TypedResults.Forbid();
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.UpdateDesignation(id, request);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Deletes a specific designation by its ID.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> DeleteDesignation([FromRoute] Guid id)
    {
        if (!await HasAccess("CanViewDesignation", "CanDeleteDesignation")) return TypedResults.Forbid();
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.DeleteDesignation(id, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
    private async Task<bool> HasAccess(string read, string action)
    {
        if (!Guid.TryParse(HttpContext.Items["Sub"] as string, out var actor)) return false;
        var keys = await APP.Services.Authorization.CurrentPermissionSet.Load(context, actor);
        return keys.Contains(read) && keys.Contains(action);
    }
}