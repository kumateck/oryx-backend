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
    /// Creates a new stock adjustment for items or material batches.
    /// </summary>
    /// <remarks>
    /// This endpoint allows for bulk adjustment of stock levels based on physical counts.
    /// 
    /// **Request Parameters:**
    /// - **AdjustmentNumber**: A unique reference string for this adjustment transaction.
    /// - **WarehouseId**: The GUID of the warehouse where the stock is physically located.
    /// - **AdjustmentDate**: The UTC timestamp when the physical count was performed.
    /// - **TargetType**: 
    ///     - `0` (Item): Adjusts general store items (e.g., spare parts, office supplies).
    ///     - `1` (Material): Adjusts specific material batches on shelves (e.g., raw materials, reagents).
    /// - **Lines**: A collection of adjustment details:
    ///     - **ProductId**: 
    ///         - If TargetType is `Item`, use the **ItemId**.
    ///         - If TargetType is `Material`, use the **ShelfMaterialBatchId**.
    ///     - **PhysicalCount**: The total quantity actually found on hand. The system will calculate the variance automatically.
    ///     - **ReasonCode**: The reason for adjustment (0: PhysicalCount, 1: Damage, 2: Theft, 3: Expiry, 4: DataEntryError, 5: ReturnedGoods, 6: Other).
    ///     - **Notes**: Optional textual context for the specific adjustment line.
    /// </remarks>
    /// <param name="request">The stock adjustment request payload.</param>
    /// <returns>Returns a summary of the processed stock adjustment including the variance total.</returns>
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
