using APP.Services.Approvals;
using APP.Utils;
using INFRASTRUCTURE.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/approval/my-pending/revisions")]
[Authorize]
public sealed class PendingRevisionApprovalController(
    ApplicationDbContext context, IAuthorizationService authorization) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK,
        Type = typeof(IReadOnlyList<PendingRevisionApprovalDto>))]
    public async Task<IResult> Get([FromQuery] string? approvalDocument,
        CancellationToken cancellationToken)
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
        var rows = await PendingRevisionApprovals.GetAsync(
            context, actorId, roleIds, permissions, cancellationToken, approvalDocument);
        return TypedResults.Ok(rows);
    }
}
