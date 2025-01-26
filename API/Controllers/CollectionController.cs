using APP.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using APP.IRepository;
using DOMAIN.Entities.Base;
using SHARED;

namespace API.Controllers;

[Route("api/v{version:apiVersion}/collection")]
[ApiController]
public class CollectionController(ICollectionRepository repository) : ControllerBase
{
    /// <summary>
    /// Retrieves a collection of items based on the item type.
    /// </summary>
    /// <returns>Returns a collection of items.</returns>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Dictionary<string, IEnumerable<CollectionItemDto>>))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetItemCollection([FromBody] List<string> itemTypes)
    {
        var result = await repository.GetItemCollection(itemTypes);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
    
    /// <summary>
    /// Retrieves a collection of items based on the item type.
    /// </summary>
    /// <param name="itemType">The type of item to retrieve.</param>
    /// <returns>Returns a collection of items.</returns>
    [HttpGet("{itemType}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<CollectionItemDto>))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetItemCollection(string itemType)
    {
        var result = await repository.GetItemCollection(itemType);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves all available item types.
    /// </summary>
    /// <returns>Returns a list of item types.</returns>
    [HttpGet("item-types")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<string>))]
    public IResult GetItemTypes()
    {
        var result = repository.GetItemTypes();
        return TypedResults.Ok(result.Value);
    }

    /// <summary>
    /// Creates a new item in the collection.
    /// </summary>
    /// <param name="request">The CreateItemRequest object containing item details.</param>
    /// <param name="itemType">The type of item to create.</param>
    /// <returns>Returns the ID of the created item.</returns>
    [HttpPost("{itemType}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> CreateItem([FromBody] CreateItemRequest request, string itemType)
    {
        var result = await repository.CreateItem(request, itemType);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
    
    /// <summary>
    /// Updates an existing item in the collection.
    /// </summary>
    /// <param name="request">The UpdateItemRequest object containing updated item details.</param>
    /// <param name="itemId">The ID of the item to update.</param>
    /// <param name="itemType">The type of item to update.</param>
    /// <returns>Returns the ID of the updated item.</returns>
    [HttpPut("{itemType}/{itemId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> UpdateItem([FromBody] CreateItemRequest request, Guid itemId, string itemType)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();
        
        var result = await repository.UpdateItem(request, itemId, itemType, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
    
    /// <summary>
    /// Deletes an item.
    /// </summary>
    /// <param name="itemId">The ID of the item to delete.</param>
    /// <param name="itemType">The type of item to delete.</param>
    /// <returns>Returns a confirmation of the deleted item's ID.</returns>
    [HttpDelete("{itemType}/{itemId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> DeleteItem(Guid itemId, string itemType)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();
        var result = await repository.SoftDeleteItem(itemId, itemType, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
    
    /// <summary>
    /// Retrieves a units of measures in the system.
    /// </summary>
    /// <returns>Returns a collection of uom items.</returns>
    [HttpGet("uom")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<CollectionItemDto>))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetUoM()
    {
        var result = await repository.GetUoM();
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
}
