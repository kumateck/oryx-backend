using APP.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using APP.IRepository;
using APP.Utils;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Materials;
using SHARED;

namespace API.Controllers;

[Route("api/v{version:apiVersion}/collection")]
[ApiController]
public class CollectionController(ICollectionRepository repository) : ControllerBase
{
    /// <summary>
    /// Retrieves a collection of items based on the item type.
    /// </summary>
    /// <param name="itemTypes">The types of items to retrieve</param>
    /// <param name="materialKind">The kind of material</param>
    /// <returns>Returns a collection of items.</returns>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Dictionary<string, IEnumerable<CollectionItemDto>>))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetItemCollection([FromBody] List<string> itemTypes,
        [FromQuery] MaterialKind? materialKind = null)
    {
        var result = await repository.GetItemCollection(itemTypes, materialKind);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a collection of items based on the item type.
    /// </summary>
    /// <param name="itemType">The type of item to retrieve.</param>
    /// <param name="materialKind">The kind of material</param>
    /// <returns>Returns a collection of items.</returns>
    [HttpGet("{itemType}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<CollectionItemDto>))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetItemCollection(string itemType,
        [FromQuery] MaterialKind? materialKind = null)
    {
        var result = await repository.GetItemCollection(itemType, materialKind);
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
    /// Retrieves all available package styles.
    /// </summary>
    /// <returns>Returns a collection of package styles.</returns>
    [HttpGet("package-styles")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<PackageStyleDto>))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetPackageStyles()
    {
        var result = await repository.GetPackageStyles();
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
    
    /// <summary>
    /// Retrieves all available package styles.
    /// </summary>
    /// <returns>Returns a collection of package styles.</returns>
    [HttpPost("uom")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> CreateUoM([FromBody] CreateUnitOfMeasure request)
    {
        var result = await repository.CreateUoM(request);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
    
    /// <summary>
    /// Retrieves a units of measures in the system.
    /// </summary>
    /// <returns>Returns a collection of uom items.</returns>
    [HttpPost("uom/paginated")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<UnitOfMeasureDto>>))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetUoM([FromBody] FilterUnitOfMeasure filter)
    {
        var result = await repository.GetUoM(filter);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
    
    
    /// <summary>
    /// Retrieves a unit of measure by its id
    /// </summary>
    /// <returns>Returns a collection of uom items.</returns>
    [HttpPost("uom/{uomId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UnitOfMeasureDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetUoM([FromRoute] Guid uomId)
    {
        var result = await repository.GetUoM(uomId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
    
    /// <summary>
    /// Retrieves a unit of measure by its id
    /// </summary>
    /// <returns>Updayes a uom.</returns>
    [HttpPut("uom/{uomId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> UpdateUoM([FromRoute] Guid uomId, [FromBody]  CreateUnitOfMeasure request)
    {
        var result = await repository.UpdateUoM(request, uomId);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
    
    
    /// <summary>
    /// Retrieves a unit of measure by its id
    /// </summary>
    /// <returns>Deletes a uom.</returns>
    [HttpDelete("uom/{uomId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> DeleteUoM([FromRoute] Guid uomId)
    {
        var result = await repository.DeleteUoM(uomId);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
}
