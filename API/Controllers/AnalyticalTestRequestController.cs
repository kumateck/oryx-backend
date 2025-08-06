using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using DOMAIN.Entities.AnalyticalTestRequests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/qa/analytical-tests")]
[Authorize]
public class AnalyticalTestRequestController(IAnalyticalTestRequestRepository repository) : ControllerBase
{

    /// <summary>
    /// Creates an analytical test request
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> CreateAnalyticalTestRequest([FromBody] CreateAnalyticalTestRequest request)
    {
        var result = await repository.CreateAnalyticalTestRequest(request);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a list of paginated analytical test requests
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<AnalyticalTestRequestDto>>))]
    public async Task<IResult> GetAnalyticalTestRequests([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string searchQuery = null, [FromQuery] AnalyticalTestStatus? status = null)
    {
        var result = await repository.GetAnalyticalTestRequests(page, pageSize, searchQuery, status);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
    
    /// <summary>
    /// Retrieves the details of an analytical test request by its ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(AnalyticalTestRequestDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetAnalyticalTestRequest([FromRoute] Guid id)
    {
        var result = await repository.GetAnalyticalTestRequest(id);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Updates the details of an analytical test request by its ID.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent, Type = typeof(AnalyticalTestRequestDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> UpdateAnalyticalTestRequest([FromRoute] Guid id, [FromBody] CreateAnalyticalTestRequest request)
    {
        var userId = (string) HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();
        
        var result = await repository.UpdateAnalyticalTestRequest(id, request, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Deletes an analytical test request.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> DeleteAnalyticalTestRequest([FromRoute] Guid id)
    {
        var userId = (string) HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();
        
        var result = await repository.DeleteAnalyticalTestRequest(id, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
    
    /// <summary>
    /// Retrieves the details of an analytical test request by its activity step ID
    /// </summary>
    [HttpGet("activity-step/{activityStepId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(AnalyticalTestRequestDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetAnalyticalTestRequestByActivityStep([FromRoute] Guid activityStepId)
    {
        var result = await repository.GetAnalyticalTestRequestByActivityStep(activityStepId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
}