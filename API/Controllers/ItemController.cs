using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using DOMAIN.Entities.Items;
using DOMAIN.Entities.ItemTransactionLogs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/v{Version:apiVersion}/items")]
[Authorize]
public class ItemController(IItemRepository repository) : ControllerBase
{
    /// <summary>
    /// Creates an item
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> CreateItem([FromBody] CreateItemsRequest request)
    {
        var result = await repository.CreateItem(request);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Uploads items from an Excel file
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPost("upload")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> UploadItem([FromForm] ImportItemsRequest request)
    {
        var result = await repository.UploadItems(request);
        return result.IsSuccess ? TypedResults.Ok() : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a paginated list of items
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<ItemDto>>))]
    public async Task<IResult> GetItems([FromQuery] Store? store, [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string searchQuery = null)
    {
        var result = await repository.GetItems(page, pageSize, searchQuery, store);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves an item by its ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ItemDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetItem([FromRoute] Guid id)
    {
        var result = await repository.GetItem(id);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Updates an item
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent, Type = typeof(ItemDto))]
    public async Task<IResult> UpdateItem([FromRoute] Guid id, [FromBody] CreateItemsRequest request)
    {
        var result = await repository.UpdateItem(id, request);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Deletes an item
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> DeleteItem([FromRoute] Guid id)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.DeleteItem(id, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
    
    /// <summary>
    /// Displays the transactions on an item
    /// </summary>
    /// <param name="itemCode"> The item code</param>
    /// <param name="transactionType"> The transaction type</param>
    /// <returns></returns>
    [HttpGet("{itemCode}/transactions")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<ItemTransactionLogDto>))]
    public async Task<IResult> GetItemTransactions([FromRoute] string itemCode, [FromQuery] string transactionType)
    {
        var result = await repository.GetItemTransactions(itemCode, transactionType);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
}