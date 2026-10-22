using APP.Extensions;
using APP.Services.Inventory;
using DOMAIN.Entities.Approvals;
using DOMAIN.Entities.Items;
using DOMAIN.Entities.StockEntries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers;
public partial class InventoryProcurementController
{
    [HttpGet("purchased-items")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<StockEntryDto>))]
    public async Task<IResult> GetStockEntries([FromQuery] ApprovalStatus status, [FromQuery] int page = 1, [FromQuery] int pageSize = 10,
        [FromQuery] Store store = Store.EquipmentStore)
    {
        if (status is not (ApprovalStatus.Pending or ApprovalStatus.Approved or ApprovalStatus.Rejected)) return TypedResults.BadRequest();
        var operation = status == ApprovalStatus.Approved ? InventoryStockOperation.ViewAvailable : InventoryStockOperation.ViewReceiving;
        // Procurement follows what it bought through its purchase-order keys; stores through their stock keys.
        var procurement = await Has("CanViewPurchaseOrdersForTrustedVendors") || await Has("CanViewOpenMarketPurchaseOrders");
        if (!procurement && !await InventoryStockAccess.Allows(context, StockActor(), store, operation)) return TypedResults.Forbid();
        var result = await repository.GetStockEntries(status, page, pageSize, store);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPost("items/{id:guid}/approve")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IResult> ApproveItem([FromRoute] Guid id)
    {
        if (!await CanDecideStock(id, InventoryStockOperation.ApproveReceipt)) return TypedResults.Forbid();
        var result = await repository.ApproveItem(id);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    [HttpPost("items/{id:guid}/reject")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IResult> RejectItem([FromRoute] Guid id)
    {
        if (!await CanDecideStock(id, InventoryStockOperation.RejectReceipt)) return TypedResults.Forbid();
        var result = await repository.RejectItem(id);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }


    private Guid? StockActor() => Guid.TryParse(HttpContext.Items["Sub"] as string, out var id) ? id : null;
    private async Task<bool> CanDecideStock(Guid id, InventoryStockOperation operation)
    {
        var actor = StockActor();
        var entry = await context.StockEntries.Include(e => e.Item).AsNoTracking().FirstOrDefaultAsync(e => e.Id == id);
        return actor.HasValue && entry != null && entry.CreatedById.HasValue && entry.CreatedById != actor &&
            await InventoryStockAccess.Allows(context, actor, entry.Item.Store, operation);
    }
}
