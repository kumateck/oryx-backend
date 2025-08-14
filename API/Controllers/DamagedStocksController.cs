using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using DOMAIN.Entities.DamagedStocks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/damaged-goods")]
[Authorize]
public class DamagedStocksController(IDamagedStocksRepository repository) : ControllerBase
{
    /// <summary>
    /// Creates a damaged stock record
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> CreateDamagedStocks([FromBody] CreateDamagedStockRequest request)
    {
        var result = await repository.CreateDamagedStocks(request);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a paginated list of damaged stocks
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK,  Type = typeof(Paginateable<IEnumerable<DamagedStockDto>>))]
    public async Task<IResult> GetDamagedStocks([FromQuery] int page = 1, [FromQuery] int pageSize = 10,
        [FromQuery] string searchQuery = null)
    {
        var result = await repository.GetDamagedStocks(page, pageSize, searchQuery);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves the details of a damaged stock by its ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK,  Type = typeof(DamagedStockDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetDamagedStock([FromRoute] Guid id)
    {
        var result = await repository.GetDamagedStock(id);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
    
    /// <summary>
    /// Updates the details of a damaged stock by its ID.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent, Type = typeof(DamagedStockDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> UpdateDamagedStock([FromRoute] Guid id, [FromBody] CreateDamagedStockRequest request)
    {
        var result = await repository.UpdateDamagedStocks(id, request);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
    
    /// <summary>
    /// Deletes the details of a damaged stock by its ID.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> DeleteDamagedStock([FromRoute] Guid id)
    {
        var userId = (string) HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();
        
        var result = await repository.DeleteDamagedStocks(id, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
}