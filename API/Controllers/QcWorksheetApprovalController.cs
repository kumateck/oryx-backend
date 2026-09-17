using APP.Extensions;
using APP.IRepository;
using APP.Utils;
using DOMAIN.Entities.QcWorksheets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

/// <summary>
/// The centralized QC approvals queue: every pending QC approval across every entity type
/// in one place, which is the point of keeping QC approvals in a single table. This list
/// grows as later milestones add Specification, WorksheetInstance and OosCase.
/// <para>
/// Approving and rejecting happen on each resource's own re-authenticating endpoint, whose
/// route is carried on each row.
/// </para>
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/qc/worksheets/approvals")]
[Authorize]
public class QcWorksheetApprovalController(IQcApprovalRepository repository) : ControllerBase
{
    /// <summary>Every QC approval currently awaiting the calling user, across all QC entity types.</summary>
    [HttpGet("my-pending")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<QcPendingApprovalDto>))]
    public async Task<IResult> GetMyPending()
    {
        var userId = (string)HttpContext.Items["Sub"];
        var roleIds = (List<Guid>)HttpContext.Items["Roles"];
        if (userId is null) return TypedResults.Unauthorized();

        var result = await repository.GetPendingApprovals(Guid.Parse(userId), roleIds ?? []);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>
    /// The full signature trail for one QC document, including when each approver
    /// re-authenticated.
    /// </summary>
    [HttpGet("{entityType}/{entityId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<QcApprovalDto>))]
    public async Task<IResult> GetForEntity(
        [FromRoute] string entityType, [FromRoute] Guid entityId)
    {
        var result = await repository.GetApprovalsForEntity(entityType, entityId);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
}
