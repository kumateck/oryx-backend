using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using DOMAIN.Entities.JobRequests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/job-orders")]
[Authorize]
public class JobOrderController(IJobOrderRepository repository) : ControllerBase
{
    /// <summary>
    /// Creates a new job order for external service providers
    /// </summary>
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

    /// <summary>
    /// Sends a job order to service providers for quotation
    /// </summary>
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
    [HttpPost("approve-execution")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> ApproveJobOrderExecution([FromBody] ApproveJobOrderExecutionRequest request)
    {
        var result = await repository.ApproveJobOrderExecution(request);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
}

