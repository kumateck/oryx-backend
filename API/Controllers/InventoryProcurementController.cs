using APP.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using APP.IRepository;
using DOMAIN.Entities.Items.Requisitions;
using APP.Utils;
using DOMAIN.Entities.Memos;
using DOMAIN.Entities.StockEntries;
using DOMAIN.Entities.VendorQuotations;

namespace API.Controllers;

[Route("api/v{version:apiVersion}/procurement/inventory")]
[ApiController]
public class InventoryProcurementController(IInventoryProcurementRepository repository) : ControllerBase
{
    #region Inventory Purchase Requisition Management
    
    /// <summary>
    /// Creates a new Inventory Purchase Requisition.
    /// </summary>
    /// <param name="request">The CreateInventoryPurchaseRequisition object containing requisition details.</param>
    /// <returns>Returns the ID of the newly created requisition.</returns>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> CreateInventoryPurchaseRequisition([FromBody] CreateInventoryPurchaseRequisition request)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();
        
        var result = await repository.CreateInventoryPurchaseRequisition(request, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Updates an existing Inventory Purchase Requisition.
    /// </summary>
    /// <param name="id">The ID of the requisition to update.</param>
    /// <param name="request">The CreateInventoryPurchaseRequisition object with updated details.</param>
    /// <returns>Returns a success or failure result.</returns>
    [HttpPut("{id}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> UpdateInventoryPurchaseRequisition(Guid id, [FromBody] CreateInventoryPurchaseRequisition request)
    {
        var result = await repository.UpdateInventoryPurchaseRequisition(id, request);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Deletes an Inventory Purchase Requisition by its ID.
    /// </summary>
    /// <param name="id">The ID of the requisition to delete.</param>
    /// <returns>Returns a success or failure result.</returns>
    [HttpDelete("{id}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> DeleteInventoryPurchaseRequisition(Guid id)
    {
        var result = await repository.DeleteInventoryPurchaseRequisition(id);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a specific Inventory Purchase Requisition by its ID.
    /// </summary>
    /// <param name="id">The ID of the requisition to retrieve.</param>
    /// <returns>Returns the Inventory Purchase Requisition.</returns>
    [HttpGet("{id}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(InventoryPurchaseRequisitionDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetInventoryPurchaseRequisition(Guid id)
    {
        var result = await repository.GetInventoryPurchaseRequisition(id);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
    
    /// <summary>
    /// Retrieves a paginated list of Inventory Purchase Requisitions.
    /// </summary>
    /// <param name="page">The current page number.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="searchQuery">Search query for filtering requisitions by code.</param>
    /// <returns>Returns a paginated list of Inventory Purchase Requisitions.</returns>
    [HttpGet]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<InventoryPurchaseRequisitionDto>>))]
    public async Task<IResult> GetInventoryPurchaseRequisitions([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string searchQuery = null)
    {
        var result = await repository.GetInventoryPurchaseRequisitions(page, pageSize, searchQuery);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    #endregion
    
    
    #region Sourcing Logic
    
    /// <summary>
    /// Creates a new Source Requisition for items from a purchase requisition, grouping them by vendor.
    /// </summary>
    /// <param name="request">The CreateSourceInventoryRequisition object containing the requisition ID and items.</param>
    /// <returns>Returns a success or failure result.</returns>
    [HttpPost("source")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> CreateSourceRequisition([FromBody] CreateSourceInventoryRequisition request)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();
        
        var result = await repository.CreateSourceRequisition(request, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Creates a new Market Requisition for a specific item that needs to be sourced from the open market.
    /// </summary>
    /// <param name="request">The CreateMarketRequisition object containing item and requisition details.</param>
    /// <returns>Returns a success or failure result.</returns>
    [HttpPost("market")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> CreateMarketRequisition([FromBody] CreateMarketRequisition request)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();
        
        var result = await repository.CreateMarketRequisition(request, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
    
    /// <summary>
    /// Retrieves a paginated list of Market Requisitions.
    /// </summary>
    /// <param name="page">The current page number.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <returns>Returns a paginated list of market requisitions.</returns>
    [HttpGet("market")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<MarketRequisitionDto>>))]
    public async Task<IResult> GetMarketRequisitions([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var result = await repository.GetMarketRequisitions(page, pageSize);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
    
    /// <summary>
    /// Retrieves a list of vendor price comparisons for items based on the sourcing method.
    /// </summary>
    /// <param name="source">The source of the requisition (TrustedVendor or OpenMarket).</param>
    /// <returns>Returns a list of price comparisons.</returns>
    [HttpGet("price-comparison")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<VendorPriceComparison>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> GetPriceComparisonOfItem([FromQuery] InventoryRequisitionSource source)
    {
        var result = await repository.GetPriceComparisonOfItem(source);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    #endregion
    

    #region Memo Creation Logic
    
    /// <summary>
    /// Processes Open Market requisitions and creates memos for them.
    /// </summary>
    /// <param name="memos">A list of ProcessMemo objects to be processed.</param>
    /// <returns>Returns a success or failure result.</returns>
    [HttpPost("memo/open-market")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> ProcessOpenMarketMemo([FromBody] List<CreateMemoItem> memos)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.ProcessOpenMarketMemo(memos, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Processes trusted vendor quotations and creates memos.
    /// </summary>
    /// <param name="memos">A list of ProcessMemo objects to be processed.</param>
    /// <returns>Returns a success or failure result.</returns>
    [HttpPost("memo/trusted-vendor")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> ProcessTrustedVendorQuotationAndCreateMemo([FromBody] List<CreateMemoItem> memos)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();
        
        var result = await repository.ProcessTrustedVendorMemo(memos, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    #endregion


    #region Trusted Vendor Specific
    
    /// <summary>
    /// Sends a quotation request to a specified trusted vendor.
    /// </summary>
    /// <param name="vendorId">The ID of the vendor to send the quotation request to.</param>
    /// <returns>Returns a success or failure result.</returns>
    [HttpPost("vendor/{vendorId}/send-quotation")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> SendQuotationToVendor(Guid vendorId)
    {
        var result = await repository.SendQuotationToVendor(vendorId);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
    
    /// <summary>
    /// Retrieves a paginated list of vendor quotations.
    /// </summary>
    /// <param name="page">The current page number.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="received">A boolean to filter by received or unreceived quotations.</param>
    /// <returns>Returns a paginated list of vendor quotations.</returns>
    [HttpGet("vendor/quotations")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<VendorQuotationDto>>))]
    public async Task<IResult> GetVendorQuotations([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] bool received = false)
    {
        var result = await repository.GetVendorQuotations(page, pageSize, received);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a single vendor quotation by its ID.
    /// </summary>
    /// <param name="vendorQuotationId">The ID of the vendor quotation.</param>
    /// <returns>Returns the vendor quotation.</returns>
    [HttpGet("vendor/quotation/{vendorQuotationId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(VendorQuotationDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetVendorQuotation(Guid vendorQuotationId)
    {
        var result = await repository.GetVendorQuotation(vendorQuotationId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Receives a quotation from a trusted vendor.
    /// </summary>
    /// <param name="vendorQuotationId">The ID of the vendor quotation to receive.</param>
    /// <param name="vendorQuotationResponse">A list of items and their quoted prices.</param>
    /// <returns>Returns a success or failure result.</returns>
    [HttpPost("vendor/quotation/{vendorQuotationId}/receive")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> ReceiveQuotationFromVendor([FromBody] List<VendorQuotationResponseDto> vendorQuotationResponse, Guid vendorQuotationId)
    {
        var result = await repository.ReceiveQuotationFromVendor(vendorQuotationResponse, vendorQuotationId);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    #endregion


    #region Open Market Specific
    
    /// <summary>
    /// Retrieves a paginated list of vendors for open market requisitions.
    /// </summary>
    /// <param name="page">The current page number.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="complete">A boolean to filter by completed or pending open market requisitions.</param>
    /// <returns>Returns a paginated list of open market requisition vendors.</returns>
    [HttpGet("market/vendors")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<MarketRequisitionVendorDto>>))]
    public async Task<IResult> GetMarketRequisitionVendors([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] bool complete = false)
    {
        var result = await repository.GetMarketRequisitionVendors(page, pageSize, complete);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Creates a new vendor for an open market requisition.
    /// </summary>
    /// <param name="request">The CreateMarketRequisitionVendor object containing vendor details.</param>
    /// <returns>Returns a success or failure result.</returns>
    [HttpPost("market/vendors")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> CreateMarketRequisitionVendor([FromBody] CreateMarketRequisitionVendor request)
    {
        var result = await repository.CreateMarketRequisitionVendor(request);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
    
    /// <summary>
    /// Confirms a vendor for an open market requisition.
    /// </summary>
    /// <param name="marketRequisitionVendorId">The ID of the vendor to confirm.</param>
    /// <returns>Returns a success or failure result.</returns>
    [HttpPost("market/vendors/{marketRequisitionVendorId}/confirm")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> ConfirmMarketRequisitionVendor(Guid marketRequisitionVendorId)
    {
        var result = await repository.ConfirmMarketRequisitionVendor(marketRequisitionVendorId);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
    
    #endregion

    #region Memos

    /// <summary>
    /// Retrieves a paginated list of memos with their details.
    /// </summary>
    /// <param name="page">The current page number.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="searchQuery">Optional search query to filter by memo code.</param>
    /// <returns>Returns a paginated list of memos.</returns>
    [HttpGet("memo")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<MemoDto>>))]
    public async Task<IResult> GetMemos([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string searchQuery = null)
    {
        var result = await repository.GetMemos(page, pageSize, searchQuery);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
    
    /// <summary>
    /// Retrieves a specific memo by its ID.
    /// </summary>
    /// <param name="id">The ID of the memo.</param>
    /// <returns>Returns the memo details if found.</returns>
    [HttpGet("memo/{id}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(MemoDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetMemo(Guid id)
    {
        var result = await repository.GetMemo(id);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
    
    /// <summary>
    /// Marks a memo item as paid.
    /// </summary>
    /// <param name="id">The ID of the memo item.</param>
    /// <returns>200 OK if successful, 404 or 400 otherwise.</returns>
    [HttpPost("memo-item/{id}/mark-paid")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> MarkMemoItemAsPaid(Guid id)
    {
        var result = await repository.MarkMemoItemAsPaid(id);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    #endregion
    
    [HttpGet("purchased-items")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<StockEntryDto>))]
    public async Task<IResult> GetStockEntries()
    {
        var result = await repository.GetStockEntries();
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPost("items/{id:guid}/approve")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IResult> ApproveItem([FromRoute] Guid id)
    {
        var result = await repository.ApproveItem(id);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
    
    [HttpPost("items/{id:guid}/reject")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IResult> RejectItem([FromRoute] Guid id)
    {
        var result = await repository.RejectItem(id);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    #region Helper Methods
    
    /// <summary>
    /// Generates a unique memo code.
    /// </summary>
    /// <returns>Returns the generated memo code.</returns>
    [HttpGet("memo/code/generate")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(string))]
    public async Task<IResult> GenerateMemoCode()
    {
        return TypedResults.Ok(await repository.GenerateMemoCode());
    }

    #endregion
}