using APP.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using APP.IRepository;
using DOMAIN.Entities.Requisitions;
using APP.Utils;
using DOMAIN.Entities.Requisitions.Request;

namespace API.Controllers;

[Route("api/v{version:apiVersion}/requisition")]
[ApiController]
public class RequisitionController(IRequisitionRepository repository) : ControllerBase
{
    #region Requisition Management

    /// <summary>
    /// Creates a new Stock Requisition.
    /// </summary>
    /// <param name="request">The CreateRequisitionRequest object.</param>
    /// <returns>Returns the ID of the created Stock Requisition.</returns>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> CreateRequisition([FromBody] CreateRequisitionRequest request)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();
        
        var result = await repository.CreateRequisition(request, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
    
    /// <summary>
    /// Retrieves a paginated list of requisitions.
    /// </summary>
    /// <param name="page">The current page number.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="searchQuery">Search query for filtering results.</param>
    /// <returns>Returns a paginated list of requisitions.</returns>
    [HttpGet]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<RequisitionDto>>))]
    public async Task<IResult> GetManufacturers([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string searchQuery = null)
    {
        var result = await repository.GetRequisitions(page, pageSize, searchQuery);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a specific Stock Requisition by its ID.
    /// </summary>
    /// <param name="requisitionId">The ID of the Stock Requisition.</param>
    /// <returns>Returns the Stock Requisition.</returns>
    [HttpGet("{requisitionId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(RequisitionDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetRequisition(Guid requisitionId)
    {
        var result = await repository.GetRequisition(requisitionId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Approves a Stock Requisition.
    /// </summary>
    /// <param name="request">The ApproveRequisitionRequest object.</param>
    ///     /// <param name="requisitionId">The ID of the Stock Requisition being approved.</param>
    /// <returns>Returns a success or failure result.</returns>
    [HttpPost("{requisitionId}/approve")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> ApproveRequisition([FromBody] ApproveRequisitionRequest request, Guid requisitionId)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();
        var roleIds = (List<Guid>)HttpContext.Items["Roles"]; 

        var result = await repository.ApproveRequisition(request,  requisitionId, Guid.Parse(userId), roleIds);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Processes a Stock Requisition.
    /// </summary>
    /// <param name="request">The CreateRequisitionRequest object.</param>
    /// <param name="requisitionId">The ID of the Stock Requisition being processed.</param>
    /// <returns>Returns a success or failure result.</returns>
    [HttpPost("{requisitionId}/process")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> ProcessRequisition([FromBody] CreateRequisitionRequest request, Guid requisitionId)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.ProcessRequisition(request, requisitionId, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    #endregion
}
