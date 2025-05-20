using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using DOMAIN.Entities.StandardTestProcedures;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/standard-test-procedures")]
[Authorize]
public class StandardTestProcedureController(IStandardTestProcedureRepository repository) : ControllerBase
{
    /// <summary>
    /// Creates a standard test procedure
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]  
    public async Task<IResult> CreateStandardTestProcedure([FromBody] CreateStandardTestProcedureRequest request)
    {
        var result = await repository.CreateStandardTestProcedure(request);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
    
    /// <summary>
    /// Retrieves a paginated list of standard test procedures.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<StandardTestProcedureDto>>))]
    public async Task<IResult> GetStandardTestProcedures([FromQuery] int page, [FromQuery] int pageSize, [FromQuery] string searchQuery)
    {
        var result = await repository.GetStandardTestProcedures(page, pageSize, searchQuery);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
    
    /// <summary>
    /// Retrieves the details of a specific standard test procedure by its ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(StandardTestProcedureDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetStandardTestProcedure([FromRoute] Guid id)
    {
        var result = await repository.GetStandardTestProcedure(id);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
    
    /// <summary>
    /// Updates the details of an existing standard test procedure.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent, Type = typeof(StandardTestProcedureDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> UpdateStandardTestProcedure([FromRoute] Guid id, [FromBody] CreateStandardTestProcedureRequest request)
    {
        var result = await repository.UpdateStandardTestProcedure(id, request);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Deletes a specific standard test procedure by its ID.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> DeleteStandardTestProcedure([FromRoute] Guid id)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();
        
        var result = await repository.DeleteStandardTestProcedure(id, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
    
}