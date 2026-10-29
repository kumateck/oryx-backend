using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using DOMAIN.Entities.ErrorLogs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

/// <summary>
/// Server errors recorded on this instance. Restricted to users who can view the IT audit trail
/// because entries include request bodies and user details.
/// </summary>
[Route("api/v{version:apiVersion}/error-log")]
[ApiController]
[Authorize(PermissionKeys.CanViewAuditTrail)]
public class ErrorLogController(IErrorLogRepository repository) : ControllerBase
{
    /// <summary>
    /// Lists errors, newest first. Defaults to the last 7 days. Pass traceId (the reference a user
    /// saw in an error message or the X-Trace-Id header) to find the errors from that request.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<ErrorLogListDto>>))]
    public async Task<IResult> GetErrorLogs([FromQuery] ErrorLogFilter filter) =>
        TypedResults.Ok(await repository.GetErrorLogs(filter));

    /// <summary>
    /// Groups errors that are the same failure, most frequent first, to decide what to fix.
    /// Use a row's fingerprint as a filter on the list endpoint to see every occurrence.
    /// </summary>
    [HttpGet("summary")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<ErrorLogSummaryDto>))]
    public async Task<IResult> GetErrorSummary([FromQuery] ErrorLogFilter filter) =>
        TypedResults.Ok(await repository.GetErrorSummary(filter));

    /// <summary>Full detail of one error: stack trace, request body, headers and user.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ErrorLogDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetErrorLog(Guid id)
    {
        var result = await repository.GetErrorLog(id);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
}
