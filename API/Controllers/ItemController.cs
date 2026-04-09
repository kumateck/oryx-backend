using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using DOMAIN.Entities.ItemShipments;
using DOMAIN.Entities.Items;
using DOMAIN.Entities.ItemTransactionLogs;
using DOMAIN.Entities.PurchaseOrders;
using DOMAIN.Entities.PurchaseOrders.Request;
using DOMAIN.Entities.Shipments;
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

    // ************* Shipment Invoice Endpoints *************

   /// <summary>
    /// Creates a new item shipment invoice.
    /// </summary>
    [HttpPost("shipment-invoice")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> CreateItemShipmentInvoice([FromBody] CreateItemShipmentInvoice request)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.CreateItemShipmentInvoice(request, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves an item shipment invoice by its ID.
    /// </summary>
    [HttpGet("shipment-invoice/{invoiceId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ItemShipmentInvoiceDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetItemShipmentInvoice([FromRoute] Guid invoiceId)
    {
        var result = await repository.GetItemShipmentInvoice(invoiceId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a paginated list of item shipment invoices.
    /// </summary>
    [HttpGet("shipment-invoice")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<ItemShipmentInvoiceDto>>))]
    public async Task<IResult> GetItemShipmentInvoices([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string searchQuery = null)
    {
        var result = await repository.GetItemShipmentInvoices(page, pageSize, searchQuery);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves all item shipment invoices not linked to a shipment document.
    /// </summary>
    [HttpGet("shipment-invoice/unattached")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<ItemShipmentInvoiceDto>))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetUnattachedItemShipmentInvoices()
    {
        var result = await repository.GetUnattachedItemShipmentInvoices();
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Updates a specific item shipment invoice by its ID.
    /// </summary>
    [HttpPut("shipment-invoice/{invoiceId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> UpdateItemShipmentInvoice([FromBody] CreateItemShipmentInvoice request, [FromRoute] Guid invoiceId)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.UpdateItemShipmentInvoice(request, invoiceId, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Marks an item shipment invoice as paid.
    /// </summary>
    [HttpPut("shipment-invoice/{invoiceId:guid}/paid")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> MarkItemShipmentInvoiceAsPaid([FromQuery] DateTime? paidAt, [FromRoute] Guid invoiceId)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.MarkItemShipmentInvoiceAsPaid(invoiceId, paidAt, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Deletes a specific item shipment invoice by its ID.
    /// </summary>
    [HttpDelete("shipment-invoice/{invoiceId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> DeleteItemShipmentInvoice([FromRoute] Guid invoiceId)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.DeleteItemShipmentInvoice(invoiceId, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    // ************* Shipment Document Endpoints *************

    /// <summary>
    /// Creates a new item shipment document.
    /// </summary>
    [HttpPost("shipment-document")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> CreateItemShipmentDocument([FromBody] CreateItemShipmentDocumentRequest request)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.CreateItemShipmentDocument(request, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves an item shipment document by its ID.
    /// </summary>
    [HttpGet("shipment-document/{shipmentDocumentId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ItemShipmentDocumentDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetItemShipmentDocument([FromRoute] Guid shipmentDocumentId)
    {
        var result = await repository.GetItemShipmentDocument(shipmentDocumentId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a paginated list of item shipment documents.
    /// </summary>
    [HttpGet("shipment-document")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<ItemShipmentDocumentDto>>))]
    public async Task<IResult> GetItemShipmentDocuments([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string searchQuery = null,
        [FromQuery] bool? onlyApproved = null)
    {
        var result = await repository.GetItemShipmentDocuments(page, pageSize, searchQuery, onlyApproved);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Updates a specific item shipment document by its ID.
    /// </summary>
    [HttpPut("shipment-document/{shipmentDocumentId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> UpdateItemShipmentDocument([FromBody] CreateItemShipmentDocumentRequest request, [FromRoute] Guid shipmentDocumentId)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.UpdateItemShipmentDocument(request, shipmentDocumentId, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Deletes a specific item shipment document by its ID.
    /// </summary>
    [HttpDelete("shipment-document/{shipmentDocumentId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> DeleteItemShipmentDocument([FromRoute] Guid shipmentDocumentId)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.DeleteItemShipmentDocument(shipmentDocumentId, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Marks an item shipment document as arrived.
    /// </summary>
    [HttpPut("shipment-document/{shipmentDocumentId:guid}/arrived")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> MarkItemShipmentAsArrived([FromRoute] Guid shipmentDocumentId)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.MarkItemShipmentAsArrived(shipmentDocumentId, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    // ************* Waybill Endpoints *************

    /// <summary>
    /// Creates a new item waybill.
    /// </summary>
    [HttpPost("waybill")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> CreateItemWaybill([FromBody] CreateItemShipmentDocumentRequest request)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.CreateItemWaybill(request, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves an item waybill by its ID.
    /// </summary>
    [HttpGet("waybill/{waybillId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ItemShipmentDocumentDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetItemWaybill([FromRoute] Guid waybillId)
    {
        var result = await repository.GetItemWaybill(waybillId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a paginated list of item waybills.
    /// </summary>
    [HttpGet("waybill")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<ItemShipmentDocumentDto>>))]
    public async Task<IResult> GetItemWaybills([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string searchQuery = null,
        [FromQuery] ShipmentStatus? status = null)
    {
        var result = await repository.GetItemWaybills(page, pageSize, searchQuery, status);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Updates a specific item waybill by its ID.
    /// </summary>
    [HttpPut("waybill/{waybillId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> UpdateItemWaybill([FromBody] CreateItemShipmentDocumentRequest request, [FromRoute] Guid waybillId)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.UpdateItemWaybill(request, waybillId, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Deletes a specific item waybill by its ID.
    /// </summary>
    [HttpDelete("waybill/{waybillId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> DeleteItemWaybill([FromRoute] Guid waybillId)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.DeleteItemWaybill(waybillId, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    // ************* Billing Sheet Endpoints *************

    /// <summary>
    /// Creates a new item billing sheet.
    /// </summary>
    [HttpPost("billing-sheet")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> CreateItemBillingSheet([FromBody] CreateItemBillingSheetRequest request)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.CreateItemBillingSheet(request, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves an item billing sheet by its ID.
    /// </summary>
    [HttpGet("billing-sheet/{billingSheetId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ItemBillingSheetDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetItemBillingSheet([FromRoute] Guid billingSheetId)
    {
        var result = await repository.GetItemBillingSheet(billingSheetId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves an item billing sheet by its invoice ID.
    /// </summary>
    [HttpGet("billing-sheet/invoice/{invoiceId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ItemBillingSheetDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetItemBillingSheetByInvoice([FromRoute] Guid invoiceId)
    {
        var result = await repository.GetItemBillingSheetByInvoice(invoiceId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a paginated list of item billing sheets.
    /// </summary>
    [HttpGet("billing-sheet")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<ItemBillingSheetDto>>))]
    public async Task<IResult> GetItemBillingSheets([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string searchQuery = null,
        [FromQuery] BillingSheetStatus? status = null)
    {
        var result = await repository.GetItemBillingSheets(page, pageSize, searchQuery, status);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Updates a specific item billing sheet by its ID.
    /// </summary>
    [HttpPut("billing-sheet/{billingSheetId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> UpdateItemBillingSheet([FromBody] UpdateItemBillingSheetRequest request, [FromRoute] Guid billingSheetId)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.UpdateItemBillingSheet(request, billingSheetId, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Adds charges to an item billing sheet.
    /// </summary>
    [HttpPut("billing-sheet/{billingSheetId:guid}/charges")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> AddChargesToItemBillingSheet([FromBody] List<CreateBillingSheetCharge> request, [FromRoute] Guid billingSheetId)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.AddChargesToItemBillingSheet(request, billingSheetId, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Marks charges within an item billing sheet as paid.
    /// </summary>
    [HttpPut("billing-sheet/charges/paid")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> MarkItemBillingSheetChargeAsPaid([FromBody] MarkBillingSheetCharge request)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.MarkItemBillingSheetChargeAsPaid(request, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Deletes a specific item billing sheet by its ID.
    /// </summary>
    [HttpDelete("billing-sheet/{billingSheetId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> DeleteItemBillingSheet([FromRoute] Guid billingSheetId)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.DeleteItemBillingSheet(billingSheetId, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
}