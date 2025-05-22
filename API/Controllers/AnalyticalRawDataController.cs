using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using DOMAIN.Entities.AnalyticalRawData;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/analytical-raw-data")]
[Authorize]
public class AnalyticalRawDataController(IAnalyticalRawDataRepository repository) : ControllerBase
{

    /// <summary>
    /// Creates analytical raw data.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> CreateAnalyticalRawData([FromBody] CreateAnalyticalRawDataRequest request)
    {
        var result = await repository.CreateAnalyticalRawData(request);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a paginated list of analytical raw data based on search criteria.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<AnalyticalRawDataDto>>))]
    public async Task<IResult> GetAnalyticalRawData([FromQuery] int page, [FromQuery] int pageSize, [FromQuery] string searchQuery)
    {
        var result = await repository.GetAnalyticalRawData(page, pageSize, searchQuery);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves specific analytical raw data by its ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(AnalyticalRawDataDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetAnalyticalRawData([FromRoute] Guid id)
    {
        var result = await repository.GetAnalyticalRawData(id);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
    
    /// <summary>
    /// Updates analytical raw data by its ID.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent, Type = typeof(AnalyticalRawDataDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> UpdateAnalyticalRawData([FromRoute] Guid id, [FromBody] CreateAnalyticalRawDataRequest request)
    {
        var result = await repository.UpdateAnalyticalRawData(id, request);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Deletes analytical raw data by its ID.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> DeleteAnalyticRawData([FromRoute] Guid id)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();
        
        var result = await repository.DeleteAnalyticalRawData(id, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
}