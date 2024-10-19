using APP.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using APP.IRepository;
using DOMAIN.Entities.Requisitions;

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
    /// <returns>Returns a success or failure result.</returns>
    [HttpPost("approve")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> ApproveRequisition([FromBody] ApproveRequisitionRequest request)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();
        var roleIds = (List<Guid>)HttpContext.Items["Roles"]; 

        var result = await repository.ApproveRequisition(request, Guid.Parse(userId), roleIds);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    #endregion
}
