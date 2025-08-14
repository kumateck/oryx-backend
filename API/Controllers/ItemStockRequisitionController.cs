using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using DOMAIN.Entities.Items;
using DOMAIN.Entities.ItemStockRequisitions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/items/stock-requisitions")]
[Authorize]
public class ItemStockRequisitionController(IItemStockRequisitionRepository repository) : ControllerBase
{
    /// <summary>
    /// Creates an item stock requisition
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> CreateItem([FromBody] CreateItemStockRequisitionRequest request)
    {
        var result = await repository.CreateItemStockRequisition(request);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a paginated list of item stock requisitions
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<ItemStockRequisitionDto>>))]
    public async Task<IResult> GetItems([FromQuery] int page = 1, [FromQuery] int pageSize = 10,
        [FromQuery] string searchQuery = null)
    {
        var result = await repository.GetItemStockRequisitions(page, pageSize, searchQuery);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves an item stock requisition by its ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ItemDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetItem([FromRoute] Guid id)
    {
        var result = await repository.GetItemStockRequisition(id);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Updates an item stock requisition
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent, Type = typeof(ItemDto))]
    public async Task<IResult> UpdateItem([FromRoute] Guid id, [FromBody] CreateItemStockRequisitionRequest request)
    {
        var result = await repository.UpdateItemStockRequisition(id, request);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Deletes an item stock requisition
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> DeleteItem([FromRoute] Guid id)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();
        
        var result = await repository.DeleteItemStockRequisition(id, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
    
    /// <summary>
    /// Issues a stock against an item stock requisition
    /// </summary>
    /// <returns></returns>
    [HttpPost("{id:guid}/issue-stock-against-requisition")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IssueItemStockRequisitionDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> IssueStockAgainstRequisition([FromRoute] Guid id, [FromBody] IssueStockAgainstRequisitionRequest request)
    {
        var result = await repository.IssueStockRequisition(id, request);
        return result.IsSuccess ? TypedResults.Ok() : result.ToProblemDetails();
    }
}