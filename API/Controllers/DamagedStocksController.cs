using APP.Services.Inventory;
using Microsoft.EntityFrameworkCore;
using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using DOMAIN.Entities.DamagedStocks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/damaged-goods")]
[Authorize]
public class DamagedStocksController(IDamagedStocksRepository repository, INFRASTRUCTURE.Context.ApplicationDbContext context) : ControllerBase
{
    /// <summary>
    /// Creates a damaged stock record
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> CreateDamagedStocks([FromBody] CreateDamagedStockRequest request)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();
        if (!Guid.TryParse(userId, out var actor)) return TypedResults.Unauthorized();
        var item = await context.Items.AsNoTracking().FirstOrDefaultAsync(i => i.Id == request.ItemId);
        if (item == null || !await InventoryStockAccess.Allows(context, actor, item.Store, InventoryStockOperation.CreateDamage))
            return TypedResults.Forbid();
        var result = await repository.CreateDamagedStocks(request, actor);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a paginated list of the damage and missing records of the stores the user may read.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<DamagedStockDto>>))]
    public async Task<IResult> GetDamagedStocks([FromQuery] int page = 1, [FromQuery] int pageSize = 10,
        [FromQuery] string searchQuery = null, [FromQuery] DOMAIN.Entities.Items.Store? store = null, [FromQuery] DamageStatus? damageStatus = null)
    {
        var readable = await APP.Services.Stores.StoreWorkflowAccess.Readable(context, Actor(), APP.Services.Stores.StoreWorkflow.DamageMissing);
        if (readable.Count == 0 || (store.HasValue && !readable.Contains(store.Value))) return TypedResults.Forbid();
        var result = await repository.GetDamagedStocks(page, pageSize, searchQuery, readable, store, damageStatus);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves the details of a damaged stock by its ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(DamagedStockDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetDamagedStock([FromRoute] Guid id)
    {
        if (await StoreOf(id) is not { } store) return TypedResults.NotFound();
        if (!await Allowed(store, APP.Services.Stores.StoreWorkflowAction.View)) return TypedResults.Forbid();
        var result = await repository.GetDamagedStock(id);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Corrects a damage or missing record; the stock difference is posted to the ledger.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IResult> UpdateDamagedStock([FromRoute] Guid id, [FromBody] CreateDamagedStockRequest request)
    {
        if (Actor() is not { } actor) return TypedResults.Unauthorized();
        if (await StoreOf(id) is not { } store) return TypedResults.NotFound();
        if (!await Allowed(store, APP.Services.Stores.StoreWorkflowAction.Edit)) return TypedResults.Forbid();
        var result = await repository.UpdateDamagedStocks(id, request, actor);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Deletes a damage or missing record and gives its quantity back to stock.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IResult> DeleteDamagedStock([FromRoute] Guid id)
    {
        if (Actor() is not { } actor) return TypedResults.Unauthorized();
        if (await StoreOf(id) is not { } store) return TypedResults.NotFound();
        if (!await Allowed(store, APP.Services.Stores.StoreWorkflowAction.Delete)) return TypedResults.Forbid();
        var result = await repository.DeleteDamagedStocks(id, actor);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    private Guid? Actor() => Guid.TryParse(HttpContext.Items["Sub"] as string, out var id) ? id : null;
    private async Task<DOMAIN.Entities.Items.Store?> StoreOf(Guid id) =>
        await context.DamagedStocks.AsNoTracking().Where(d => d.Id == id).Select(d => (DOMAIN.Entities.Items.Store?)d.Item.Store).FirstOrDefaultAsync();
    private Task<bool> Allowed(DOMAIN.Entities.Items.Store store, APP.Services.Stores.StoreWorkflowAction action) =>
        APP.Services.Stores.StoreWorkflowAccess.Allows(context, Actor(), store, APP.Services.Stores.StoreWorkflow.DamageMissing, action);
}
