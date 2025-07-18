using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using DOMAIN.Entities.ProductionOrders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/v{Version:apiVersion}/production-orders")]
[Authorize]
public class ProductionOrderController(IProductionOrderRepository repository) : ControllerBase
{
    /// <summary>
    /// Creates a production order
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> CreateProductionOrder([FromBody] CreateProductionOrderRequest request)
    {
        var result = await repository.CreateProductionOrder(request);
        return result.IsSuccess ? TypedResults.Ok(result) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a paginated list of production orders.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<ProductionOrderDto>>))]
    public async Task<IResult> GetProductionOrders([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string searchQuery = null)
    {
        var result = await repository.GetProductionOrders(page, pageSize, searchQuery);
        return result.IsSuccess ? TypedResults.Ok(result) : result.ToProblemDetails();
    }
    
    /// <summary>
    /// Retrieves a production order by its ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ProductionOrderDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetProductionOrder([FromRoute] Guid id)
    {
        var result = await repository.GetProductionOrder(id);
        return result.IsSuccess ? TypedResults.Ok(result) : result.ToProblemDetails();
    }
    
    /// <summary>
    /// Updates a production order by its ID.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent, Type = typeof(ProductionOrderDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> UpdateProductionOrder([FromRoute] Guid id, [FromBody] CreateProductionOrderRequest request)
    {
        var result = await repository.UpdateProductionOrder(id, request);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
    
    /// <summary>
    /// Deletes a production order by its ID.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> DeleteProductionOrder([FromRoute] Guid id)
    {
        var userId =  (string) HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();
        
        var result = await repository.DeleteProductionOrder(id, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.Ok(result) : result.ToProblemDetails();
    }
}