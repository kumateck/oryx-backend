using DOMAIN.Entities.AbsenceRequests;

namespace API.Controllers;

using APP.IRepository;
using APP.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


[ApiController]
[Route("api/v{version:apiVersion}/absence-request")]
public class AbsenceRequestController(IAbsenceRequestRepository repository): ControllerBase
{

    /// <summary>
    /// Creates a absence request.
    /// </summary>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type= typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> CreateAbsenceRequest([FromBody] CreateAbsenceRequest leaveRequest)
    { 
        var userId = (string) HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();
        
        var result = await repository.CreateAbsenceRequest(leaveRequest, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.Ok(result.Value) : TypedResults.NotFound();

    }
    

    /// <summary>
    /// Returns a paginated list of absence requests based on a search criteria.
    /// </summary>
    [HttpGet]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<AbsenceRequestDto>>))]
    public async Task<IResult> GetAbsenceRequests([FromQuery] int page, [FromQuery] int pageSize, [FromQuery] string searchQuery)
    {
        var userId = (string) HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();
        
        var result = await repository.GetAbsenceRequests(page, pageSize, searchQuery);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : TypedResults.NotFound();

    }
    
    /// <summary>
    /// Retrieves the details of a specific absence request.
    /// </summary>
    [HttpGet("{id:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(AbsenceRequestDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetAbsenceRequest([FromRoute] Guid id)
    {
        var userId = (string) HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();
        
        var result = await repository.GetAbsenceRequest(id);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : TypedResults.NotFound();

    }

    /// <summary>
    /// Updates the details of an existing absence request.
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent, Type = typeof(AbsenceRequestDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> UpdateAbsenceRequest([FromRoute] Guid id, [FromBody] CreateAbsenceRequest leaveRequest)
    {
        var userId = (string) HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();
        
        var result = await repository.UpdateAbsenceRequest(id, leaveRequest, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : TypedResults.NotFound();

    }

    /// <summary>
    /// Deletes a specific absence request by its ID.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> DeleteAbsenceRequest([FromRoute] Guid id)
    {
        var userId = (string) HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();
        
        var result = await repository.DeleteAbsenceRequest(id, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : TypedResults.NotFound();
    }
}