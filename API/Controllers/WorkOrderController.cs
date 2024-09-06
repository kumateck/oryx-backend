using APP.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using APP.IRepository;
using DOMAIN.Entities.WorkOrders;

namespace API.Controllers;

[Route("api/v{version:apiVersion}/work-orders")]
[ApiController]
public class WorkOrderController(IWorkOrderRepository repository) : ControllerBase
{
    /// <summary>
    /// Creates a new work order.
    /// </summary>
    /// <param name="request">The CreateWorkOrderRequest object.</param>
    /// <param name="userId">The ID of the user creating the work order.</param>
    /// <returns>Returns the ID of the created work order.</returns>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> CreateWorkOrder([FromBody] CreateWorkOrderRequest request, Guid userId)
    {
        var result = await repository.CreateWorkOrder(request, userId);
        return result.IsSuccess ? TypedResults.Created("", result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a specific work order by its ID.
    /// </summary>
    /// <param name="workOrderId">The ID of the work order.</param>
    /// <returns>Returns the work order details.</returns>
    [HttpGet("{workOrderId}")]
    //[Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(WorkOrderDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetWorkOrder(Guid workOrderId)
    {
        var result = await repository.GetWorkOrder(workOrderId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a paginated list of work orders.
    /// </summary>
    /// <param name="page">The current page number.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="searchQuery">Search query for filtering results.</param>
    /// <returns>Returns a paginated list of work orders.</returns>
    [HttpGet]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IResult> GetWorkOrders([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string searchQuery = null)
    {
        var result = await repository.GetWorkOrders(page, pageSize, searchQuery);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Updates a specific work order by its ID.
    /// </summary>
    /// <param name="request">The UpdateWorkOrderRequest object containing updated work order data.</param>
    /// <param name="workOrderId">The ID of the work order to be updated.</param>
    /// <param name="userId">The ID of the user performing the update.</param>
    /// <returns>Returns a success or failure result.</returns>
    [HttpPut("{workOrderId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> UpdateWorkOrder([FromBody] UpdateWorkOrderRequest request, Guid workOrderId, Guid userId)
    {
        var result = await repository.UpdateWorkOrder(request, workOrderId, userId);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Deletes a specific work order by its ID.
    /// </summary>
    /// <param name="workOrderId">The ID of the work order to be deleted.</param>
    /// <param name="userId">The ID of the user performing the deletion.</param>
    /// <returns>Returns a success or failure result.</returns>
    [HttpDelete("{workOrderId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> DeleteWorkOrder(Guid workOrderId, Guid userId)
    {
        var result = await repository.DeleteWorkOrder(workOrderId, userId);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
}
