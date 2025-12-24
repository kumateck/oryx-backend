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
/// **Workflow:**
/// 1. Department creates a job request
/// 2. Request can be assigned internally or sent externally
/// 3. Internal: Assigned to employee → Execution → Verification → Approval
/// 4. External: Job Order → Quotations → Selection → Proforma Invoice → Service Memo → Execution → Approval
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
    /// Allows any department to create a service request. The request will be created with status "Pending" 
    /// and can later be assigned to internal staff or sent to external contractors.
    /// 
    /// **Required Fields:**
    /// - Location: Where the service is needed
    /// - DateOfIssue: When the request was created
    /// - DescriptionOfWork: Detailed description of the work required
    /// - PreferredCompletionDate: When the work should be completed
    /// 
    /// **Optional Fields:**
    /// - EquipmentId: If the request is for specific equipment
    /// - EquipmentInstrumentNumber: Equipment identification number
    /// 
    /// **Example Request:**
    /// ```json
    /// {
    ///   "location": "Production Floor - Line 2",
    ///   "equipmentId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    ///   "equipmentInstrumentNumber": "EQ-2024-001",
    ///   "dateOfIssue": "2024-01-15T10:00:00Z",
    ///   "descriptionOfWork": "Floor scale is not reading correctly. Needs calibration and repair.",
    ///   "preferredCompletionDate": "2024-01-20T17:00:00Z"
    /// }
    /// ```
    /// </remarks>
    /// <param name="request">Job request details</param>
    /// <returns>Returns the ID of the created job request</returns>
    /// <response code="200">Job request created successfully</response>
    /// <response code="400">Invalid request data</response>
    /// <response code="401">Unauthorized - User must be authenticated</response>
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

        var result = await repository.CreateJobRequest(request, Guid.Parse(departmentId), Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a paginated list of job requests
    /// </summary>
    /// <remarks>
    /// Returns a paginated list of job requests for the current user's department. 
    /// Results can be filtered by status and handling type.
    /// 
    /// **Filter Options:**
    /// - **status**: Filter by request status (Pending, Acknowledged, Assigned, JobStarted, Completed, etc.)
    /// - **handlingType**: Filter by how the request is handled (NotAssigned, Internal, External)
    /// - **searchQuery**: Search in location, description, or equipment number
    /// 
    /// **Pagination:**
    /// - **page**: Page number (default: 1)
    /// - **pageSize**: Number of items per page (default: 10)
    /// 
    /// **Example Request:**
    /// ```
    /// GET /api/v1/job-requests?page=1&pageSize=20&status=Pending&handlingType=Internal
    /// ```
    /// </remarks>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Items per page (default: 10)</param>
    /// <param name="searchQuery">Search term for location, description, or equipment number</param>
    /// <param name="status">Filter by job request status</param>
    /// <param name="handlingType">Filter by handling type (Internal/External)</param>
    /// <returns>Paginated list of job requests</returns>
    /// <response code="200">Returns paginated list of job requests</response>
    /// <response code="401">Unauthorized - User must be authenticated</response>
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

        var result = await repository.GetJobRequests(page, pageSize, searchQuery, status, handlingType, Guid.Parse(departmentId));
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a job request by its unique identifier
    /// </summary>
    /// <remarks>
    /// Returns detailed information about a specific job request including:
    /// - Request details (location, description, dates)
    /// - Assignment information (if assigned internally)
    /// - Execution history (activities, consumed items)
    /// - Related job orders (if sent externally)
    /// </remarks>
    /// <param name="id">Job request unique identifier</param>
    /// <returns>Job request details</returns>
    /// <response code="200">Returns job request details</response>
    /// <response code="404">Job request not found</response>
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
    /// Updates job request details. Only updates fields that are provided (partial update supported).
    /// Can only update requests that are in "Pending" status and not yet assigned.
    /// </remarks>
    /// <param name="id">Job request unique identifier</param>
    /// <param name="request">Updated job request details</param>
    /// <returns>No content on success</returns>
    /// <response code="204">Job request updated successfully</response>
    /// <response code="400">Invalid request data or request cannot be updated</response>
    /// <response code="404">Job request not found</response>
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
    /// Deletes a job request
    /// </summary>
    /// <remarks>
    /// Soft deletes a job request. Can only delete requests that are in "Pending" or "Cancelled" status 
    /// and have not been assigned or sent to external contractors.
    /// </remarks>
    /// <param name="id">Job request unique identifier</param>
    /// <returns>No content on success</returns>
    /// <response code="204">Job request deleted successfully</response>
    /// <response code="404">Job request not found</response>
    /// <response code="400">Job request cannot be deleted (already assigned or in progress)</response>
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
    /// Assigns a job request to an internal employee for execution. This creates a JobExecution record 
    /// and changes the request status to "Assigned".
    /// 
    /// **Prerequisites:**
    /// - Job request must be in "Pending" or "Acknowledged" status
    /// - Employee must exist and be active
    /// 
    /// **After Assignment:**
    /// - Employee can acknowledge the assignment
    /// - Employee can start work and record activities
    /// - Employee can record consumed items
    /// - Employee can mark work as complete
    /// - Supervisor can verify completion
    /// - Requester can approve completion
    /// 
    /// **Example Request:**
    /// ```json
    /// {
    ///   "jobRequestId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    ///   "assignedToEmployeeId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
    ///   "assignedById": "8d9e6679-7425-40de-944b-e07fc1f90ae8",
    ///   "notes": "Priority: High. Please complete by end of week."
    /// }
    /// ```
    /// </remarks>
    /// <param name="request">Assignment details</param>
    /// <returns>Returns the ID of the created job execution</returns>
    /// <response code="200">Job request assigned successfully</response>
    /// <response code="400">Invalid assignment data or request cannot be assigned</response>
    [HttpPost("assign-internal")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> AssignInternalJob([FromBody] AssignInternalJobRequest request)
    {
        var result = await repository.AssignInternalJob(request);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
}