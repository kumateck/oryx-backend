using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using DOMAIN.Entities.OvertimeRequests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/overtime-requests")]
[Authorize]
public class OvertimeRequestController(IOvertimeRequestRepository repository) : ControllerBase
{
    
    /// <summary>
    /// Creates an overtime request.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> CreateOvertimeRequest([FromBody] CreateOvertimeRequest request)
    {
        var result = await repository.CreateOvertimeRequest(request);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a paginated list of overtime requests based on a search criteria.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<OvertimeRequestDto>>))]
    public async Task<IResult> GetOvertimeRequests([FromQuery] int page, [FromQuery] int pageSize, [FromQuery] string searchQuery)
    {
        var result = await repository.GetOvertimeRequests(page, pageSize, searchQuery);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
    
    /// <summary>
    /// Retrieves the details of a specific overtime request by its ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(OvertimeRequestDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetOvertimeRequest([FromRoute] Guid id)
    {
        var result = await repository.GetOvertimeRequest(id);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
    
    /// <summary>
    /// Updates a specific overtime request by its ID.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent, Type = typeof(OvertimeRequestDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> UpdateOvertimeRequest([FromRoute] Guid id, [FromBody] CreateOvertimeRequest request)
    {
        var result = await repository.UpdateOvertimeRequest(id, request);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
    
    /// <summary>
    /// Deletes a specific overtime request by its ID.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> DeleteOvertimeRequest([FromRoute] Guid id)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();
        
        var result = await repository.DeleteOvertimeRequest(id, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
    
}