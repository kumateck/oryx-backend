using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using DOMAIN.Entities.JobRequests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

/// <summary>
/// Job Order Management API
/// </summary>
/// <remarks>
/// Manages job orders sent to external service providers/contractors. Job orders are created when a job request 
/// needs to be handled externally. The workflow includes:
/// 
/// 1. **Create Job Order** - Convert job request to job order
/// 2. **Send to Providers** - Send RFQ to multiple contractors
/// 3. **Receive Quotations** - Contractors submit quotations
/// 4. **Compare & Select** - Compare quotations and select winner
/// 5. **Request Proforma Invoice** - Request formal invoice from selected contractor
/// 6. **Receive Proforma Invoice** - Contractor submits proforma invoice
/// 7. **Create Service Memo** - Generate formal service memo
/// 8. **Execute Service** - Contractor performs the work
/// 9. **Verify & Approve** - Supervisor verifies, requester approves
/// </remarks>
[ApiController]
[Route("api/v{version:apiVersion}/job-orders")]
[Authorize]
[Tags("Job Orders")]
public class JobOrderController(IJobOrderRepository repository) : ControllerBase
{
    /// <summary>
    /// Creates a new job order for external service providers
    /// </summary>
    /// <remarks>
    /// Creates a job order from a job request to be sent to external contractors. This is the first step 
    /// in the external sourcing workflow.
    /// 
    /// **Prerequisites:**
    /// - Job request must exist
    /// - Service is optional (can be null)
    /// - At least one service provider must be selected
    /// 
    /// **What Happens:**
    /// - Job order is created with status "Pending"
    /// - Job request status changes to "SentToExternal"
    /// - Job request handling type changes to "External"
    /// - If `issuedBySignature` is not provided, it will be retrieved from the user's profile
    /// 
    /// **Next Steps:**
    /// - Send job order to providers using `/send-to-providers` endpoint
    /// 
    /// **Example Request:**
    /// ```json
    /// {
    ///   "jobRequestId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    ///   "serviceId": "5fa85f64-5717-4562-b3fc-2c963f66afa7",
    ///   "issuedDate": "2024-01-15T10:00:00Z",
    ///   "issuedById": "8d9e6679-7425-40de-944b-e07fc1f90ae8",
    ///   "description": "Fabrication and installation of ductwork at dispensing room",
    ///   "issuedBySignature": "signature-reference-string",
    ///   "serviceProviderIds": [
    ///     "9e0f6679-7425-40de-944b-e07fc1f90ae9",
    ///     "af1f6679-7425-40de-944b-e07fc1f90afa"
    ///   ]
    /// }
    /// ```
    /// 
    /// **Note:** Both `serviceId` and `description` are optional. If `issuedBySignature` is not provided, the system will use the signature from the user's profile.
    /// </remarks>
    /// <param name="request">Job order creation details</param>
    /// <returns>Returns the ID of the created job order</returns>
    /// <response code="200">Job order created successfully</response>
    /// <response code="400">Invalid request data</response>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> CreateJobOrder([FromBody] CreateJobOrderRequest request)
    {
        var result = await repository.CreateJobOrder(request);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a paginated list of job orders
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<JobOrderDto>>))]
    public async Task<IResult> GetJobOrders(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] JobOrderStatus? status = null,
        [FromQuery] Guid? jobRequestId = null,
        [FromQuery] Guid? serviceId = null)
    {
        var result = await repository.GetJobOrders(page, pageSize, status, jobRequestId, serviceId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a job order by its unique identifier
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(JobOrderDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetJobOrder([FromRoute] Guid id)
    {
        var result = await repository.GetJobOrder(id);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
    
    
    [HttpGet("service-providers")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<JobOrderServiceProviderDto>>))]
    public async Task<IResult> GetJobOrderServiceProviders(int page = 1, int pageSize = 10, string searchQuery = null)
    {
        var result = await repository.GetJobOrderResponseServiceProviders(page, pageSize, searchQuery);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Sends a job order to service providers for quotation (RFQ)
    /// </summary>
    /// <remarks>
    /// Sends the job order to multiple service providers requesting quotations. This initiates the RFQ process.
    /// 
    /// **What Happens:**
    /// - Job order status changes to "SentToProviders"
    /// - Service providers are notified (via email if configured)
    /// - Providers can then submit quotations using the Service Quotation API
    /// 
    /// **Note:** You can send to additional providers later by calling this endpoint again with new provider IDs.
    /// 
    /// **Example Request:**
    /// ```json
    /// {
    ///   "jobOrderId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    ///   "serviceProviderIds": [
    ///     "9e0f6679-7425-40de-944b-e07fc1f90ae9",
    ///     "af1f6679-7425-40de-944b-e07fc1f90afa"
    ///   ]
    /// }
    /// ```
    /// </remarks>
    /// <param name="request">Job order and provider IDs</param>
    /// <returns>No content on success</returns>
    /// <response code="204">Job order sent to providers successfully</response>
    /// <response code="400">Invalid request data</response>
    [HttpPost("send-to-providers")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> SendJobOrderToProviders([FromBody] SendJobOrderToProvidersRequest request)
    {
        var result = await repository.SendJobOrderToProviders(request);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Selects a quotation for a job order
    /// </summary>
    /// <remarks>
    /// Selects the winning quotation from multiple received quotations. This marks the selected quotation 
    /// and rejects all others.
    /// 
    /// **What Happens:**
    /// - Selected quotation is marked as "Selected"
    /// - Other quotations are marked as "Rejected"
    /// - Job order status changes to "QuotationSelected"
    /// - Job request status changes to "ContractorSelected"
    /// 
    /// **Next Steps:**
    /// - Request proforma invoice from selected contractor using Service Proforma Invoice API
    /// 
    /// **Example Request:**
    /// ```json
    /// {
    ///   "jobOrderId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    ///   "quotationId": "4fa85f64-5717-4562-b3fc-2c963f66afa7"
    /// }
    /// ```
    /// </remarks>
    /// <param name="request">Job order and quotation IDs</param>
    /// <returns>No content on success</returns>
    /// <response code="204">Quotation selected successfully</response>
    /// <response code="400">Invalid request data or quotation not found</response>
    [HttpPost("select-quotation")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> SelectQuotation([FromBody] SelectQuotationRequest request)
    {
        var result = await repository.SelectQuotation(request);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Starts job order execution by external contractor
    /// </summary>
    /// <remarks>
    /// Initiates the execution phase after service memo has been created and issued. This allows the 
    /// contractor to start performing the work and record activities.
    /// 
    /// **Prerequisites:**
    /// - Service memo must be created and issued
    /// - Job order status must be "MemoCreated"
    /// 
    /// **What Happens:**
    /// - Job order execution record is created
    /// - Job order status changes to "InProgress"
    /// - Job request status changes to "JobStarted"
    /// 
    /// **After Starting:**
    /// - Contractor can record activities using `/executions/{id}/activities`
    /// - Contractor can record consumed items using `/executions/{id}/consumed-items`
    /// - Contractor can complete execution using `/complete-execution`
    /// </remarks>
    /// <param name="request">Execution start details</param>
    /// <returns>Returns the ID of the job order execution</returns>
    /// <response code="200">Execution started successfully</response>
    /// <response code="400">Invalid request data or service memo not created</response>
    [HttpPost("start-execution")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> StartJobOrderExecution([FromBody] StartJobOrderExecutionRequest request)
    {
        var result = await repository.StartJobOrderExecution(request);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Records an activity performed by external contractor
    /// </summary>
    [HttpPost("executions/{id:guid}/activities")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> RecordJobOrderActivity([FromRoute] Guid id, [FromBody] RecordJobActivityRequest request)
    {
        var result = await repository.RecordJobOrderActivity(id, request);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Records an item consumed by external contractor
    /// </summary>
    [HttpPost("executions/{id:guid}/consumed-items")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> RecordJobOrderConsumedItem([FromRoute] Guid id, [FromBody] RecordConsumedItemRequest request)
    {
        var result = await repository.RecordJobOrderConsumedItem(id, request);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Completes a job order execution
    /// </summary>
    [HttpPost("complete-execution")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> CompleteJobOrderExecution([FromBody] CompleteJobOrderExecutionRequest request)
    {
        var result = await repository.CompleteJobOrderExecution(request);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Verifies a completed job order execution (by supervisor)
    /// </summary>
    /// <remarks>
    /// Allows a supervisor to verify that the work completed by the contractor meets requirements. 
    /// This is a required step before final approval by the requester.
    /// 
    /// **Prerequisites:**
    /// - Job order execution must be completed
    /// - Execution status must be "Completed"
    /// 
    /// **What Happens:**
    /// - Execution status changes to "VerifiedBySupervisor"
    /// - Verification comments are recorded
    /// 
    /// **Next Steps:**
    /// - Requester can approve using `/approve-execution` endpoint
    /// </remarks>
    /// <param name="request">Verification details</param>
    /// <returns>No content on success</returns>
    /// <response code="204">Execution verified successfully</response>
    /// <response code="400">Invalid request data or execution not completed</response>
    [HttpPost("verify-execution")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> VerifyJobOrderExecution([FromBody] VerifyJobOrderExecutionRequest request)
    {
        var result = await repository.VerifyJobOrderExecution(request);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Approves a verified job order execution (by requester)
    /// </summary>
    /// <remarks>
    /// Final approval step where the original requester confirms satisfaction with the completed work. 
    /// This completes the entire job order workflow.
    /// 
    /// **Prerequisites:**
    /// - Job order execution must be verified by supervisor
    /// - Execution status must be "VerifiedBySupervisor"
    /// 
    /// **What Happens:**
    /// - Execution status changes to "ApprovedByRequester"
    /// - Job order status changes to "Approved"
    /// - Job request status changes to "Approved"
    /// - Approval comments and satisfaction status are recorded
    /// 
    /// **This completes the workflow!**
    /// </remarks>
    /// <param name="request">Approval details including satisfaction status</param>
    /// <returns>No content on success</returns>
    /// <response code="204">Execution approved successfully</response>
    /// <response code="400">Invalid request data or execution not verified</response>
    [HttpPost("approve-execution")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> ApproveJobOrderExecution([FromBody] ApproveJobOrderExecutionRequest request)
    {
        var result = await repository.ApproveJobOrderExecution(request);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
}

