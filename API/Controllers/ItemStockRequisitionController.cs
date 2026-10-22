using APP.Extensions;
using APP.IRepository;
using APP.Services.Stores;
using APP.Utils;
using DOMAIN.Entities.Items;
using DOMAIN.Entities.ItemStockRequisitions;
using INFRASTRUCTURE.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers;

/// <summary>
/// Store stock requisitions. Access follows the requisition's store: its stock-requisition keys to raise and read it,
/// and its issue-stock keys to read it and issue against it.
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/items/stock-requisitions")]
[Authorize]
public class ItemStockRequisitionController(IItemStockRequisitionRepository repository, ApplicationDbContext context) : ControllerBase
{
    /// <summary>
    /// Creates an item stock requisition for the signed-in user. All items must be from one store.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IResult> CreateItem([FromBody] CreateItemStockRequisitionRequest request)
    {
        if (Actor() is not { } actor) return TypedResults.Unauthorized();
        if (!await AllowedForItems(request, StoreWorkflow.StockRequisitions, StoreWorkflowAction.Create)) return TypedResults.Forbid();
        var result = await repository.CreateItemStockRequisition(request, actor);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a paginated list of the item stock requisitions of the stores the user may read.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<ItemStockRequisitionDto>>))]
    public async Task<IResult> GetItems([FromQuery] int page = 1, [FromQuery] int pageSize = 10,
        [FromQuery] string searchQuery = null, [FromQuery] Store? store = null, [FromQuery] IssueItemStockRequisitionStatus? status = null)
    {
        // Returners read issued requisitions to choose what is being returned.
        var readable = await StoreWorkflowAccess.Readable(context, Actor(), StoreWorkflow.StockRequisitions, StoreWorkflow.IssueStock, StoreWorkflow.ReturnItems);
        if (readable.Count == 0 || (store.HasValue && !readable.Contains(store.Value))) return TypedResults.Forbid();
        var result = await repository.GetItemStockRequisitions(page, pageSize, searchQuery, readable, store, status);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves an item stock requisition by its ID, with what has been issued and what remains on each line.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ItemStockRequisitionDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetItem([FromRoute] Guid id)
    {
        var stores = await repository.StoresOf(id);
        if (stores.Count == 0) return TypedResults.NotFound();
        if (!await AllowedRead(stores)) return TypedResults.Forbid();
        var result = await repository.GetItemStockRequisition(id);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Updates an item stock requisition that has not had stock issued against it.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IResult> UpdateItem([FromRoute] Guid id, [FromBody] CreateItemStockRequisitionRequest request)
    {
        if (Actor() is null) return TypedResults.Unauthorized();
        var stores = await repository.StoresOf(id);
        if (stores.Count == 0) return TypedResults.NotFound();
        if (!await StoreWorkflowAccess.AllowsAll(context, Actor(), stores, StoreWorkflow.StockRequisitions, StoreWorkflowAction.Create)
            || !await AllowedForItems(request, StoreWorkflow.StockRequisitions, StoreWorkflowAction.Create)) return TypedResults.Forbid();
        var result = await repository.UpdateItemStockRequisition(id, request);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Deletes an item stock requisition that has not had stock issued against it.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> DeleteItem([FromRoute] Guid id)
    {
        if (Actor() is not { } actor) return TypedResults.Unauthorized();
        var stores = await repository.StoresOf(id);
        if (stores.Count == 0) return TypedResults.NotFound();
        if (!await StoreWorkflowAccess.AllowsAll(context, actor, stores, StoreWorkflow.StockRequisitions, StoreWorkflowAction.Create)) return TypedResults.Forbid();
        var result = await repository.DeleteItemStockRequisition(id, actor);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Issues stock against an item stock requisition. Quantities cannot exceed what remains on each line or what is in stock.
    /// </summary>
    [HttpPost("{stockRequisitionId:guid}/issue-stock-against-requisition")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<IResult> IssueStockAgainstRequisition([FromRoute] Guid stockRequisitionId, [FromBody] IssueStockAgainstRequisitionRequest request) =>
        Issue(stockRequisitionId, request);

    /// <summary>
    /// Issues more stock against a partly issued requisition. Kept for existing clients; it is the same issue.
    /// </summary>
    [HttpPost("{stockRequisitionId:guid}/partial-issue-stock-against-requisition")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<IResult> IssueStockAgainstRequisitionPartial([FromRoute] Guid stockRequisitionId, [FromBody] IssueStockAgainstRequisitionRequest request) =>
        Issue(stockRequisitionId, request);

    private async Task<IResult> Issue(Guid id, IssueStockAgainstRequisitionRequest request)
    {
        if (Actor() is not { } actor) return TypedResults.Unauthorized();
        var stores = await repository.StoresOf(id);
        if (stores.Count == 0) return TypedResults.NotFound();
        if (!await StoreWorkflowAccess.AllowsAll(context, actor, stores, StoreWorkflow.IssueStock, StoreWorkflowAction.Create)) return TypedResults.Forbid();
        var result = await repository.IssueStock(id, request, actor);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    private Guid? Actor() => Guid.TryParse(HttpContext.Items["Sub"] as string, out var id) ? id : null;

    private async Task<bool> AllowedRead(IReadOnlyCollection<Store> stores) =>
        await StoreWorkflowAccess.AllowsAll(context, Actor(), stores, StoreWorkflow.StockRequisitions, StoreWorkflowAction.View)
        || await StoreWorkflowAccess.AllowsAll(context, Actor(), stores, StoreWorkflow.IssueStock, StoreWorkflowAction.View)
        || await StoreWorkflowAccess.AllowsAll(context, Actor(), stores, StoreWorkflow.ReturnItems, StoreWorkflowAction.View);

    private async Task<bool> AllowedForItems(CreateItemStockRequisitionRequest request, StoreWorkflow workflow, StoreWorkflowAction action)
    {
        var ids = (request?.StockItems ?? []).Select(i => i.ItemId).Distinct().ToList();
        var stores = await context.Items.AsNoTracking().Where(i => ids.Contains(i.Id)).Select(i => i.Store).Distinct().ToListAsync();
        if (stores.Count > 0) return await StoreWorkflowAccess.AllowsAll(context, Actor(), stores, workflow, action);
        // No known item: the repository reports the problem, but only to someone who may act in some store.
        foreach (var store in StoreWorkflowAccess.Stores)
            if (await StoreWorkflowAccess.Allows(context, Actor(), store, workflow, action)) return true;
        return false;
    }
}
