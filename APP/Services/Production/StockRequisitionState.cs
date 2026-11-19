using DOMAIN.Entities.Approvals;
using DOMAIN.Entities.Requisitions;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
namespace APP.Services.Production;

/// <summary>Operational state also recognises legacy recorded rejections whose header was never updated.</summary>
public static class StockRequisitionState
{
    public static IQueryable<Requisition> Active(INFRASTRUCTURE.Context.ApplicationDbContext db) => db.Requisitions.Where(r =>
        r.RequisitionType == RequisitionType.Stock && r.Status != RequestStatus.Rejected && (
            r.Status == RequestStatus.Completed || r.Approved || r.Items.Any(item => item.QuantityReceived > 0)
            || db.StockRequisitionIssues.IgnoreQueryFilters().Any(issue => issue.RequisitionId == r.Id)
            || db.BinCardInformation.IgnoreQueryFilters().Any(entry => entry.RequisitionId == r.Id)
            || !r.Approvals.Any(stage => stage.ContentRevision == r.ContentRevision && stage.Status == ApprovalStatus.Rejected)));
}
