using APP.Extensions;
using APP.IRepository;
using DOMAIN.Entities.StockAdjustments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Route("api/v{version:apiVersion}/stock-adjustment")]
[ApiController]
[Authorize]
public class StockAdjustmentController(IStockAdjustmentRepository repository) : ControllerBase
{
    /// <summary>
    /// Creates a new stock adjustment.
    /// </summary>
    /// <param name="request">The stock adjustment request payload.</param>
    /// <returns>Returns a summary of the processed stock adjustment.</returns>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(StockAdjustmentSummaryDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IResult> CreateStockAdjustment(
        [FromBody] CreateStockAdjustmentRequest request
    )
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null)
            return TypedResults.Unauthorized();

        var result = await repository.CreateStockAdjustment(request, Guid.Parse(userId));

        if (result.IsSuccess)
        {
            return TypedResults.Created(
                $"/api/v1/stock-adjustment/{result.Value.Id}",
                result.Value
            );
        }

        return result.ToProblemDetails();
    }
}
