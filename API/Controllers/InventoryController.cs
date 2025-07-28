using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using DOMAIN.Entities.Inventory;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/v{Version:apiVersion}/inventories")]
[Authorize]
public class InventoryController(IInventoryRepository repository) : ControllerBase
{
    /// <summary>
    /// Creates an inventory
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> CreateInventory([FromBody] CreateInventoryRequest request)
    {
        var result = await repository.CreateInventory(request);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a paginated list of inventories
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<InventoryDto>>))]
    public async Task<IResult> GetInventories([FromQuery] int page = 1, [FromQuery] int pageSize = 10,
        [FromQuery] string searchQuery = null)
    {
        var result = await repository.GetInventories(page, pageSize, searchQuery);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves an inventory by its ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(InventoryDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetInventory([FromRoute] Guid id)
    {
        var result = await repository.GetInventory(id);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Updates an inventory
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent, Type = typeof(InventoryDto))]
    public async Task<IResult> UpdateInventory([FromRoute] Guid id, [FromBody] CreateInventoryRequest request)
    {
        var result = await repository.UpdateInventory(id, request);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Deletes an inventory
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> DeleteInventory([FromRoute] Guid id)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();
        
        var result = await repository.DeleteInventory(id, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
}