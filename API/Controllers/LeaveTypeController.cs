using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using DOMAIN.Entities.LeaveTypes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/leave-type")]
[Authorize]
public class LeaveTypeController(ILeaveTypeRepository repository): ControllerBase
{

    /// <summary>
    /// Creates a leave type.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK, Type= typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> CreateLeaveType([FromBody] CreateLeaveTypeRequest leaveType)
    {
        var userId = (string) HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();
        
        var result = await repository.CreateLeaveType(leaveType);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();

    }

    /// <summary>
    /// Returns a paginated list of leave types based on a search criteria.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<LeaveTypeDto>>))]
    public async Task<IResult> GetLeaveTypes([FromQuery] int page, [FromQuery] int pageSize, [FromQuery] string searchQuery)
    {
        var userId = (string) HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();
        
        var result = await repository.GetLeaveTypes(page, pageSize, searchQuery);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();

    }
    
    /// <summary>
    /// Retrieves the details of a specific leave type.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(LeaveTypeDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetLeaveType([FromRoute] Guid id)
    {
        var userId = (string) HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();
        
        var result = await repository.GetLeaveType(id);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();

    }

    /// <summary>
    /// Updates the details of an existing leave type.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent, Type = typeof(LeaveTypeDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> UpdateLeaveType([FromRoute] Guid id, [FromBody] CreateLeaveTypeRequest leaveType)
    {
        var userId = (string) HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();
        
        var result = await repository.UpdateLeaveType(id, leaveType);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();

    }

    /// <summary>
    /// Deletes a specific leave type by its ID.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> DeleteLeaveType([FromRoute] Guid id)
    {
        var userId = (string) HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();
        
        var result = await repository.DeleteLeaveType(id, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
}