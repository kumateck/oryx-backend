using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using DOMAIN.Entities.Warehouses;
using DOMAIN.Entities.Warehouses.Request;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Route("api/v{version:apiVersion}/warehouse")]
[ApiController]
public class WarehouseController(IWarehouseRepository repository) : ControllerBase
{
    /// <summary>
    /// Creates a new warehouse.
    /// </summary>
    /// <param name="request">The CreateWarehouseRequest object.</param>
    /// <returns>Returns the ID of the created warehouse.</returns>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> CreateWarehouse([FromBody] CreateWarehouseRequest request)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.CreateWarehouse(request, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a warehouse by its ID.
    /// </summary>
    /// <param name="warehouseId">The ID of the warehouse.</param>
    /// <returns>Returns the warehouse details.</returns>
    [HttpGet("{warehouseId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(WarehouseDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetWarehouse(Guid warehouseId)
    {
        var result = await repository.GetWarehouse(warehouseId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a paginated list of warehouses.
    /// </summary>
    /// <param name="page">The current page number.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="searchQuery">Search query for filtering results.</param>
    /// <returns>Returns a paginated list of warehouses.</returns>
    [HttpGet]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<WarehouseDto>>))]
    public async Task<IResult> GetWarehouses([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string searchQuery = null)
    {
        var result = await repository.GetWarehouses(page, pageSize, searchQuery);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Updates a specific warehouse by its ID.
    /// </summary>
    /// <param name="request">The CreateWarehouseRequest object.</param>
    /// <param name="warehouseId">The ID of the warehouse to update.</param>
    /// <returns>Returns success or failure.</returns>
    [HttpPut("{warehouseId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> UpdateWarehouse([FromBody] CreateWarehouseRequest request, Guid warehouseId)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.UpdateWarehouse(request, warehouseId, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Deletes a specific warehouse by its ID.
    /// </summary>
    /// <param name="warehouseId">The ID of the warehouse to delete.</param>
    /// <returns>Returns success or failure.</returns>
    [HttpDelete("{warehouseId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> DeleteWarehouse(Guid warehouseId)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.DeleteWarehouse(warehouseId, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
}
