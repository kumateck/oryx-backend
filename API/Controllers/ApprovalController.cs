using APP.Extensions;
using APP.IRepository;
using APP.Services.Approvals;
using APP.Utils;
using DOMAIN.Entities.Approvals;
using DOMAIN.Entities.QcWorksheets;
using DOMAIN.Entities.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Route("api/v{version:apiVersion}/approval")]
[ApiController]
public class ApprovalController(IApprovalRepository repository, IApprovalDelegationService delegations,
    IAuthorizationService authorization) : ControllerBase
{
    /// <summary>
    /// Gets all users that have a pending approval.
    /// </summary>
    [HttpGet("users-with-pending")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<UserDto>))]
    public async Task<IResult> GetUsersWithPendingApprovals()
    {
        var result = await repository.GetUsersWithPendingApprovals();
        return TypedResults.Ok(result);
    }

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
        if (userId == null)
            return TypedResults.Unauthorized();

        var result = await repository.CreateApproval(request, Guid.Parse(userId));
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Hands the caller's approvals to another user for a period (at most 90 days, one delegation at a time).
    /// While it is active the delegate sees and may decide the caller's pending approvals; the caller keeps
    /// that right too. QC documents that are signed electronically are not delegated.
    /// </summary>
    [HttpPost("delegate")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Guid))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IResult> DelegateApproval([FromBody] DelegateApproval approval)
    {
        if (!Guid.TryParse(HttpContext.Items["Sub"] as string, out var userId))
            return TypedResults.Unauthorized();
        var result = await delegations.Delegate(userId, approval, DateTime.UtcNow);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>The caller's delegations: those they gave and those given to them, including ones ended in the last 30 days.</summary>
    [HttpGet("delegations/mine")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(MyApprovalDelegationsDto))]
    public async Task<IResult> GetMyDelegations()
    {
        if (!Guid.TryParse(HttpContext.Items["Sub"] as string, out var userId))
            return TypedResults.Unauthorized();
        return TypedResults.Ok(await delegations.GetMine(userId, DateTime.UtcNow));
    }

    /// <summary>Ends a delegation at once. Either the delegator or the delegate may end it.</summary>
    [HttpDelete("delegations/{delegationId:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> RevokeDelegation(Guid delegationId)
    {
        if (!Guid.TryParse(HttpContext.Items["Sub"] as string, out var userId))
            return TypedResults.Unauthorized();
        var result = await delegations.Revoke(delegationId, userId, DateTime.UtcNow);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Transfers approval rights from one user to another.
    /// </summary>
    [HttpPost("transfer-rights")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> TransferApprovalRights([FromBody] TransferApprovalRequest request)
    {
        var result = await repository.TransferApprovalRights(request);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a specific approval by its ID.
    /// </summary>
    [HttpGet("{approvalId:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ApprovalDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetApproval(Guid approvalId)
    {
        var result = await repository.GetApproval(approvalId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpGet("{modelType}/{modelId:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ApprovalEntity))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetApprovalByModelId(string modelType, Guid modelId)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null)
            return TypedResults.Unauthorized();
        if (modelType == "ShipmentDiscrepancy" &&
            !(await authorization.AuthorizeAsync(User, PermissionKeys.CanViewShipmentDiscrepancy)).Succeeded)
            return TypedResults.Forbid();
        if (modelType == "StockDiscrepancy" &&
            !(await authorization.AuthorizeAsync(User, PermissionKeys.CanViewStockDiscrepancy)).Succeeded)
            return TypedResults.Forbid();

        var result = await repository.GetEntityRequiringApproval(modelType, modelId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>Details of a document in the caller's active pending queue.</summary>
    [HttpGet("my-pending/{modelType}/{modelId:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ApprovalEntity))]
    public async Task<IResult> GetMyPendingApprovalDetail(string modelType, Guid modelId)
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null)
            return TypedResults.Unauthorized();
        if (modelType == "ShipmentDiscrepancy" &&
            !(await authorization.AuthorizeAsync(User, PermissionKeys.CanViewShipmentDiscrepancy)).Succeeded)
            return TypedResults.Forbid();
        if (modelType == "StockDiscrepancy" &&
            !(await authorization.AuthorizeAsync(User, PermissionKeys.CanViewStockDiscrepancy)).Succeeded)
            return TypedResults.Forbid();

        var roleIds = (List<Guid>)HttpContext.Items["Roles"];
        var assigned = (await delegations.Pending(repository, Guid.Parse(userId), roleIds, modelType, DateTime.UtcNow))
            .FirstOrDefault(item => item.Entity.Id == modelId);
        if (assigned is null)
            return TypedResults.Forbid();

        var result = await repository.GetEntityRequiringApproval(modelType, modelId);
        if (result.IsSuccess) result.Value.OnBehalfOf = assigned.Entity.OnBehalfOf;
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Retrieves a paginated list of approvals.
    /// </summary>
    [HttpGet]
    [Authorize]
    [ProducesResponseType(
        StatusCodes.Status200OK,
        Type = typeof(Paginateable<IEnumerable<ApprovalDto>>)
    )]
    public async Task<IResult> GetApprovals(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string searchQuery = null
    )
    {
        var result = await repository.GetApprovals(page, pageSize, searchQuery);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// Updates a specific approval by its ID.
    /// </summary>
    [HttpPut("{approvalId}")]
    [Authorize(PermissionKeys.CanEditApprovalWorkflow)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> UpdateApproval(
        [FromBody] CreateApprovalRequest request,
        Guid approvalId
    )
    {
        var userId = (string)HttpContext.Items["Sub"];
        if (userId == null)
            return TypedResults.Unauthorized();

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
        if (userId == null)
            return TypedResults.Unauthorized();

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
    public async Task<IResult> ApproveItem(
        string modelType,
        Guid modelId,
        [FromBody] ApprovalRequestBody body
    )
    {
        var userId = (string)HttpContext.Items["Sub"];
        var roleIds = (List<Guid>)HttpContext.Items["Roles"];

        if (userId == null)
            return TypedResults.Unauthorized();
        if (modelType == "ShipmentDiscrepancy" &&
            !(await authorization.AuthorizeAsync(User, PermissionKeys.CanApproveShipmentDiscrepancy)).Succeeded)
            return TypedResults.Forbid();
        if (modelType == "StockDiscrepancy" &&
            !(await authorization.AuthorizeAsync(User, PermissionKeys.CanApproveStockDiscrepancy)).Succeeded)
            return TypedResults.Forbid();
        if (modelType == nameof(DOMAIN.Entities.Forms.Response) &&
            !(await authorization.AuthorizeAsync(User, PermissionKeys.CanApproveResponse)).Succeeded)
            return TypedResults.Forbid();

        var actorId = Guid.Parse(userId);
        var comments = body.Comments;
        if (!QcWorksheetModelTypes.IsQcWorksheetModelType(modelType))
        {
            var assigned = (await delegations.Pending(repository, actorId, roleIds, modelType, DateTime.UtcNow))
                .FirstOrDefault(item => item.Entity.Id == modelId);
            if (assigned is null)
                return TypedResults.Forbid();
            // Waiting only for someone who delegated to the caller: decided with that person's user and
            // roles, so the document's own rules apply, and the comment records who decided.
            if (assigned.OnBehalfOf is not null)
            {
                comments = ApprovalDelegationService.OnBehalfComment(await delegations.NameOf(actorId), assigned.OnBehalfOf, comments);
                actorId = assigned.OnBehalfOf.UserId;
                roleIds = assigned.OnBehalfOf.RoleIds;
            }
        }

        var result = await repository.ApproveItem(
            modelType,
            modelId,
            actorId,
            roleIds,
            comments
        );
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
    public async Task<IResult> RejectItem(
        string modelType,
        Guid modelId,
        [FromBody] ApprovalRequestBody body
    )
    {
        var userId = (string)HttpContext.Items["Sub"];
        var roleIds = (List<Guid>)HttpContext.Items["Roles"];

        if (userId == null)
            return TypedResults.Unauthorized();
        if (modelType == "ShipmentDiscrepancy" &&
            !(await authorization.AuthorizeAsync(User, PermissionKeys.CanRejectShipmentDiscrepancy)).Succeeded)
            return TypedResults.Forbid();
        if (modelType == "StockDiscrepancy" &&
            !(await authorization.AuthorizeAsync(User, PermissionKeys.CanRejectStockDiscrepancy)).Succeeded)
            return TypedResults.Forbid();
        if (modelType == nameof(DOMAIN.Entities.Forms.Response) &&
            !(await authorization.AuthorizeAsync(User, PermissionKeys.CanRejectResponse)).Succeeded)
            return TypedResults.Forbid();

        var actorId = Guid.Parse(userId);
        var comments = body.Comments;
        if (!QcWorksheetModelTypes.IsQcWorksheetModelType(modelType))
        {
            var assigned = (await delegations.Pending(repository, actorId, roleIds, modelType, DateTime.UtcNow))
                .FirstOrDefault(item => item.Entity.Id == modelId);
            if (assigned is null)
                return TypedResults.Forbid();
            // Waiting only for someone who delegated to the caller: decided with that person's user and
            // roles, so the document's own rules apply, and the comment records who decided.
            if (assigned.OnBehalfOf is not null)
            {
                comments = ApprovalDelegationService.OnBehalfComment(await delegations.NameOf(actorId), assigned.OnBehalfOf, comments);
                actorId = assigned.OnBehalfOf.UserId;
                roleIds = assigned.OnBehalfOf.RoleIds;
            }
        }

        var result = await repository.RejectItem(
            modelType,
            modelId,
            actorId,
            roleIds,
            comments,
            body.ResponseRejectionOutcome
        );
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    /// <summary>
    /// Gets all items requiring approval by the current user.
    /// </summary>
    [HttpGet("my-pending")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<ApprovalEntity>))]
    public async Task<IResult> GetPendingApprovals(
        [FromQuery] string modelType,
        [FromQuery] Guid? userId
    )
    {
        var currentUserId = (string)HttpContext.Items["Sub"];
        var roleIds = (List<Guid>)HttpContext.Items["Roles"];

        if (currentUserId == null)
            return TypedResults.Unauthorized();

        // The queue is always scoped to the authenticated approver. A supplied
        // userId must never expose another user's assigned documents.
        if (userId.HasValue && userId.Value != Guid.Parse(currentUserId))
            return TypedResults.Forbid();

        var result = await delegations.Pending(repository, Guid.Parse(currentUserId), roleIds, modelType, DateTime.UtcNow);
        return TypedResults.Ok(result.Select(item => item.Entity).ToList());
    }

    /// <summary>
    /// Gets all items requiring approval by the current user.
    /// </summary>
    [HttpGet("my-pending/stats")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Dictionary<string, int>))]
    public async Task<IResult> GetPendingApprovalStatistics()
    {
        var userId = (string)HttpContext.Items["Sub"];
        var roleIds = (List<Guid>)HttpContext.Items["Roles"];

        if (userId == null)
            return TypedResults.Unauthorized();

        var pending = await delegations.Pending(repository, Guid.Parse(userId), roleIds, null, DateTime.UtcNow);
        return TypedResults.Ok(pending.GroupBy(item => item.Entity.ModelType).ToDictionary(group => group.Key, group => group.Count()));
    }
}
