using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using DOMAIN.Entities.LeaveRequests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/leave-request")]
[Authorize]
public class LeaveRequestController(ILeaveRequestRepository repository): ControllerBase
{

    /// <summary>
    /// Creates a leave request.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK, Type= typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> CreateLeaveOrAbsenceRequest([FromBody] CreateLeaveRequest leaveRequest)
    {
        var userId = (string) HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();
        
        var result = await repository.CreateLeaveOrAbsenceRequest(leaveRequest);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Returns a paginated list of leave requests based on a search criteria.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<LeaveRequestDto>>))]
    public async Task<IResult> GetLeaveRequests([FromQuery] int page, [FromQuery] int pageSize, [FromQuery] string searchQuery)
    {
        var userId = (string) HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();
        
        var result = await repository.GetLeaveRequests(page, pageSize, searchQuery);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
    
    /// <summary>
    /// Retrieves the details of a specific leave request.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(LeaveRequestDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetLeaveRequest([FromRoute] Guid id)
    {
        var userId = (string) HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();
        
        var result = await repository.GetLeaveRequest(id);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Updates the details of an existing leave request.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent, Type = typeof(LeaveRequestDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> UpdateLeaveRequest([FromRoute] Guid id, [FromBody] CreateLeaveRequest leaveRequest)
    {
        var userId = (string) HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();
        
        var result = await repository.UpdateLeaveRequest(id, leaveRequest);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
    
    /// <summary>
    /// Recalls an employee from a leave.
    /// </summary>
    [HttpPut("recall")]
    [ProducesResponseType(StatusCodes.Status204NoContent, Type = typeof(LeaveRequestDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> SubmitLeaveRequest([FromBody] CreateLeaveRecallRequest leaveRecallRequest)
    {
        var result = await repository.SubmitLeaveRecallRequest(leaveRecallRequest);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Deletes a specific leave request by its ID.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> DeleteLeaveRequest([FromRoute] Guid id)
    {
        var userId = (string) HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();
        
        var result = await repository.DeleteLeaveRequest(id, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
}