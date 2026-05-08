using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using DOMAIN.Entities.JobRequests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

/// <summary>
/// Service Memo Management API
/// </summary>
/// <remarks>
/// Manages formal service memos created after proforma invoice approval. The service memo is the 
/// formal document that authorizes the contractor to proceed with the work.
/// 
/// **Service Memo Workflow:**
/// 1. Create service memo (requires approved proforma invoice)
/// 2. Issue service memo to contractor
/// 3. Contractor starts execution
/// 4. Work is completed, verified, and approved
/// </remarks>
[ApiController]
[Route("api/v{version:apiVersion}/service-memos")]
[Authorize]
[Tags("Service Memos")]
public class ServiceMemoController(IServiceMemoRepository repository) : ControllerBase
{
    /// <summary>
    /// Creates a new service memo
    /// </summary>
    /// <remarks>
    /// Creates a formal service memo after proforma invoice has been approved. The service memo 
    /// formalizes the agreement with the contractor and authorizes them to proceed with the work.
    /// 
    /// **Prerequisites:**
    /// - Job order must have an approved proforma invoice
    /// - Proforma invoice status must be "Approved"
    /// - Selected quotation must exist
    /// 
    /// **Required Information:**
    /// - Agreed service charge
    /// - Agreed materials cost
    /// - Expected start and completion dates
    /// - Terms and conditions
    /// 
    /// **What Happens:**
    /// - Service memo is created with status "Draft"
    /// - Memo number is auto-generated
    /// - Job order status changes to "MemoCreated"
    /// 
    /// **Next Steps:**
    /// - Issue service memo using `/issue` endpoint
    /// 
    /// **Example Request:**
    /// ```json
    /// {
    ///   "jobOrderId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    ///   "serviceQuotationId": "4fa85f64-5717-4562-b3fc-2c963f66afa7",
    ///   "serviceProviderId": "9e0f6679-7425-40de-944b-e07fc1f90ae9",
    ///   "issuedDate": "2024-01-18T10:00:00Z",
    ///   "issuedById": "8d9e6679-7425-40de-944b-e07fc1f90ae8",
    ///   "agreedServiceCharge": 9000.00,
    ///   "agreedMaterialsCost": 8650.00,
    ///   "expectedStartDate": "2024-01-22T08:00:00Z",
    ///   "expectedCompletionDate": "2024-01-26T17:00:00Z",
    ///   "termsAndConditions": "Payment within 30 days of completion. Warranty period: 6 months.",
    ///   "specialInstructions": "Work must be completed during non-production hours (6 PM - 6 AM)"
    /// }
    /// ```
    /// </remarks>
    /// <param name="request">Service memo creation details</param>
    /// <returns>Returns the ID of the created service memo</returns>
    /// <response code="200">Service memo created successfully</response>
    /// <response code="400">Invalid request data or proforma invoice not approved</response>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> CreateServiceMemo([FromBody] CreateServiceMemoRequest request)
    {
        var result = await repository.CreateServiceMemo(request);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a paginated list of service memos
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<ServiceMemoDto>>))]
    public async Task<IResult> GetServiceMemos(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] ServiceMemoStatus? status = null,
        [FromQuery] Guid? jobOrderId = null,
        [FromQuery] Guid? serviceProviderId = null)
    {
        var result = await repository.GetServiceMemos(page, pageSize, status, jobOrderId, serviceProviderId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a service memo by its unique identifier
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ServiceMemoDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetServiceMemo([FromRoute] Guid id)
    {
        var result = await repository.GetServiceMemo(id);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Updates an existing service memo
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> UpdateServiceMemo([FromRoute] Guid id, [FromBody] UpdateServiceMemoRequest request)
    {
        var result = await repository.UpdateServiceMemo(id, request);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
    
    /// <summary>
    /// Updates an existing service memo
    /// </summary>
    [HttpPut("paid/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> MarkServiceMemoAsPaid([FromRoute] Guid id)
    {
        var result = await repository.MarkServiceMemoAsPaid(id);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Issues a service memo to the contractor
    /// </summary>
    /// <remarks>
    /// Issues the service memo to the contractor, officially authorizing them to begin work. 
    /// This changes the memo status from "Draft" to "Issued".
    /// 
    /// **Prerequisites:**
    /// - Service memo must be in "Draft" status
    /// 
    /// **What Happens:**
    /// - Service memo status changes to "Issued"
    /// - Job request status changes to "JobStarted"
    /// - Contractor can now start execution using Job Order API
    /// 
    /// **Next Steps:**
    /// - Contractor starts execution using `/job-orders/start-execution` endpoint
    /// </remarks>
    /// <param name="request">Service memo ID to issue</param>
    /// <returns>No content on success</returns>
    /// <response code="204">Service memo issued successfully</response>
    /// <response code="400">Invalid request data or memo not in draft status</response>
    [HttpPost("issue")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> IssueServiceMemo([FromBody] IssueServiceMemoRequest request)
    {
        var result = await repository.IssueServiceMemo(request);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
}

