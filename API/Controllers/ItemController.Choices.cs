using APP.Services.Inventory;
using APP.Services.Stores;
using APP.Utils;
using DOMAIN.Entities.Items;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers;

/// <summary>An active store item offered for a requisition, with what is in stock.</summary>
public class StoreItemChoiceDto
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Code { get; set; }
    public Store Store { get; set; }
    public InventoryClassification Classification { get; set; }
    public int AvailableQuantity { get; set; }
    public Guid UnitOfMeasureId { get; set; }
    public string UnitOfMeasureName { get; set; }
    public string UnitOfMeasureSymbol { get; set; }
}

public partial class ItemController
{
    /// <summary>
    /// Active items of one store to choose on a stock or purchase requisition. Open to whoever may view the store's items
    /// or raise its requisitions, without giving them the full item register.
    /// </summary>
    [HttpGet("choices")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<StoreItemChoiceDto>>))]
    public async Task<IResult> GetItemChoices([FromQuery] Store store, [FromQuery] string searchQuery = null,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        if (!Enum.IsDefined(store)) return TypedResults.BadRequest();
        var actor = Actor();
        var allowed = await InventoryItemAccess.Allows(context, actor, store, "ViewItemsIn")
            || await StoreWorkflowAccess.Allows(context, actor, store, StoreWorkflow.StockRequisitions, StoreWorkflowAction.Create)
            || await StoreWorkflowAccess.Allows(context, actor, store, StoreWorkflow.PurchaseRequisitions, StoreWorkflowAction.Create)
            || await StoreWorkflowAccess.Allows(context, actor, store, StoreWorkflow.PurchaseRequisitions, StoreWorkflowAction.Edit)
            || await StoreWorkflowAccess.Allows(context, actor, store, StoreWorkflow.ReturnItems, StoreWorkflowAction.Create);
        if (!allowed) return TypedResults.Forbid();
        page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 1, 100);
        var query = context.Items.AsNoTracking().Where(i => i.Store == store && i.IsActive);
        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            var term = searchQuery.Trim().ToLower();
            query = query.Where(i => i.Name.ToLower().Contains(term) || i.Code.ToLower().Contains(term));
        }
        var total = await query.CountAsync();
        var rows = await query.OrderBy(i => i.Name).Skip((page - 1) * pageSize).Take(pageSize).Select(i => new StoreItemChoiceDto
        {
            Id = i.Id, Name = i.Name, Code = i.Code, Store = i.Store, Classification = i.Classification, AvailableQuantity = i.AvailableQuantity,
            UnitOfMeasureId = i.UnitOfMeasureId, UnitOfMeasureName = i.UnitOfMeasure.Name, UnitOfMeasureSymbol = i.UnitOfMeasure.Symbol
        }).ToListAsync();
        return TypedResults.Ok(PaginationHelper.Page(rows, page, pageSize, total));
    }
}
