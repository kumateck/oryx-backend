using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using DOMAIN.Entities.JobRequests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/job-executions")]
[Authorize]
public class JobExecutionController(IJobExecutionRepository repository) : ControllerBase
{
    /// <summary>
    /// Retrieves a paginated list of job executions
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<JobExecutionDto>>))]
    public async Task<IResult> GetJobExecutions(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] JobExecutionStatus? status = null,
        [FromQuery] Guid? employeeId = null,
        [FromQuery] Guid? jobRequestId = null)
    {
        var result = await repository.GetJobExecutions(page, pageSize, status, employeeId, jobRequestId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a job execution by its unique identifier
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(JobExecutionDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetJobExecution([FromRoute] Guid id)
    {
        var result = await repository.GetJobExecution(id);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Acknowledges a job execution (employee acknowledges receipt of assignment)
    /// </summary>
    [HttpPost("acknowledge")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> AcknowledgeJobExecution([FromBody] AcknowledgeJobExecutionRequest request)
    {
        var result = await repository.AcknowledgeJobExecution(request);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Starts a job execution
    /// </summary>
    [HttpPost("start")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> StartJobExecution([FromBody] StartJobExecutionRequest request)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.StartJobExecution(request, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Records an activity performed during job execution
    /// </summary>
    [HttpPost("{id:guid}/activities")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> RecordJobActivity([FromRoute] Guid id, [FromBody] RecordJobActivityRequest request)
    {
        var result = await repository.RecordJobActivity(id, request);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Records an item consumed during job execution
    /// </summary>
    [HttpPost("{id:guid}/consumed-items")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> RecordConsumedItem([FromRoute] Guid id, [FromBody] RecordConsumedItemRequest request)
    {
        var result = await repository.RecordConsumedItem(id, request);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Completes a job execution
    /// </summary>
    [HttpPost("complete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> CompleteJobExecution([FromBody] CompleteJobExecutionRequest request)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.CompleteJobExecution(request, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Reassigns a job execution to another employee
    /// </summary>
    [HttpPost("reassign")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> ReassignJobExecution([FromBody] ReassignJobExecutionRequest request)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.ReassignJobExecution(request, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Verifies a completed job execution (by supervisor)
    /// </summary>
    [HttpPost("verify")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> VerifyJobExecution([FromBody] VerifyJobExecutionRequest request)
    {
        var result = await repository.VerifyJobExecution(request);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Approves a verified job execution
    /// </summary>
    [HttpPost("approve")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> ApproveJobExecution([FromBody] ApproveJobExecutionRequest request)
    {
        var result = await repository.ApproveJobExecution(request);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
}

