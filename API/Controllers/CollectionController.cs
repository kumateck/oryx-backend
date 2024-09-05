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
        return result.IsSuccess ? TypedResults.Created("", result.Value) : result.ToProblemDetails();
    }
}
