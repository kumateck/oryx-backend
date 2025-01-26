using APP.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using APP.IRepository;
using DOMAIN.Entities.Requisitions;
using APP.Utils;
using DOMAIN.Entities.Procurement.Suppliers;
using DOMAIN.Entities.Requisitions.Request;

namespace API.Controllers;

[Route("api/v{version:apiVersion}/requisition")]
[ApiController]
public class RequisitionController(IRequisitionRepository repository) : ControllerBase
{
    #region Requisition Management

    /// <summary>
    /// Creates a new Stock Requisition.
    /// </summary>
    /// <param name="request">The CreateRequisitionRequest object.</param>
    /// <returns>Returns the ID of the created Stock Requisition.</returns>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> CreateRequisition([FromBody] CreateRequisitionRequest request)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();
        
        var result = await repository.CreateRequisition(request, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a paginated list of requisitions.
    /// </summary>
    /// <param name="page">The current page number.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="searchQuery">Search query for filtering results.</param>
    /// <param name="status">Filter by status of the requisition.</param>
    /// <param name="type">Filter between stock and purchase requisitions. (Stock = 0, Purchase =  1)</param>
    /// <returns>Returns a paginated list of requisitions.</returns>
    [HttpGet]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<RequisitionDto>>))]
    public async Task<IResult> GetRequisitions([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string searchQuery = null,
        [FromQuery] RequestStatus? status = null,  [FromQuery] RequisitionType? type = null)
    {
        var result = await repository.GetRequisitions(page, pageSize, searchQuery, status, type);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a specific Stock Requisition by its ID.
    /// </summary>
    /// <param name="requisitionId">The ID of the Stock Requisition.</param>
    /// <returns>Returns the Stock Requisition.</returns>
    [HttpGet("{requisitionId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(RequisitionDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetRequisition(Guid requisitionId)
    {
        var result = await repository.GetRequisition(requisitionId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Approves a Stock Requisition.
    /// </summary>
    /// <param name="request">The ApproveRequisitionRequest object.</param>
    /// <param name="requisitionId">The ID of the Stock Requisition being approved.</param>
    /// <returns>Returns a success or failure result.</returns>
    [HttpPost("{requisitionId}/approve")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> ApproveRequisition([FromBody] ApproveRequisitionRequest request, Guid requisitionId)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();
        
        var roleIds = (List<Guid>)HttpContext.Items["Roles"]; 

        var result = await repository.ApproveRequisition(request, requisitionId, Guid.Parse(userId), roleIds);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Processes a Stock Requisition.
    /// </summary>
    /// <param name="request">The CreateRequisitionRequest object.</param>
    /// <param name="requisitionId">The ID of the Stock Requisition being processed.</param>
    /// <returns>Returns a success or failure result.</returns>
    [HttpPost("{requisitionId}/process")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> ProcessRequisition([FromBody] CreateRequisitionRequest request, Guid requisitionId)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.ProcessRequisition(request, requisitionId, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    #endregion

    #region Source Requisition Management

    /// <summary>
    /// Creates a new Source Requisition.
    /// </summary>
    /// <param name="request">The CreateSourceRequisitionRequest object.</param>
    /// <returns>Returns the ID of the created Source Requisition.</returns>
    [HttpPost("source")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> CreateSourceRequisition([FromBody] CreateSourceRequisitionRequest request)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.CreateSourceRequisition(request, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a specific Source Requisition by its ID.
    /// </summary>
    /// <param name="sourceRequisitionId">The ID of the Source Requisition.</param>
    /// <returns>Returns the Source Requisition.</returns>
    [HttpGet("source/{sourceRequisitionId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(SourceRequisitionDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetSourceRequisition(Guid sourceRequisitionId)
    {
        var result = await repository.GetSourceRequisition(sourceRequisitionId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a paginated list of Source Requisitions.
    /// </summary>
    /// <param name="page">The current page number.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="searchQuery">Search query for filtering results.</param>
    /// <returns>Returns a paginated list of Source Requisitions.</returns>
    [HttpGet("source")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<SourceRequisitionDto>>))]
    public async Task<IResult> GetSourceRequisitions([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string searchQuery = null)
    {
        var result = await repository.GetSourceRequisitions(page, pageSize, searchQuery);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a paginated list of Source Requisition Items based on Procurement Source.
    /// </summary>
    /// <param name="page">The current page number.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="source">The procurement source of the material(e.g., Local, Foreign, Internal).</param>
    /// <returns>Returns a paginated list of Source Requisition Items.</returns>
    [HttpGet("source/items")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<SourceRequisitionItemDto>>))]
    public async Task<IResult> GetSourceRequisitionItems([FromQuery] ProcurementSource source,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var result = await repository.GetSourceRequisitionItems(page, pageSize, source);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Updates a Source Requisition.
    /// </summary>
    /// <param name="request">The CreateSourceRequisitionRequest object.</param>
    /// <param name="sourceRequisitionId">The ID of the Source Requisition to update.</param>
    /// <returns>Returns a success or failure result.</returns>
    [HttpPut("source/{sourceRequisitionId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> UpdateSourceRequisition([FromBody] CreateSourceRequisitionRequest request, Guid sourceRequisitionId)
    {
        var result = await repository.UpdateSourceRequisition(request, sourceRequisitionId);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Deletes a Source Requisition.
    /// </summary>
    /// <param name="sourceRequisitionId">The ID of the Source Requisition to delete.</param>
    /// <returns>Returns a success or failure result.</returns>
    [HttpDelete("source/{sourceRequisitionId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> DeleteSourceRequisition(Guid sourceRequisitionId)
    {
        var result = await repository.DeleteSourceRequisition(sourceRequisitionId);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a paginated list of suppliers with their associated source requisition items.
    /// </summary>
    /// <param name="source">The source of the requisition. (example Local, Foreign, Internal)</param>
    /// <param name="page">The current page number.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="sent">Filter by whether a quotation has been sent.</param>
    /// <returns>Returns a paginated list of suppliers with their requisition items.</returns>
    [HttpGet("source/supplier")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<SupplierQuotationDto>>))]
    public async Task<IResult> GetSuppliersWithSourceRequisitionItems([FromQuery] ProcurementSource source,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] bool sent = false)
    {
        var result = await repository.GetSuppliersWithSourceRequisitionItems(page, pageSize, source, sent);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
    
    /// <summary>
    /// Retrieves a supplier with their associated source requisition items.
    /// </summary>
    /// <param name="supplierId">The id of the supplier with associated requisition items.</param>
    /// <returns>Returns a supplier with their requisition items.</returns>
    [HttpGet("source/supplier/{supplierId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(SupplierQuotationDto))]
    public async Task<IResult> GetSuppliersWithSourceRequisitionItems(Guid supplierId)
    {
        var result = await repository.GetSuppliersWithSourceRequisitionItems(supplierId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Send Quotation request email to supplier.
    /// </summary>
    /// <param name="supplierId">The ID of the supplier.</param>
    /// <returns>Returns a success or failure result.</returns>
    [HttpPost("source/supplier/{supplierId}/send-quotation")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> MarkQuotationAsSent(Guid supplierId)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();
        
        var result = await repository.SendQuotationToSupplier(supplierId);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    #endregion
    
    #region Supplier Quotation Management

    /// <summary>
    /// Retrieves a paginated list of suppliers with their associated source requisition items for quotation.
    /// </summary>
    /// <param name="page">The current page number.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="supplierType">The type of the supplier (example Foreign, Local).</param>
    /// <param name="received">Filter by whether the quotation has been received.</param>
    /// <returns>Returns a paginated list of suppliers with their requisition items for quotation.</returns>
    [HttpGet("source/supplier/quotation")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<SupplierQuotationDto>>))]
    public async Task<IResult> GetSuppliersWithSourceRequisitionItemsForQuotation([FromQuery] SupplierType supplierType,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] bool received = false)
    {
        var result = await repository.GetSupplierQuotations(page, pageSize, supplierType, received);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a supplier with their associated source requisition items for quotation.
    /// </summary>
    /// <param name="supplierQuotationId">The ID of the supplier quotation.</param>
    /// <returns>Returns a supplier with their requisition items for quotation.</returns>
    [HttpGet("source/supplier/{supplierQuotationId}/quotation")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(SupplierQuotationDto))]
    public async Task<IResult> GetSuppliersWithSourceRequisitionItemsForQuotation(Guid supplierQuotationId)
    {
        var result = await repository.GetSupplierQuotation(supplierQuotationId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Receives quotations from a supplier.
    /// </summary>
    /// <param name="supplierQuotationResponse">The list of quotations received from the supplier.</param>
    /// <param name="supplierQuotationId">The ID of the supplier quotation.</param>
    /// <returns>Returns a success or failure result.</returns>
    [HttpPost("source/supplier/{supplierQuotationId}/quotation/receive")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> ReceiveQuotationFromSupplier(
        [FromBody] List<SupplierQuotationResponseDto> supplierQuotationResponse,
        Guid supplierQuotationId)
    {
        var result = await repository.ReceiveQuotationFromSupplier(supplierQuotationResponse, supplierQuotationId);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a price comparison of materials for a procurement source.
    /// </summary>
    /// <param name="supplierType">The type of the supplier (example Local, Foreign).</param>
    /// <returns>Returns a list of price comparisons.</returns>
    [HttpGet("source/material/price-comparison")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<SupplierPriceComparison>))]
    public async Task<IResult> GetPriceComparisonOfMaterial([FromQuery] SupplierType supplierType)
    {
        var result = await repository.GetPriceComparisonOfMaterial(supplierType);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Processes quotations and creates purchase orders.
    /// </summary>
    /// <param name="processQuotations">The list of quotations to process.</param>
    /// <returns>Returns a success or failure result.</returns>
    [HttpPost("source/quotation/process-purchase-order")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> ProcessQuotationAndCreatePurchaseOrder(
        [FromBody] List<ProcessQuotation> processQuotations)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();
        
        var result = await repository.ProcessQuotationAndCreatePurchaseOrder(processQuotations, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    #endregion

}
