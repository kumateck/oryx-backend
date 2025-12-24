using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using DOMAIN.Entities.JobRequests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

/// <summary>
/// Service Quotation Management API
/// </summary>
/// <remarks>
/// Manages quotations submitted by service providers/contractors in response to job orders. 
/// Contractors submit quotations with service charges, required materials, and completion timelines.
/// 
/// **Quotation Workflow:**
/// 1. Contractor receives job order (RFQ)
/// 2. Contractor submits quotation with pricing and materials
/// 3. System compares quotations side-by-side
/// 4. Negotiation can occur (price adjustments)
/// 5. Best quotation is selected
/// </remarks>
[ApiController]
[Route("api/v{version:apiVersion}/service-quotations")]
[Authorize]
[Tags("Service Quotations")]
public class ServiceQuotationController(IServiceQuotationRepository repository) : ControllerBase
{
    /// <summary>
    /// Creates a new service quotation (Contractor submits quotation)
    /// </summary>
    /// <remarks>
    /// Allows a service provider/contractor to submit a quotation in response to a job order RFQ.
    /// 
    /// **Required Information:**
    /// - Service charge (labor cost)
    /// - Materials/items required with quantities and prices
    /// - Estimated completion time
    /// - Currency
    /// 
    /// **What Happens:**
    /// - Quotation is created with status "Submitted"
    /// - Job order status changes to "QuotationsReceived" (if first quotation)
    /// - Provider's response status is updated
    /// 
    /// **Example Request:**
    /// ```json
    /// {
    ///   "jobOrderId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    ///   "serviceProviderId": "9e0f6679-7425-40de-944b-e07fc1f90ae9",
    ///   "quotationNumber": "QT-2024-001",
    ///   "submittedDate": "2024-01-16T10:00:00Z",
    ///   "serviceCharge": 9750.00,
    ///   "currencyId": "bf2f6679-7425-40de-944b-e07fc1f90afb",
    ///   "estimatedDays": 5,
    ///   "estimatedCompletionDate": "2024-01-21T17:00:00Z",
    ///   "notes": "Can start immediately upon approval",
    ///   "items": [
    ///     {
    ///       "itemName": "Ductwork Material",
    ///       "description": "Galvanized steel ductwork",
    ///       "quantity": 35,
    ///       "unitOfMeasureId": "cf3f6679-7425-40de-944b-e07fc1f90afc",
    ///       "unitPrice": 125.00,
    ///       "supplier": "Steel Supplies Ltd"
    ///     },
    ///     {
    ///       "itemName": "Aeromat Glue",
    ///       "quantity": 1,
    ///       "unitOfMeasureId": "df4f6679-7425-40de-944b-e07fc1f90afd",
    ///       "unitPrice": 475.00,
    ///       "supplier": "Building Materials Co"
    ///     }
    ///   ]
    /// }
    /// ```
    /// </remarks>
    /// <param name="request">Quotation details</param>
    /// <returns>Returns the ID of the created quotation</returns>
    /// <response code="200">Quotation created successfully</response>
    /// <response code="400">Invalid request data or provider not sent job order</response>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> CreateServiceQuotation([FromBody] CreateServiceQuotationRequest request)
    {
        var result = await repository.CreateServiceQuotation(request);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a paginated list of service quotations
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<ServiceQuotationDto>>))]
    public async Task<IResult> GetServiceQuotations(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] QuotationStatus? status = null,
        [FromQuery] Guid? jobOrderId = null,
        [FromQuery] Guid? serviceProviderId = null)
    {
        var result = await repository.GetServiceQuotations(page, pageSize, status, jobOrderId, serviceProviderId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a service quotation by its unique identifier
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ServiceQuotationDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetServiceQuotation([FromRoute] Guid id)
    {
        var result = await repository.GetServiceQuotation(id);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Updates an existing service quotation
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> UpdateServiceQuotation([FromRoute] Guid id, [FromBody] UpdateServiceQuotationRequest request)
    {
        var result = await repository.UpdateServiceQuotation(id, request);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Negotiates quotation pricing
    /// </summary>
    /// <remarks>
    /// Allows negotiation of quotation prices before selection. You can negotiate:
    /// - Service charge (labor cost)
    /// - Individual item/material prices
    /// 
    /// **What Happens:**
    /// - Quotation status changes to "Negotiating"
    /// - Negotiated prices are stored separately from original prices
    /// - Total cost is recalculated based on negotiated prices
    /// 
    /// **Note:** Negotiation is optional. You can select a quotation without negotiating.
    /// 
    /// **Example Request:**
    /// ```json
    /// {
    ///   "quotationId": "4fa85f64-5717-4562-b3fc-2c963f66afa7",
    ///   "negotiatedServiceCharge": 9000.00,
    ///   "negotiationNotes": "Reduced service charge by 7.7%",
    ///   "negotiatedItems": [
    ///     {
    ///       "quotationItemId": "5ga85f64-5717-4562-b3fc-2c963f66afa8",
    ///       "negotiatedUnitPrice": 120.00
    ///     }
    ///   ]
    /// }
    /// ```
    /// </remarks>
    /// <param name="request">Negotiation details</param>
    /// <returns>No content on success</returns>
    /// <response code="204">Quotation negotiated successfully</response>
    /// <response code="400">Invalid request data or quotation not found</response>
    [HttpPost("negotiate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> NegotiateQuotation([FromBody] NegotiateQuotationRequest request)
    {
        var result = await repository.NegotiateQuotation(request);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Compares quotations for a job order side-by-side
    /// </summary>
    /// <remarks>
    /// Retrieves all quotations for a job order sorted by total cost (lowest first) for easy comparison.
    /// This helps in making an informed decision when selecting the best quotation.
    /// 
    /// **Comparison Includes:**
    /// - Service charge from each provider
    /// - Materials/items cost breakdown
    /// - Total cost (service + materials)
    /// - Estimated completion time
    /// - Provider information
    /// - Negotiated prices (if any)
    /// 
    /// **Use Case:**
    /// Use this endpoint to display a comparison table in the UI showing all quotations side-by-side.
    /// 
    /// **Example Request:**
    /// ```json
    /// {
    ///   "jobOrderId": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
    /// }
    /// ```
    /// </remarks>
    /// <param name="request">Job order ID</param>
    /// <returns>List of quotations sorted by total cost</returns>
    /// <response code="200">Returns list of quotations for comparison</response>
    /// <response code="400">Invalid request data or no quotations found</response>
    [HttpPost("compare")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<ServiceQuotationDto>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> CompareQuotations([FromBody] CompareQuotationsRequest request)
    {
        var result = await repository.CompareQuotations(request);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
}

