using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using DOMAIN.Entities.QcWorksheets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

/// <summary>
/// Test requests (the ARD) in the rebuilt QC module.
/// <para>
/// New and additive: this coexists with, and never touches, the live
/// <c>AnalyticalTestRequestController</c> and its routes.
/// </para>
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/qc/worksheets/test-requests")]
[Authorize]
public class QcTestRequestController(ITestRequestRepository repository) : ControllerBase
{
    /// <summary>Retrieves a paginated list of test requests.</summary>
    [HttpGet]
    [Authorize(QcWorksheetPermissionKeys.CanViewQcTestRequests)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<TestRequestSummaryDto>>))]
    public async Task<IResult> GetTestRequests(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string searchQuery = null,
        [FromQuery] TestRequestStatus? status = null,
        [FromQuery] TestRequestType? type = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        var result = await repository.GetTestRequests(page, pageSize, searchQuery, status, type, from, to);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a test request with its subjects and their worksheets. Worksheet field values
    /// come from the worksheet endpoints, not from here.
    /// </summary>
    [HttpGet("{id:guid}")]
    [Authorize(QcWorksheetPermissionKeys.CanViewQcTestRequests)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(TestRequestDetailDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetTestRequest([FromRoute] Guid id)
    {
        var result = await repository.GetTestRequest(id);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Creates a round and, in the same transaction, every worksheet it implies — one per
    /// subject per worksheet link on the specification version it pins to.
    /// <para>
    /// The permission key required depends on the body: raising a round outside the schedule is
    /// a different authority from overriding a scheduled one, so the two are checked separately
    /// rather than sharing one key.
    /// </para>
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(TestRequestDetailDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IResult> CreateTestRequest(
        [FromBody] CreateTestRequestRequest request,
        [FromServices] IAuthorizationService authorizationService)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId is null) return TypedResults.Unauthorized();

        var authorized = await authorizationService.AuthorizeAsync(
            User, RequiredCreateKey(request.ScheduleOrigin));

        if (!authorized.Succeeded) return TypedResults.Forbid();

        var result = await repository.CreateTestRequest(request, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Adds subjects to a round that has not started testing — the Water/EM case where points
    /// are added incrementally. Rejected once the round is past Sampled.
    /// </summary>
    [HttpPost("{id:guid}/subjects")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(TestRequestDetailDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IResult> AddSubjects(
        [FromRoute] Guid id,
        [FromBody] AddTestRequestSubjectsRequest request,
        [FromServices] IAuthorizationService authorizationService)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId is null) return TypedResults.Unauthorized();

        // Adding a subject creates real testing work, so it is gated like creating the round.
        // The round already records its own origin, so either create key admits the caller.
        var scheduled = await authorizationService.AuthorizeAsync(
            User, QcWorksheetPermissionKeys.CanCreateScheduledQcTestRequest);

        var unscheduled = await authorizationService.AuthorizeAsync(
            User, QcWorksheetPermissionKeys.CanCreateUnscheduledQcTestRequest);

        if (!scheduled.Succeeded && !unscheduled.Succeeded) return TypedResults.Forbid();

        var result = await repository.AddSubjects(id, request, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Records sample collection against one or more subjects and moves the round from Draft to
    /// Sampled. An empty subject list means every subject of the round.
    /// </summary>
    [HttpPost("{id:guid}/record-sample")]
    [Authorize(QcWorksheetPermissionKeys.CanRecordQcSample)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(TestRequestDetailDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> RecordSample(
        [FromRoute] Guid id, [FromBody] RecordTestRequestSampleRequest request)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId is null) return TypedResults.Unauthorized();

        var result = await repository.RecordSample(id, request, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static string RequiredCreateKey(TestRequestScheduleOrigin? origin) =>
        origin == TestRequestScheduleOrigin.Unscheduled
            ? QcWorksheetPermissionKeys.CanCreateUnscheduledQcTestRequest
            : QcWorksheetPermissionKeys.CanCreateScheduledQcTestRequest;
}
