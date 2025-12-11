using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using DOMAIN.Entities.JobRequests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/service-memos")]
[Authorize]
public class ServiceMemoController(IServiceMemoRepository repository) : ControllerBase
{
    /// <summary>
    /// Creates a new service memo
    /// </summary>
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
    /// Issues a service memo to the contractor
    /// </summary>
    [HttpPost("issue")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> IssueServiceMemo([FromBody] IssueServiceMemoRequest request)
    {
        var result = await repository.IssueServiceMemo(request);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
}

