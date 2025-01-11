using APP.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using APP.IRepository;
using APP.Utils;
using DOMAIN.Entities.Procurement.Manufacturers;
using DOMAIN.Entities.Procurement.Suppliers;
using DOMAIN.Entities.PurchaseOrders;
using DOMAIN.Entities.PurchaseOrders.Request;
using DOMAIN.Entities.Shipments;
using DOMAIN.Entities.Shipments.Request;

namespace API.Controllers;

[Route("api/v{version:apiVersion}/procurement")]
[ApiController]
public class ProcurementController(IProcurementRepository repository) : ControllerBase
{
    // ************* Manufacturer Endpoints *************

    /// <summary>
    /// Creates a new manufacturer.
    /// </summary>
    /// <param name="request">The CreateManufacturerRequest object.</param>
    /// <returns>Returns the ID of the created manufacturer.</returns>
    [HttpPost("manufacturer")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> CreateManufacturer([FromBody] CreateManufacturerRequest request)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.CreateManufacturer(request, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a manufacturer by its ID.
    /// </summary>
    /// <param name="manufacturerId">The ID of the manufacturer.</param>
    /// <returns>Returns the manufacturer details.</returns>
    [HttpGet("manufacturer/{manufacturerId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ManufacturerDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetManufacturer(Guid manufacturerId)
    {
        var result = await repository.GetManufacturer(manufacturerId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a paginated list of manufacturers.
    /// </summary>
    /// <param name="page">The current page number.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="searchQuery">Search query for filtering results.</param>
    /// <returns>Returns a paginated list of manufacturers.</returns>
    [HttpGet("manufacturer")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<ManufacturerDto>>))]
    public async Task<IResult> GetManufacturers([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string searchQuery = null)
    {
        var result = await repository.GetManufacturers(page, pageSize, searchQuery);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
    
    /// <summary>
    /// Retrieves a list of manufacturers by their material ID.
    /// </summary>
    /// <param name="materialId">The ID of the material.</param>
    /// <returns>Returns the supplier details.</returns>
    [HttpGet("manufacturer/material/{materialId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<ManufacturerDto>))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetManufacturerByMaterial(Guid materialId)
    {
        var result = await repository.GetManufacturersByMaterial(materialId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Updates a specific manufacturer by its ID.
    /// </summary>
    /// <param name="request">The CreateManufacturerRequest object.</param>
    /// <param name="manufacturerId">The ID of the manufacturer to update.</param>
    /// <returns>Returns success or failure.</returns>
    [HttpPut("manufacturer/{manufacturerId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> UpdateManufacturer([FromBody] CreateManufacturerRequest request, Guid manufacturerId)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.UpdateManufacturer(request, manufacturerId, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Deletes a specific manufacturer by its ID.
    /// </summary>
    /// <param name="manufacturerId">The ID of the manufacturer to delete.</param>
    /// <returns>Returns success or failure.</returns>
    [HttpDelete("manufacturer/{manufacturerId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> DeleteManufacturer(Guid manufacturerId)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.DeleteManufacturer(manufacturerId, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    // ************* Supplier Endpoints *************

    /// <summary>
    /// Creates a new supplier.
    /// </summary>
    /// <param name="request">The CreateSupplierRequest object.</param>
    /// <returns>Returns the ID of the created supplier.</returns>
    [HttpPost("supplier")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> CreateSupplier([FromBody] CreateSupplierRequest request)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.CreateSupplier(request, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a supplier by its ID.
    /// </summary>
    /// <param name="supplierId">The ID of the supplier.</param>
    /// <returns>Returns the supplier details.</returns>
    [HttpGet("supplier/{supplierId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(SupplierDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetSupplier(Guid supplierId)
    {
        var result = await repository.GetSupplier(supplierId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a paginated list of suppliers.
    /// </summary>
    /// <param name="page">The current page number.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="searchQuery">Search query for filtering results.</param>
    /// <returns>Returns a paginated list of suppliers.</returns>
    [HttpGet("supplier")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<SupplierDto>>))]
    public async Task<IResult> GetSuppliers([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string searchQuery = null)
    {
        var result = await repository.GetSuppliers(page, pageSize, searchQuery);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
    
    /// <summary>
    /// Retrieves a list of suppliers by their material ID.
    /// </summary>
    /// <param name="materialId">The ID of the material.</param>
    /// <returns>Returns the supplier details.</returns>
    [HttpGet("supplier/material/{materialId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<SupplierDto>))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetSupplierByMaterial(Guid materialId)
    {
        var result = await repository.GetSupplierByMaterial(materialId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
    
    /// <summary>
    /// Retrieves a list of suppliers by their material ID.
    /// </summary>
    /// <param name="materialId">The ID of the material.</param>
    /// <param name="type">The type of the supplier.</param>
    /// <returns>Returns the supplier details.</returns>
    [HttpGet("supplier/{materialId}/{type}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<SupplierDto>))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetSupplierByMaterialAndType(Guid materialId, SupplierType type)
    {
        var result = await repository.GetSupplierByMaterialAndType(materialId, type);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Updates a specific supplier by its ID.
    /// </summary>
    /// <param name="request">The CreateSupplierRequest object.</param>
    /// <param name="supplierId">The ID of the supplier to update.</param>
    /// <returns>Returns success or failure.</returns>
    [HttpPut("supplier/{supplierId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> UpdateSupplier([FromBody] CreateSupplierRequest request, Guid supplierId)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.UpdateSupplier(request, supplierId, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Deletes a specific supplier by its ID.
    /// </summary>
    /// <param name="supplierId">The ID of the supplier to delete.</param>
    /// <returns>Returns success or failure.</returns>
    [HttpDelete("supplier/{supplierId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> DeleteSupplier(Guid supplierId)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.DeleteSupplier(supplierId, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
    
     // ************* PurchaseOrder Endpoints *************

    /// <summary>
    /// Creates a new purchase order.
    /// </summary>
    /// <param name="request">The CreatePurchaseOrderRequest object.</param>
    /// <returns>Returns the ID of the created purchase order.</returns>
    [HttpPost("purchase-order")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> CreatePurchaseOrder([FromBody] CreatePurchaseOrderRequest request)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.CreatePurchaseOrder(request, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a purchase order by its ID.
    /// </summary>
    /// <param name="purchaseOrderId">The ID of the purchase order.</param>
    /// <returns>Returns the purchase order details.</returns>
    [HttpGet("purchase-order/{purchaseOrderId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PurchaseOrderDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetPurchaseOrder(Guid purchaseOrderId)
    {
        var result = await repository.GetPurchaseOrder(purchaseOrderId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a paginated list of purchase orders.
    /// </summary>
    /// <param name="page">The current page number.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="searchQuery">Search query for filtering results.</param>
    /// <param name="status">Filter by the status of the purchase order. (Pending, Delivered, Completed)</param>
    /// <returns>Returns a paginated list of purchase orders.</returns>
    [HttpGet("purchase-order")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<PurchaseOrderDto>>))]
    public async Task<IResult> GetPurchaseOrders([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string searchQuery = null, [FromQuery] PurchaseOrderStatus? status = null)
    {
        var result = await repository.GetPurchaseOrders(page, pageSize, searchQuery, status);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
    
    /// <summary>
    /// Sends a purchase order or awarded quote to a supplier
    /// </summary>
    /// <param name="request">The request metadata to send purchase orders to suppliers.</param>
    /// <param name="purchaseOrderId">The ID of the purchase order you want to send to a supplier as an email.</param>
    /// <returns>Returns a 204 no content response</returns>
    [HttpPost("purchase-order/{purchaseOrderId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> SendPurchaseOrderToSupplier([FromBody] SendPurchaseOrderRequest request, Guid purchaseOrderId)
    {
        var result = await repository.SendPurchaseOrderToSupplier(request, purchaseOrderId);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
    
    /// <summary>
    /// Sends a proforma-invoice to a supplier
    /// </summary>
    /// <param name="purchaseOrderId">The ID of the purchase order you want to send to a supplier as an email.</param>
    /// <returns>Returns a 204 no content response</returns>
    [HttpPost("purchase-order/proforma-invoice/{purchaseOrderId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> SendProformaInvoiceToSupplier(Guid purchaseOrderId)
    {
        var result = await repository.SendProformaInvoiceToSupplier(purchaseOrderId);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Updates a specific purchase order by its ID.
    /// </summary>
    /// <param name="request">The UpdatePurchaseOrderRequest object.</param>
    /// <param name="purchaseOrderId">The ID of the purchase order to update.</param>
    /// <returns>Returns success or failure.</returns>
    [HttpPut("purchase-order/{purchaseOrderId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> UpdatePurchaseOrder([FromBody] CreatePurchaseOrderRequest request, Guid purchaseOrderId)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.UpdatePurchaseOrder(request, purchaseOrderId, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Deletes a specific purchase order by its ID.
    /// </summary>
    /// <param name="purchaseOrderId">The ID of the purchase order to delete.</param>
    /// <returns>Returns success or failure.</returns>
    [HttpDelete("purchase-order/{purchaseOrderId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> DeletePurchaseOrder(Guid purchaseOrderId)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.DeletePurchaseOrder(purchaseOrderId, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    // ************* PurchaseOrderInvoice Endpoints *************

    /// <summary>
    /// Creates a new purchase order invoice.
    /// </summary>
    /// <param name="request">The CreatePurchaseOrderInvoiceRequest object.</param>
    /// <returns>Returns the ID of the created invoice.</returns>
    [HttpPost("purchase-order-invoice")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> CreatePurchaseOrderInvoice([FromBody] CreatePurchaseOrderInvoiceRequest request)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.CreatePurchaseOrderInvoice(request, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a purchase order invoice by its ID.
    /// </summary>
    /// <param name="invoiceId">The ID of the invoice.</param>
    /// <returns>Returns the invoice details.</returns>
    [HttpGet("purchase-order-invoice/{invoiceId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PurchaseOrderInvoiceDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetPurchaseOrderInvoice(Guid invoiceId)
    {
        var result = await repository.GetPurchaseOrderInvoice(invoiceId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a paginated list of purchase order invoices.
    /// </summary>
    /// <param name="page">The current page number.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="searchQuery">Search query for filtering results.</param>
    /// <returns>Returns a paginated list of invoices.</returns>
    [HttpGet("purchase-order-invoice")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<PurchaseOrderInvoiceDto>>))]
    public async Task<IResult> GetPurchaseOrderInvoices([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string searchQuery = null)
    {
        var result = await repository.GetPurchaseOrderInvoices(page, pageSize, searchQuery);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Updates a specific purchase order invoice by its ID.
    /// </summary>
    /// <param name="request">The UpdatePurchaseOrderInvoiceRequest object.</param>
    /// <param name="invoiceId">The ID of the invoice to update.</param>
    /// <returns>Returns success or failure.</returns>
    [HttpPut("purchase-order-invoice/{invoiceId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> UpdatePurchaseOrderInvoice([FromBody] CreatePurchaseOrderInvoiceRequest request, Guid invoiceId)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.UpdatePurchaseOrderInvoice(request, invoiceId, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Deletes a specific purchase order invoice by its ID.
    /// </summary>
    /// <param name="invoiceId">The ID of the invoice to delete.</param>
    /// <returns>Returns success or failure.</returns>
    [HttpDelete("purchase-order-invoice/{invoiceId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> DeletePurchaseOrderInvoice(Guid invoiceId)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.DeletePurchaseOrderInvoice(invoiceId, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    // ************* BillingSheet Endpoints *************

    /// <summary>
    /// Creates a new billing sheet.
    /// </summary>
    /// <param name="request">The CreateBillingSheetRequest object.</param>
    /// <returns>Returns the ID of the created billing sheet.</returns>
    [HttpPost("billing-sheet")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> CreateBillingSheet([FromBody] CreateBillingSheetRequest request)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.CreateBillingSheet(request, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a billing sheet by its ID.
    /// </summary>
    /// <param name="billingSheetId">The ID of the billing sheet.</param>
    /// <returns>Returns the billing sheet details.</returns>
    [HttpGet("billing-sheet/{billingSheetId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(BillingSheetDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetBillingSheet(Guid billingSheetId)
    {
        var result = await repository.GetBillingSheet(billingSheetId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a paginated list of billing sheets.
    /// </summary>
    /// <param name="page">The current page number.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="searchQuery">Search query for filtering results.</param>
    /// <returns>Returns a paginated list of billing sheets.</returns>
    [HttpGet("billing-sheet")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<BillingSheetDto>>))]
    public async Task<IResult> GetBillingSheets([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string searchQuery = null)
    {
        var result = await repository.GetBillingSheets(page, pageSize, searchQuery);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Updates a specific billing sheet by its ID.
    /// </summary>
    /// <param name="request">The UpdateBillingSheetRequest object.</param>
    /// <param name="billingSheetId">The ID of the billing sheet to update.</param>
    /// <returns>Returns success or failure.</returns>
    [HttpPut("billing-sheet/{billingSheetId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> UpdateBillingSheet([FromBody] CreateBillingSheetRequest request, Guid billingSheetId)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.UpdateBillingSheet(request, billingSheetId, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Deletes a specific billing sheet by its ID.
    /// </summary>
    /// <param name="billingSheetId">The ID of the billing sheet to delete.</param>
    /// <returns>Returns success or failure.</returns>
    [HttpDelete("billing-sheet/{billingSheetId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> DeleteBillingSheet(Guid billingSheetId)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.DeleteBillingSheet(billingSheetId, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
    
    /// <summary>
    /// Creates a new shipment document.
    /// </summary>
    /// <param name="request">The CreateShipmentDocumentRequest object.</param>
    /// <returns>Returns the ID of the created shipment document.</returns>
    [HttpPost("shipment-document")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> CreateShipmentDocument([FromBody] CreateShipmentDocumentRequest request)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.CreateShipmentDocument(request, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a shipment document by its ID.
    /// </summary>
    /// <param name="shipmentDocumentId">The ID of the shipment document.</param>
    /// <returns>Returns the shipment document details.</returns>
    [HttpGet("shipment-document/{shipmentDocumentId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ShipmentDocumentDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetShipmentDocument(Guid shipmentDocumentId)
    {
        var result = await repository.GetShipmentDocument(shipmentDocumentId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a paginated list of shipment documents.
    /// </summary>
    /// <param name="page">The current page number.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="searchQuery">Search query for filtering results.</param>
    /// <returns>Returns a paginated list of shipment documents.</returns>
    [HttpGet("shipment-document")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<ShipmentDocumentDto>>))]
    public async Task<IResult> GetShipmentDocuments([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string searchQuery = null)
    {
        var result = await repository.GetShipmentDocuments(page, pageSize, searchQuery);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Updates a specific shipment document by its ID.
    /// </summary>
    /// <param name="request">The CreateShipmentDocumentRequest object.</param>
    /// <param name="shipmentDocumentId">The ID of the shipment document to update.</param>
    /// <returns>Returns success or failure.</returns>
    [HttpPut("shipment-document/{shipmentDocumentId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> UpdateShipmentDocument([FromBody] CreateShipmentDocumentRequest request, Guid shipmentDocumentId)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.UpdateShipmentDocument(request, shipmentDocumentId, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Deletes a specific shipment document by its ID.
    /// </summary>
    /// <param name="shipmentDocumentId">The ID of the shipment document to delete.</param>
    /// <returns>Returns success or failure.</returns>
    [HttpDelete("shipment-document/{shipmentDocumentId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> DeleteShipmentDocument(Guid shipmentDocumentId)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.DeleteShipmentDocument(shipmentDocumentId, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
    
    /// <summary>
    /// Creates a new shipment invoice.
    /// </summary>
    [HttpPost("shipment-invoice")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> CreateShipmentInvoice([FromBody] CreateShipmentInvoice request)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.CreateShipmentInvoice(request, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a shipment invoice by its ID.
    /// </summary>
    [HttpGet("shipment-invoice/{shipmentInvoiceId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ShipmentInvoiceDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetShipmentInvoice(Guid shipmentInvoiceId)
    {
        var result = await repository.GetShipmentInvoice(shipmentInvoiceId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Updates a specific shipment invoice by its ID.
    /// </summary>
    [HttpPut("shipment-invoice/{shipmentInvoiceId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> UpdateShipmentInvoice([FromBody] CreateShipmentInvoice request, Guid shipmentInvoiceId)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.UpdateShipmentInvoice(request, shipmentInvoiceId, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Deletes a specific shipment invoice by its ID.
    /// </summary>
    [HttpDelete("shipment-invoice/{shipmentInvoiceId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> DeleteShipmentInvoice(Guid shipmentInvoiceId)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.DeleteShipmentInvoice(shipmentInvoiceId, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Creates a new shipment discrepancy.
    /// </summary>
    [HttpPost("shipment-discrepancy")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> CreateShipmentDiscrepancy([FromBody] CreateShipmentDiscrepancy request)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.CreateShipmentDiscrepancy(request, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a shipment discrepancy by its ID.
    /// </summary>
    [HttpGet("shipment-discrepancy/{shipmentDiscrepancyId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ShipmentDiscrepancyDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetShipmentDiscrepancy(Guid shipmentDiscrepancyId)
    {
        var result = await repository.GetShipmentDiscrepancy(shipmentDiscrepancyId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Updates a specific shipment discrepancy by its ID.
    /// </summary>
    [HttpPut("shipment-discrepancy/{shipmentDiscrepancyId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> UpdateShipmentDiscrepancy([FromBody] CreateShipmentDiscrepancy request, Guid shipmentDiscrepancyId)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.UpdateShipmentDiscrepancy(request, shipmentDiscrepancyId, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Deletes a specific shipment discrepancy by its ID.
    /// </summary>
    [HttpDelete("shipment-discrepancy/{shipmentDiscrepancyId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> DeleteShipmentDiscrepancy(Guid shipmentDiscrepancyId)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.DeleteShipmentDiscrepancy(shipmentDiscrepancyId, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
}
