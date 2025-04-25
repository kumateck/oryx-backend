using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using DOMAIN.Entities.ShiftTypes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/shift-type")]
public class ShiftTypeController(IShiftTypeRepository repository): ControllerBase
{
    /// <summary>
    /// Creates a new shift type.
    /// </summary>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> CreateShiftType([FromBody] CreateShiftTypeRequest shiftType)
    {
        var userId = (string) HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();
        
        var result = await repository.CreateShiftType(shiftType, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.Ok(result.Value) : TypedResults.NotFound();
    }
    
    /// <summary>
    /// Retrieves a paginated list of shift types based on search criteria.
    /// </summary>
    [HttpGet]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<ShiftTypeDto>>))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetShiftTypes([FromQuery] int page, [FromQuery] int pageSize, [FromQuery] string searchQuery)
    {
        var userId = (string) HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();
        
        var result = await repository.GetShiftTypes(page, pageSize, searchQuery);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : TypedResults.NotFound();
    }


    /// <summary>
    /// Retrieves the details of a specific shift type by its ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ShiftTypeDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetShiftType([FromRoute] Guid id)
    {
        var userId = (string) HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();
        
        var result = await repository.GetShiftType(id);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : TypedResults.NotFound();
    }

    /// <summary>
    /// Updates the details of an existing shift type.
    /// </summary>
    [HttpPut]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent, Type = typeof(ShiftTypeDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> UpdateShiftType([FromRoute] Guid id, [FromBody] CreateShiftTypeRequest shiftType)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.UpdateShiftType(id, shiftType, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
    
    /// <summary>
    /// Deletes a specific shift type by its ID.
    /// </summary>
    [HttpDelete]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> DeleteShiftType([FromRoute] Guid id)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();
        
        var result = await repository.DeleteShiftType(id, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
}