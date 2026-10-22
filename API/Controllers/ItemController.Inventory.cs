using APP.Extensions;
using APP.Utils;
using APP.Services.Inventory;
using DOMAIN.Entities.Items;
using DOMAIN.Entities.ItemTransactionLogs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers;

public partial class ItemController
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
        if (!await Allowed(request.Store, "CreateNewItemIn")) return TypedResults.Forbid();
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
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<ItemDto>>))]
    public async Task<IResult> GetItems([FromQuery] Store? store, [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string searchQuery = null)
    {
        if (store.HasValue) {
            if (!await Allowed(store.Value, "ViewItemsIn")) return TypedResults.Forbid();
        } else {
            foreach (var scope in Enum.GetValues<Store>())
                if (!await Allowed(scope, "ViewItemsIn")) return TypedResults.Forbid();
        }
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
        if (!await AllowedItem(id, "ViewItemsIn", true)) return TypedResults.Forbid();
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
        if (!await AllowedItem(id, "EditItemIn") || !await Allowed(request.Store, "EditItemIn")) return TypedResults.Forbid();
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

        if (!await AllowedItem(id, "DeleteItemFrom")) return TypedResults.Forbid();
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
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<ItemTransactionLogDto>))]
    public async Task<IResult> GetItemTransactions([FromRoute] string itemCode, [FromQuery] string transactionType)
    {
        var items = await context.Items.IgnoreQueryFilters().Where(i => i.Code == itemCode).Take(2).ToListAsync();
        if (items.Count != 1) return SHARED.Result.Failure(SHARED.Error.Validation("Item.AmbiguousLedger", "The code does not identify one historical item. Reconcile the ledger before viewing it.")).ToProblemDetails();
        if (!await InventoryItemAccess.Allows(context, Actor(), items[0].Store, "ViewItemsIn", true, false)) return TypedResults.Forbid();
        var result = await repository.GetItemTransactions(itemCode, transactionType);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }


    private Guid? Actor() => Guid.TryParse(HttpContext.Items["Sub"] as string, out var id) ? id : null;
    private Task<bool> Allowed(Store store, string action, bool stockRead = false) =>
        InventoryItemAccess.Allows(context, Actor(), store, action, stockRead);
    private async Task<bool> AllowedItem(Guid id, string action, bool stockRead = false)
    {
        var item = await context.Items.AsNoTracking().FirstOrDefaultAsync(i => i.Id == id);
        return item != null && await Allowed(item.Store, action, stockRead);
    }
}
