using APP.Extensions;
using APP.IRepository;
using APP.Services.Approvals;
using APP.Utils;
using INFRASTRUCTURE.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/approval/my-pending/document-counts")]
[Authorize]
public sealed class PendingApprovalDocumentCountsController(
    IApprovalRepository approvals, IQcApprovalRepository qcApprovals,
    ApplicationDbContext context, IAuthorizationService authorization) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Dictionary<string, int>))]
    public async Task<IResult> Get(CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(HttpContext.Items["Sub"] as string, out var actorId))
            return TypedResults.Unauthorized();
        var roleIds = HttpContext.Items["Roles"] as List<Guid> ?? [];
        async Task<bool> Can(string permission) =>
            (await authorization.AuthorizeAsync(User, permission)).Succeeded;
        var permissions = new PendingRevisionPermissions(
            await Can(PermissionKeys.CanViewReviewFormulaRevision),
            await Can(PermissionKeys.CanViewApproveFormulaRevision),
            await Can(FullProcedurePermissionKeys.CanReviewTemplateRevision),
            await Can(FullProcedurePermissionKeys.CanPublishTemplateRevision),
            await Can(FullProcedurePermissionKeys.CanReviewProcedureRevision),
            await Can(FullProcedurePermissionKeys.CanApproveProcedureRevision),
            await Can(QcWorksheetPermissionKeys.CanDispositionQcOosCase));

        var counts = await approvals.GetStatisticsOfEntitiesRequiringApproval(actorId, roleIds);
        var qc = await qcApprovals.GetPendingApprovals(actorId, roleIds);
        if (!qc.IsSuccess) return qc.ToProblemDetails();
        foreach (var group in qc.Value.GroupBy(item => $"Qc{item.EntityType}"))
            counts[group.Key] = counts.GetValueOrDefault(group.Key) + group.Count();

        var revisions = await PendingRevisionApprovals.GetAsync(
            context, actorId, roleIds, permissions, cancellationToken);
        foreach (var group in revisions.GroupBy(item => item.ResourceType))
            counts[group.Key] = counts.GetValueOrDefault(group.Key) + group.Count();
        return TypedResults.Ok(counts);
    }
}
