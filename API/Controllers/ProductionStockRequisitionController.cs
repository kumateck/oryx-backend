using APP.Extensions;
using APP.IRepository;
using APP.Services.Approvals;
using APP.Utils;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Requisitions;
using INFRASTRUCTURE.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SHARED;
namespace API.Controllers;
[ApiController]
[Route("api/v{version:apiVersion}/production-stock-requisitions/{requisitionId:guid}")]
public sealed class ProductionStockRequisitionController(ApplicationDbContext db, IApprovalRepository repository,
    IApprovalDelegationService delegations) : ControllerBase
{
    [HttpPost("raw/reject", Name = "RejectProductionRawStockRequisition")]
    [Authorize(PermissionKeys.CanRejectRawMaterialStockRequisition)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public Task<IResult> Raw(Guid requisitionId, RejectProductionStockRequisitionRequest request) => Run(requisitionId, MaterialKind.Raw, request);
    [HttpPost("packaging/reject", Name = "RejectProductionPackagingStockRequisition")]
    [Authorize(PermissionKeys.CanRejectPackagingMaterialStockRequisition)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public Task<IResult> Packaging(Guid requisitionId, RejectProductionStockRequisitionRequest request) => Run(requisitionId, MaterialKind.Package, request);
    private async Task<IResult> Run(Guid id, MaterialKind kind, RejectProductionStockRequisitionRequest request)
    {
        if (!Guid.TryParse(HttpContext.Items["Sub"] as string, out var actor)) return TypedResults.Unauthorized();
        var kinds = await db.RequisitionItems.Where(item => item.RequisitionId == id
            && item.Requisition.RequisitionType == RequisitionType.Stock && item.Requisition.ProductionScheduleProductId != null)
            .Select(item => item.Material.Kind).Distinct().ToListAsync();
        if (kinds.Count != 1 || kinds[0] != kind) return TypedResults.Forbid();
        var roles = await db.UserRoles.Where(role => role.UserId == actor).Select(role => role.RoleId).ToListAsync();
        var assigned = (await delegations.Pending(repository, actor, roles, "StockRequisition", DateTime.UtcNow)).FirstOrDefault(item => item.Entity.Id == id);
        if (assigned == null) return TypedResults.Forbid();
        var reason = request.Reason.Trim();
        if (assigned.OnBehalfOf != null)
        {
            reason = ApprovalDelegationService.OnBehalfComment(await delegations.NameOf(actor), assigned.OnBehalfOf, reason);
            actor = assigned.OnBehalfOf.UserId; roles = assigned.OnBehalfOf.RoleIds;
        }
        try
        {
            var result = await repository.RejectItem("StockRequisition", id, actor, roles, reason, contentRevision: request.ContentRevision);
            return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
        }
        catch (DbUpdateException e) when (e.InnerException is PostgresException { SqlState: "40001" or "40P01" or "55P03" }) { return Changed(); }
        catch (InvalidOperationException e) when (e.InnerException is DbUpdateException { InnerException: PostgresException { SqlState: "40001" or "40P01" or "55P03" } }) { return Changed(); }
        catch (PostgresException e) when (e.SqlState is "40001" or "40P01" or "55P03") { return Changed(); }
    }
    private static IResult Changed() => Result.Failure(Error.Conflict("StockRequisition.ConcurrentChange", "The requisition changed. Reload its current details before deciding.")).ToProblemDetails();
}
