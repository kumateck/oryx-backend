using APP.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using APP.IRepository;
using APP.Utils;
using DOMAIN.Entities.Approvals;

namespace API.Controllers;

[Route("api/v{version:apiVersion}/approval")]
[ApiController]
public class ApprovalController(IApprovalRepository repository) : ControllerBase
{
    /// <summary>
    /// Creates a new approval.
    /// </summary>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> CreateApproval([FromBody] CreateApprovalRequest request)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();
        
        var result = await repository.CreateApproval(request, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPost("delegate")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent, Type = typeof(DelegateApprovalDto))]
    public IResult DelegateApproval([FromBody] DelegateApproval approval)
    {
        var result =  repository.DelegateApproval(approval);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a specific approval by its ID.
    /// </summary>
    [HttpGet("{approvalId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ApprovalDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetApproval(Guid approvalId)
    {
        var result = await repository.GetApproval(approvalId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpGet("{modelType}/{modelId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ApprovalEntity))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetApprovalByModelId(string modelType, Guid modelId)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();
        
        var result = await repository.GetEntityRequiringApproval(modelType, modelId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a paginated list of approvals.
    /// </summary>
    [HttpGet]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Paginateable<IEnumerable<ApprovalDto>>))]
    public async Task<IResult> GetApprovals([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string searchQuery = null)
    {
        var result = await repository.GetApprovals(page, pageSize, searchQuery);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Updates a specific approval by its ID.
    /// </summary>
    [HttpPut("{approvalId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> UpdateApproval([FromBody] CreateApprovalRequest request, Guid approvalId)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();
        
        var result = await repository.UpdateApproval(request, approvalId, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Deletes a specific approval by its ID.
    /// </summary>
    [HttpDelete("{approvalId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> DeleteApproval(Guid approvalId)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null) return TypedResults.Unauthorized();
        
        var result = await repository.DeleteApproval(approvalId, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
    
    /// <summary>
    /// Approves an item (Requisition, PurchaseOrder, BillingSheet) by model type and ID.
    /// </summary>
    [HttpPost("approve/{modelType}/{modelId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IResult> ApproveItem(string modelType, Guid modelId, [FromBody] ApprovalRequestBody body)
    {
        var userId = (string)HttpContext.Items["Sub"];
        var roleIds =(List<Guid>)HttpContext.Items["Roles"];

        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.ApproveItem(modelType, modelId, Guid.Parse(userId), roleIds, body.Comments);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
    
    /// <summary>
    /// Rejects an item (Requisition, PurchaseOrder, BillingSheet) by model type and ID.
    /// </summary>
    [HttpPost("reject/{modelType}/{modelId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IResult> RejectItem(string modelType, Guid modelId, [FromBody] ApprovalRequestBody body)
    {
        var userId = (string)HttpContext.Items["Sub"];
        var roleIds =(List<Guid>)HttpContext.Items["Roles"];

        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.RejectItem(modelType, modelId, Guid.Parse(userId), roleIds, body.Comments);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Gets all items requiring approval by the current user.
    /// </summary>
    [HttpGet("my-pending")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<ApprovalEntity>))]
    public async Task<IResult> GetPendingApprovals()
    {
        var userId = (string)HttpContext.Items["Sub"];
        var roleIds =(List<Guid>)HttpContext.Items["Roles"];

        if (userId == null) return TypedResults.Unauthorized();

        var result = await repository.GetEntitiesRequiringApproval(Guid.Parse(userId), roleIds);
        return TypedResults.Ok(result);
    }
}
