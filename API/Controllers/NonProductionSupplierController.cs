using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using DOMAIN.Entities.NonProductionSuppliers;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

public class NonProductionSupplierController(INonProductionSupplierRepository repository) : ControllerBase
{
     /// <summary>
    /// Creates a non production supplier
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> CreateNonProductionSupplier([FromBody] CreateNonProductionSupplierRequest request)
    {
        var result = await repository.CreateNonProductionSupplier(request);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
    
    /// <summary>
    /// Retrieves a paginated list of non-production suppliers 
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<NonProductionSupplierDto>>))]
    public async Task<IResult> GetServiceProviders([FromQuery] int page = 1, [FromQuery] int pageSize = 10,
        [FromQuery] string searchQuery = null)
    {
        var result = await repository.GetNonProductionSuppliers(page, pageSize, searchQuery);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a non production supplier by its unique identifier.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(NonProductionSupplierDto))]
    public async Task<IResult> GetServiceProvider([FromRoute] Guid id)
    {
        var result = await repository.GetNonProductionSupplier(id);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Updates an existing non production supplier.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent, Type = typeof(NonProductionSupplierDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> UpdateServiceProvider([FromRoute] Guid id, [FromBody] CreateNonProductionSupplierRequest request)
    {
        var result = await repository.UpdateNonProductionSupplier(id, request);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Deletes a nonproduction supplier using the specified supplier ID.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> DeleteServiceProvider([FromRoute] Guid id)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.DeleteNonProductionSupplier(id, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
}