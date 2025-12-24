using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using DOMAIN.Entities.JobRequests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

/// <summary>
/// Service Proforma Invoice Management API
/// </summary>
/// <remarks>
/// Manages proforma invoice requests and responses in the external service workflow. After selecting 
/// a quotation, a proforma invoice is requested from the contractor before creating the formal service memo.
/// 
/// **Proforma Invoice Workflow:**
/// 1. Request proforma invoice from selected contractor
/// 2. Contractor responds with proforma invoice document
/// 3. Approve proforma invoice
/// 4. Create service memo (requires approved proforma invoice)
/// </remarks>
[ApiController]
[Route("api/v{version:apiVersion}/service-proforma-invoices")]
[Authorize]
[Tags("Service Proforma Invoices")]
public class ServiceProformaInvoiceController(IServiceProformaInvoiceRepository repository) : ControllerBase
{
    /// <summary>
    /// Requests a proforma invoice from the selected contractor
    /// </summary>
    /// <remarks>
    /// Requests a formal proforma invoice from the contractor whose quotation was selected. 
    /// This is a required step before creating the service memo.
    /// 
    /// **Prerequisites:**
    /// - Job order must have a selected quotation
    /// - Job order status must be "QuotationSelected"
    /// 
    /// **What Happens:**
    /// - Proforma invoice request is created
    /// - Service provider receives email notification (if configured)
    /// - Job order status changes to "ProformaInvoiceRequested"
    /// - Items and pricing are copied from the selected quotation
    /// 
    /// **Next Steps:**
    /// - Contractor responds using `/respond` endpoint
    /// 
    /// **Example Request:**
    /// ```json
    /// {
    ///   "jobOrderId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    ///   "serviceQuotationId": "4fa85f64-5717-4562-b3fc-2c963f66afa7",
    ///   "notes": "Please provide proforma invoice for approval before proceeding"
    /// }
    /// ```
    /// </remarks>
    /// <param name="request">Proforma invoice request details</param>
    /// <returns>Returns the ID of the created proforma invoice request</returns>
    /// <response code="200">Proforma invoice requested successfully</response>
    /// <response code="400">Invalid request data or quotation not selected</response>
    [HttpPost("request")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> RequestServiceProformaInvoice([FromBody] RequestServiceProformaInvoiceRequest request)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.RequestServiceProformaInvoice(request, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Contractor responds with proforma invoice
    /// </summary>
    /// <remarks>
    /// Allows the contractor to submit their proforma invoice in response to the request.
    /// 
    /// **Required Information:**
    /// - Invoice number
    /// - Response date
    /// - Proforma invoice document URL (upload document first, then provide URL)
    /// 
    /// **Optional:**
    /// - Can update item prices/quantities if needed
    /// - Response notes
    /// 
    /// **What Happens:**
    /// - Proforma invoice status changes to "ResponseReceived"
    /// - Job order status changes to "ProformaInvoiceReceived"
    /// - Updated prices/quantities are saved if provided
    /// 
    /// **Next Steps:**
    /// - Approve proforma invoice using `/approve` endpoint
    /// 
    /// **Example Request:**
    /// ```json
    /// {
    ///   "serviceProformaInvoiceId": "5ga85f64-5717-4562-b3fc-2c963f66afa8",
    ///   "invoiceNumber": "PI-2024-001",
    ///   "responseDate": "2024-01-17T10:00:00Z",
    ///   "proformaInvoiceDocumentUrl": "/uploads/proforma-invoices/pi-2024-001.pdf",
    ///   "responseNotes": "Proforma invoice attached. Valid for 30 days.",
    ///   "updatedItems": [
    ///     {
    ///       "serviceProformaInvoiceItemId": "6ha85f64-5717-4562-b3fc-2c963f66afa9",
    ///       "unitPrice": 118.00
    ///     }
    ///   ]
    /// }
    /// ```
    /// </remarks>
    /// <param name="request">Proforma invoice response details</param>
    /// <returns>No content on success</returns>
    /// <response code="204">Proforma invoice response received successfully</response>
    /// <response code="400">Invalid request data or proforma invoice not found</response>
    [HttpPost("respond")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> RespondServiceProformaInvoice([FromBody] RespondServiceProformaInvoiceRequest request)
    {
        var result = await repository.RespondServiceProformaInvoice(request);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Approves a proforma invoice response
    /// </summary>
    /// <remarks>
    /// Approves the proforma invoice submitted by the contractor. This is required before creating 
    /// the service memo.
    /// 
    /// **Prerequisites:**
    /// - Proforma invoice must have been responded to
    /// - Status must be "ResponseReceived"
    /// 
    /// **What Happens:**
    /// - Proforma invoice status changes to "Approved"
    /// - Job order is now ready for service memo creation
    /// 
    /// **Next Steps:**
    /// - Create service memo using Service Memo API (requires approved proforma invoice)
    /// 
    /// **Example Request:**
    /// ```json
    /// {
    ///   "serviceProformaInvoiceId": "5ga85f64-5717-4562-b3fc-2c963f66afa8",
    ///   "approvalComments": "Proforma invoice approved. Proceed with service memo creation."
    /// }
    /// ```
    /// </remarks>
    /// <param name="request">Approval details (approvedById is set automatically from authenticated user)</param>
    /// <returns>No content on success</returns>
    /// <response code="204">Proforma invoice approved successfully</response>
    /// <response code="400">Invalid request data or proforma invoice not responded to</response>
    [HttpPost("approve")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> ApproveServiceProformaInvoice([FromBody] ApproveServiceProformaInvoiceRequest request)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        request.ApprovedById = Guid.Parse(userId);
        var result = await repository.ApproveServiceProformaInvoice(request);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a paginated list of service proforma invoices
    /// </summary>
    /// <remarks>
    /// Returns a paginated list of proforma invoices with filtering options.
    /// 
    /// **Filter Options:**
    /// - **status**: Filter by status (Requested, ResponseReceived, Approved, Rejected)
    /// - **jobOrderId**: Filter by job order
    /// - **serviceProviderId**: Filter by service provider
    /// </remarks>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Items per page (default: 10)</param>
    /// <param name="status">Filter by proforma invoice status</param>
    /// <param name="jobOrderId">Filter by job order ID</param>
    /// <param name="serviceProviderId">Filter by service provider ID</param>
    /// <returns>Paginated list of proforma invoices</returns>
    /// <response code="200">Returns paginated list of proforma invoices</response>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<ServiceProformaInvoiceDto>>))]
    public async Task<IResult> GetServiceProformaInvoices(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] ServiceProformaInvoiceStatus? status = null,
        [FromQuery] Guid? jobOrderId = null,
        [FromQuery] Guid? serviceProviderId = null)
    {
        var result = await repository.GetServiceProformaInvoices(page, pageSize, status, jobOrderId, serviceProviderId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a service proforma invoice by its unique identifier
    /// </summary>
    /// <remarks>
    /// Returns detailed information about a specific proforma invoice including:
    /// - Request and response details
    /// - Items/materials with pricing
    /// - Service charge
    /// - Total cost
    /// - Status and approval information
    /// - Related job order and quotation
    /// </remarks>
    /// <param name="id">Proforma invoice unique identifier</param>
    /// <returns>Proforma invoice details</returns>
    /// <response code="200">Returns proforma invoice details</response>
    /// <response code="404">Proforma invoice not found</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ServiceProformaInvoiceDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetServiceProformaInvoice([FromRoute] Guid id)
    {
        var result = await repository.GetServiceProformaInvoice(id);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
}

