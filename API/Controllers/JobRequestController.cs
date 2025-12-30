using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using DOMAIN.Entities.JobRequests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

/// <summary>
/// Job Request Management API
/// </summary>
/// <remarks>
/// This API manages service requests from departments. Departments can create job requests for equipment repairs, 
/// maintenance, or other services. These requests can then be assigned to internal staff or sent to external contractors.
/// 
/// ## Authentication
/// All endpoints require authentication via Bearer token. The user's department is automatically determined from their token.
/// 
/// ## Status Flow
/// 
/// **Job Request Status Values:**
/// - `Pending` (0): Initial status when request is created
/// - `Acknowledged` (1): Employee has acknowledged the assignment (internal only)
/// - `Assigned` (2): Request has been assigned to an employee (internal) or sent to contractors (external)
/// - `JobStarted` (3): Work has begun on the request
/// - `Completed` (4): Work has been completed and is awaiting verification/approval
/// - `SentToExternal` (5): Request has been sent to external contractors via Job Order
/// - `QuotationReceived` (6): Quotations have been received from service providers
/// - `ContractorSelected` (7): A contractor has been selected from quotations
/// - `Approved` (8): Request has been verified and approved by requester
/// - `Cancelled` (9): Request has been cancelled
/// 
/// **Handling Type Values:**
/// - `NotAssigned` (0): Request not yet assigned
/// - `Internal` (1): Handled by internal employee
/// - `External` (2): Handled by external contractor
/// 
/// ## Workflow Paths
/// 
/// **Internal Workflow:**
/// 1. Create job request → Status: `Pending`, HandlingType: `NotAssigned`
/// 2. Assign to internal employee → Status: `Assigned`, HandlingType: `Internal`
/// 3. Employee acknowledges → Status: `Acknowledged` (via JobExecution API)
/// 4. Employee starts work → Status: `JobStarted` (via JobExecution API)
/// 5. Employee completes work → Status: `Completed` (via JobExecution API)
/// 6. Supervisor verifies → Status remains `Completed` (via JobExecution API)
/// 7. Requester approves → Status: `Approved` (via JobExecution API)
/// 
/// **External Workflow:**
/// 1. Create job request → Status: `Pending`, HandlingType: `NotAssigned`
/// 2. Create job order → Status: `SentToExternal`, HandlingType: `External` (via JobOrder API)
/// 3. Receive quotations → Status: `QuotationReceived` (via ServiceQuotation API)
/// 4. Select contractor → Status: `ContractorSelected` (via JobOrder API)
/// 5. Start execution → Status: `JobStarted` (via JobOrder API)
/// 6. Complete execution → Status: `Completed` (via JobOrder API)
/// 7. Approve → Status: `Approved` (via JobOrder API)
/// 
/// ## Related APIs
/// - **Job Execution API**: `/api/v{version}/job-executions` - Manage internal job executions
/// - **Job Order API**: `/api/v{version}/job-orders` - Manage external job orders
/// - **Service Quotation API**: `/api/v{version}/service-quotations` - Manage contractor quotations
/// </remarks>
[ApiController]
[Route("api/v{version:apiVersion}/job-requests")]
[Authorize]
[Tags("Job Requests")]
public class JobRequestController(IJobRequestRepository repository) : ControllerBase
{
    /// <summary>
    /// Creates a new job request
    /// </summary>
    /// <remarks>
    /// Allows any authenticated department user to create a service request. The request will be created with:
    /// - Status: `Pending`
    /// - HandlingType: `NotAssigned`
    /// - Department: Automatically set from the authenticated user's token
    /// - IssuedBy: Automatically set to the authenticated user
    /// 
    /// **Field Validation:**
    /// - `location` (required, max 500 chars): Physical location where service is needed
    /// - `dateOfIssue` (required, DateTime): When the request was created (typically current date/time)
    /// - `descriptionOfWork` (required, max 2000 chars): Detailed description of the work required
    /// - `preferredCompletionDate` (required, DateTime): Target completion date (must be in the future)
    /// - `equipmentId` (optional, Guid): Reference to specific equipment if applicable
    /// - `equipmentInstrumentNumber` (optional, max 1000 chars): Equipment identification number
    /// - `item` (optional, max 500 chars): Related item name
    /// - `itemNumber` (optional, max 500 chars): Related item number
    /// 
    /// **Business Rules:**
    /// - The `preferredCompletionDate` should be after `dateOfIssue`
    /// - If `equipmentId` is provided, the equipment must exist in the system
    /// - Department is automatically determined from the user's authentication token
    /// 
    /// **Example Request:**
    /// ```json
    /// {
    ///   "location": "Production Floor - Line 2",
    ///   "equipmentId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    ///   "equipmentInstrumentNumber": "EQ-2024-001",
    ///   "dateOfIssue": "2024-01-15T10:00:00Z",
    ///   "descriptionOfWork": "Floor scale is not reading correctly. Needs calibration and repair.",
    ///   "preferredCompletionDate": "2024-01-20T17:00:00Z",
    ///   "item": "Floor Scale",
    ///   "itemNumber": "FS-001"
    /// }
    /// ```
    /// 
    /// **Example Response (200 OK):**
    /// ```
    /// "3fa85f64-5717-4562-b3fc-2c963f66afa6"
    /// ```
    /// </remarks>
    /// <param name="request">Job request creation details</param>
    /// <returns>Returns the GUID of the created job request</returns>
    /// <response code="200">Job request created successfully. Returns the job request ID (Guid)</response>
    /// <response code="400">Invalid request data. Check validation errors in response body</response>
    /// <response code="401">Unauthorized - User must be authenticated with valid Bearer token</response>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IResult> CreateJobRequest([FromBody] CreateJobRequest request)
    {
        var departmentId = (string)HttpContext.Items["Department"];
        if (string.IsNullOrEmpty(departmentId)) return TypedResults.Unauthorized();

        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.CreateJobRequest(request, Guid.Parse(departmentId),
            Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a paginated list of job requests
    /// </summary>
    /// <remarks>
    /// Returns a paginated list of job requests for the current user's department. 
    /// Results can be filtered by status, handling type, and searched by keywords.
    /// 
    /// **Query Parameters:**
    /// - `page` (int, default: 1): Page number (1-based indexing)
    /// - `pageSize` (int, default: 10): Number of items per page (max recommended: 100)
    /// - `searchQuery` (string, optional): Searches across:
    ///   - Location field
    ///   - DescriptionOfWork field
    ///   - EquipmentInstrumentNumber field
    ///   - Case-insensitive partial matching
    /// - `status` (JobRequestStatus enum, optional): Filter by specific status:
    ///   - `0` = Pending
    ///   - `1` = Acknowledged
    ///   - `2` = Assigned
    ///   - `3` = JobStarted
    ///   - `4` = Completed
    ///   - `5` = SentToExternal
    ///   - `6` = QuotationReceived
    ///   - `7` = ContractorSelected
    ///   - `8` = Approved
    ///   - `9` = Cancelled
    /// - `handlingType` (JobHandlingType enum, optional): Filter by handling type:
    ///   - `0` = NotAssigned
    ///   - `1` = Internal
    ///   - `2` = External
    /// 
    /// **Response Structure:**
    /// The response includes pagination metadata and the list of job requests:
    /// ```json
    /// {
    ///   "data": [
    ///     {
    ///       "id": "guid",
    ///       "location": "string",
    ///       "status": 0,
    ///       "handlingType": 0,
    ///       "descriptionOfWork": "string",
    ///       "dateOfIssue": "2024-01-15T10:00:00Z",
    ///       "preferredCompletionDate": "2024-01-20T17:00:00Z",
    ///       "department": { ... },
    ///       "equipment": { ... },
    ///       "issuedBy": { ... },
    ///       "assignedToEmployee": { ... },
    ///       "executions": [ ... ],
    ///       "jobOrders": [ ... ]
    ///     }
    ///   ],
    ///   "page": 1,
    ///   "pageSize": 10,
    ///   "totalCount": 50,
    ///   "totalPages": 5,
    ///   "hasPreviousPage": false,
    ///   "hasNextPage": true
    /// }
    /// ```
    /// 
    /// **Example Requests:**
    /// - GET /api/v1/job-requests
    /// - GET /api/v1/job-requests?page=1&amp;pageSize=20
    /// - GET /api/v1/job-requests?status=2&amp;handlingType=1
    /// - GET /api/v1/job-requests?searchQuery=floor scale&amp;page=1&amp;pageSize=10
    /// - GET /api/v1/job-requests?status=0&amp;handlingType=0&amp;page=1
    /// </remarks>
    /// <param name="page">Page number starting from 1 (default: 1)</param>
    /// <param name="pageSize">Number of items per page (default: 10, recommended max: 100)</param>
    /// <param name="searchQuery">Search term for location, description, or equipment number (case-insensitive)</param>
    /// <param name="status">Filter by job request status enum value (0-9)</param>
    /// <param name="handlingType">Filter by handling type enum value (0-2)</param>
    /// <returns>Paginated list of job requests with metadata</returns>
    /// <response code="200">Returns paginated list of job requests with pagination metadata</response>
    /// <response code="401">Unauthorized - User must be authenticated with valid Bearer token</response>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<JobRequestDto>>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IResult> GetJobRequests(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string searchQuery = null,
        [FromQuery] JobRequestStatus? status = null,
        [FromQuery] JobHandlingType? handlingType = null)
    {
        var departmentId = (string)HttpContext.Items["Department"];
        if (string.IsNullOrEmpty(departmentId)) return TypedResults.Unauthorized();

        var result = await repository.GetJobRequests(page, pageSize, searchQuery, status, handlingType,
            Guid.Parse(departmentId));
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a job request by its unique identifier
    /// </summary>
    /// <remarks>
    /// Returns comprehensive details about a specific job request including all related entities.
    /// 
    /// **Response Includes:**
    /// - **Basic Information**: ID, location, status, handling type, dates, description
    /// - **Department**: Department that created the request
    /// - **Equipment**: Equipment details if linked (nullable)
    /// - **Issued By**: User who created the request
    /// - **Internal Assignment** (if HandlingType = Internal):
    ///   - Assigned employee details
    ///   - Assignment date and assigned by user
    ///   - Job executions with activities and consumed items
    /// - **External Assignment** (if HandlingType = External):
    ///   - Service details
    ///   - Job orders with quotations and executions
    /// - **Attachments**: Any files attached to the request
    /// 
    /// **Response Structure:**
    /// ```json
    /// {
    ///   "id": "guid",
    ///   "location": "string",
    ///   "status": 0,
    ///   "handlingType": 0,
    ///   "descriptionOfWork": "string",
    ///   "dateOfIssue": "2024-01-15T10:00:00Z",
    ///   "preferredCompletionDate": "2024-01-20T17:00:00Z",
    ///   "equipmentInstrumentNumber": "string",
    ///   "item": "string",
    ///   "itemNumber": "string",
    ///   "department": {
    ///     "id": "guid",
    ///     "name": "string",
    ///     ...
    ///   },
    ///   "equipment": { ... } | null,
    ///   "issuedBy": { ... },
    ///   "assignedToEmployee": { ... } | null,
    ///   "assignedAt": "2024-01-16T10:00:00Z" | null,
    ///   "assignedBy": { ... } | null,
    ///   "service": { ... } | null,
    ///   "executions": [
    ///     {
    ///       "id": "guid",
    ///       "status": 0,
    ///       "activities": [ ... ],
    ///       "consumedItems": [ ... ],
    ///       ...
    ///     }
    ///   ],
    ///   "jobOrders": [
    ///     {
    ///       "id": "guid",
    ///       "status": 0,
    ///       "serviceProviders": [ ... ],
    ///       "executions": [ ... ],
    ///       ...
    ///     }
    ///   ],
    ///   "attachments": [ ... ]
    /// }
    /// ```
    /// 
    /// **Example Request:**
    /// ```
    /// GET /api/v1/job-requests/3fa85f64-5717-4562-b3fc-2c963f66afa6
    /// ```
    /// </remarks>
    /// <param name="id">Job request unique identifier (Guid)</param>
    /// <returns>Complete job request details with all related entities</returns>
    /// <response code="200">Returns complete job request details</response>
    /// <response code="404">Job request not found with the provided ID</response>
    /// <response code="401">Unauthorized - User must be authenticated (if authorization is required)</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(JobRequestDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetJobRequest([FromRoute] Guid id)
    {
        var result = await repository.GetJobRequest(id);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Updates an existing job request
    /// </summary>
    /// <remarks>
    /// Updates job request details using partial update (only provided fields are updated).
    /// 
    /// **Update Restrictions:**
    /// - Can only update requests with status `Pending` (0)
    /// - Cannot update requests that have been assigned (HandlingType != `NotAssigned`)
    /// - Cannot update requests that have been sent to external contractors
    /// 
    /// **Updatable Fields:**
    /// All fields are optional - only include fields you want to update:
    /// - `location` (string, max 500 chars): Update location
    /// - `equipmentId` (Guid?): Update or remove equipment reference (set to null to remove)
    /// - `equipmentInstrumentNumber` (string, max 1000 chars): Update equipment number
    /// - `descriptionOfWork` (string, max 2000 chars): Update work description
    /// - `preferredCompletionDate` (DateTime?): Update preferred completion date
    /// - `item` (string, max 500 chars): Update item name
    /// - `itemNumber` (string, max 500 chars): Update item number
    /// 
    /// **Validation:**
    /// - If `equipmentId` is provided, the equipment must exist in the system
    /// - `preferredCompletionDate` must be in the future if provided
    /// 
    /// **Example Request:**
    /// ```json
    /// {
    ///   "location": "Updated Production Floor - Line 3",
    ///   "descriptionOfWork": "Updated description with more details",
    ///   "preferredCompletionDate": "2024-01-25T17:00:00Z"
    /// }
    /// ```
    /// 
    /// **Example Response:**
    /// - Status 204: Update successful (no response body)
    /// - Status 400: Validation error or request cannot be updated (check error details)
    /// - Status 404: Job request not found
    /// </remarks>
    /// <param name="id">Job request unique identifier (Guid)</param>
    /// <param name="request">Partial update object with fields to update</param>
    /// <returns>No content on success</returns>
    /// <response code="204">Job request updated successfully</response>
    /// <response code="400">Invalid request data, validation error, or request cannot be updated (e.g., already assigned)</response>
    /// <response code="404">Job request not found with the provided ID</response>
    /// <response code="401">Unauthorized - User must be authenticated</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> UpdateJobRequest([FromRoute] Guid id, [FromBody] UpdateJobRequestRequest request)
    {
        var result = await repository.UpdateJobRequest(id, request);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Deletes a job request (Soft Delete)
    /// </summary>
    /// <remarks>
    /// Performs a soft delete on a job request. The record is marked as deleted but remains in the database.
    /// 
    /// **Deletion Restrictions:**
    /// - Can only delete requests with status:
    ///   - `Pending` (0)
    ///   - `Cancelled` (9)
    /// - Cannot delete if:
    ///   - Status is not `Pending` or `Cancelled`
    ///   - Request has been assigned (HandlingType != `NotAssigned`)
    ///   - Request has been sent to external contractors
    ///   - Request has any job executions or job orders
    /// 
    /// **What Happens:**
    /// - The request is marked with `DeletedAt` timestamp
    /// - The `LastDeletedById` is set to the authenticated user
    /// - The request will not appear in normal queries (filtered out)
    /// - Related data (executions, orders) remain intact
    /// 
    /// **Example Request:**
    /// ```
    /// DELETE /api/v1/job-requests/3fa85f64-5717-4562-b3fc-2c963f66afa6
    /// ```
    /// 
    /// **Example Responses:**
    /// - Status 204: Deletion successful (no response body)
    /// - Status 400: Request cannot be deleted (check error message for reason)
    /// - Status 404: Job request not found
    /// </remarks>
    /// <param name="id">Job request unique identifier (Guid)</param>
    /// <returns>No content on success</returns>
    /// <response code="204">Job request soft deleted successfully</response>
    /// <response code="400">Job request cannot be deleted - already assigned, in progress, or has related records</response>
    /// <response code="404">Job request not found with the provided ID</response>
    /// <response code="401">Unauthorized - User must be authenticated</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> DeleteJobRequest([FromRoute] Guid id)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.DeleteJobRequest(id, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Assigns a job request to an internal employee
    /// </summary>
    /// <remarks>
    /// Assigns a job request to an internal employee for execution. This operation:
    /// - Creates a new `JobExecution` record
    /// - Updates the job request status to `Assigned` (2)
    /// - Sets the handling type to `Internal` (1)
    /// - Records assignment timestamp and assigner information
    /// 
    /// **Prerequisites:**
    /// - Job request must exist and not be deleted
    /// - Job request status must be `Pending` (0) or `Acknowledged` (1)
    /// - Job request must not already be assigned (HandlingType = `NotAssigned`)
    /// - Employee (`assignedToEmployeeId`) must exist and be active
    /// - Assigner (`assignedById`) must exist and be a valid user
    /// 
    /// **Request Fields:**
    /// - `jobRequestId` (required, Guid): The job request to assign
    /// - `assignedToEmployeeId` (required, Guid): Employee who will execute the work
    /// - `assignedById` (required, Guid): User assigning the request (typically current user)
    /// - `notes` (optional, string): Additional notes or instructions for the employee
    /// 
    /// **What Happens After Assignment:**
    /// 1. Employee receives the assignment (status: `Assigned`)
    /// 2. Employee can acknowledge via JobExecution API → Status: `Acknowledged`
    /// 3. Employee can start work via JobExecution API → Status: `JobStarted`
    /// 4. Employee can record activities and consumed items
    /// 5. Employee can complete work via JobExecution API → Status: `Completed`
    /// 6. Supervisor can verify via JobExecution API
    /// 7. Requester can approve via JobExecution API → Status: `Approved`
    /// 
    /// **Related Endpoints:**
    /// - POST `/api/v{version}/job-executions/acknowledge` - Employee acknowledges assignment
    /// - POST `/api/v{version}/job-executions/start` - Employee starts work
    /// - POST `/api/v{version}/job-executions/complete` - Employee completes work
    /// - POST `/api/v{version}/job-executions/verify` - Supervisor verifies completion
    /// - POST `/api/v{version}/job-executions/approve` - Requester approves completion
    /// 
    /// **Example Request:**
    /// ```json
    /// {
    ///   "jobRequestId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    ///   "assignedToEmployeeId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
    ///   "assignedById": "8d9e6679-7425-40de-944b-e07fc1f90ae8",
    ///   "notes": "Priority: High. Please complete by end of week. Contact supervisor if issues arise."
    /// }
    /// ```
    /// 
    /// **Example Response (200 OK):**
    /// ```
    /// "7c9e6679-7425-40de-944b-e07fc1f90ae7"
    /// ```
    /// Returns the GUID of the created JobExecution record.
    /// </remarks>
    /// <param name="request">Assignment details including job request ID, employee ID, and assigner ID</param>
    /// <returns>Returns the GUID of the created JobExecution record</returns>
    /// <response code="200">Job request assigned successfully. Returns the JobExecution ID (Guid)</response>
    /// <response code="400">Invalid assignment data, validation error, or request cannot be assigned (e.g., already assigned, wrong status, invalid employee)</response>
    /// <response code="401">Unauthorized - User must be authenticated</response>
    [HttpPost("assign-internal")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IResult> AssignInternalJob([FromBody] AssignInternalJobRequest request)
    {
        var result = await repository.AssignInternalJob(request);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
}