using DOMAIN.Entities.Approvals;
using DOMAIN.Entities.Customers;
using DOMAIN.Entities.Forms;
using DOMAIN.Entities.JobRequests;
using DOMAIN.Entities.LeaveRequests;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.OvertimeRequests;
using DOMAIN.Entities.Payments;
using DOMAIN.Entities.Payroll;
using DOMAIN.Entities.Performance;
using DOMAIN.Entities.ProductionOrders;
using DOMAIN.Entities.ProductionSchedules;
using DOMAIN.Entities.Products.Production;
using DOMAIN.Entities.ProformaInvoices;
using DOMAIN.Entities.PurchaseOrders;
using DOMAIN.Entities.Procurement.Suppliers;
using DOMAIN.Entities.Requisitions;
using DOMAIN.Entities.RndProjects;
using DOMAIN.Entities.RndFormulations;
using DOMAIN.Entities.RndTechnologyTransfers;
using DOMAIN.Entities.Shipments;
using DOMAIN.Entities.StaffRequisitions;
using DOMAIN.Entities.StockAdjustments;
using INFRASTRUCTURE.Context;

namespace APP.Repository;

/// <summary>
/// Shared queue predicates for the list and statistics. Statistics project only document keys;
/// entity AutoIncludes and audit history must never be loaded just to count a queue.
/// </summary>
internal sealed class PendingApprovalQueries(ApplicationDbContext context, Guid userId, List<Guid> roleIds)
{
    internal IQueryable<StockDiscrepancy> StockDiscrepancies => context.StockDiscrepancies
        .Where(item => !item.Approved && !item.Rejected && item.Approvals.Any(stage =>
            stage.Status == ApprovalStatus.Pending && stage.ActivatedAt.HasValue &&
            (stage.UserId == userId ||
                (stage.RoleId.HasValue && roleIds.Contains(stage.RoleId.Value)))));

    internal IQueryable<ShipmentDiscrepancy> ShipmentDiscrepancies => context.ShipmentDiscrepancies
        .Where(item => !item.Approved && !item.Rejected && item.Approvals.Any(stage =>
            stage.Status == ApprovalStatus.Pending && stage.ActivatedAt.HasValue &&
            (stage.UserId == userId ||
                (stage.RoleId.HasValue && roleIds.Contains(stage.RoleId.Value)))));

    internal IQueryable<PurchaseOrder> PurchaseOrders => context.PurchaseOrders
        .Where(po =>
            po.Approvals.Any(a =>
                (a.UserId == userId || (a.RoleId.HasValue && roleIds.Contains(a.RoleId.Value)))
                && a.Status == ApprovalStatus.Pending
                && a.ActivatedAt.HasValue
            )
        );

    internal IQueryable<Requisition> Requisitions => context.Requisitions
        .Where(po =>
            po.Approvals.Any(a =>
                (a.UserId == userId || (a.RoleId.HasValue && roleIds.Contains(a.RoleId.Value)))
                && a.Status == ApprovalStatus.Pending
                && a.ActivatedAt.HasValue
            )
        );

    internal IQueryable<BillingSheet> BillingSheets => context.BillingSheets
        .Where(bs =>
            bs.Approvals.Any(a =>
                (a.UserId == userId || (a.RoleId.HasValue && roleIds.Contains(a.RoleId.Value)))
                && a.Status == ApprovalStatus.Pending
                && a.ActivatedAt.HasValue
            )
        );

    internal IQueryable<OvertimeRequest> OvertimeRequests => context.OvertimeRequests
        .Where(bs =>
            bs.Approvals.Any(a =>
                (a.UserId == userId || (a.RoleId.HasValue && roleIds.Contains(a.RoleId.Value)))
                && a.Status == ApprovalStatus.Pending
                && a.ActivatedAt.HasValue
            )
        );

    internal IQueryable<LeaveRequest> LeaveRequests => context.LeaveRequests
        .Where(bs =>
            bs.Approvals.Any(a =>
                (a.UserId == userId || (a.RoleId.HasValue && roleIds.Contains(a.RoleId.Value)))
                && a.Status == ApprovalStatus.Pending
                && a.ActivatedAt.HasValue
            )
        );

    internal IQueryable<PayrollRun> PayrollRuns => context.PayrollRuns
        .Where(bs =>
            bs.Approvals.Any(a =>
                (a.UserId == userId || (a.RoleId.HasValue && roleIds.Contains(a.RoleId.Value)))
                && a.Status == ApprovalStatus.Pending
                && a.ActivatedAt.HasValue
            )
        );

    internal IQueryable<PerformanceReview> PerformanceReviews => context.PerformanceReviews
        .Where(bs =>
            bs.Approvals.Any(a =>
                (a.UserId == userId || (a.RoleId.HasValue && roleIds.Contains(a.RoleId.Value)))
                && a.Status == ApprovalStatus.Pending
                && a.ActivatedAt.HasValue
            )
        );

    internal IQueryable<Response> Responses => context.Responses
        .Where(bs =>
            bs.Approvals.Any(a =>
                a.ApprovalRound == bs.Approvals.Max(item => item.ApprovalRound)
                &&
                (a.UserId == userId || (a.RoleId.HasValue && roleIds.Contains(a.RoleId.Value)))
                && a.Status == ApprovalStatus.Pending
                && a.ActivatedAt.HasValue
            )
        );

    internal IQueryable<ProformaInvoice> ProformaInvoices => context.ProformaInvoices
        .Where(bs =>
            bs.Approvals.Any(a =>
                (a.UserId == userId || (a.RoleId.HasValue && roleIds.Contains(a.RoleId.Value)))
                && a.Status == ApprovalStatus.Pending
                && a.ActivatedAt.HasValue
            )
        );

    internal IQueryable<ShipmentDocument> ShipmentDocuments => context.ShipmentDocuments
        .Where(bs =>
            bs.Approvals.Any(a =>
                (a.UserId == userId || (a.RoleId.HasValue && roleIds.Contains(a.RoleId.Value)))
                && a.Status == ApprovalStatus.Pending
                && a.ActivatedAt.HasValue
            )
        );

    internal IQueryable<JobRequest> JobRequests => context.JobRequests
        .Where(bs =>
            bs.Approvals.Any(a =>
                (a.UserId == userId || (a.RoleId.HasValue && roleIds.Contains(a.RoleId.Value)))
                && a.Status == ApprovalStatus.Pending
                && a.ActivatedAt.HasValue
            )
        );

    internal IQueryable<ProductionExtraPacking> ProductionExtraPackings => context.ProductionExtraPackings
        .Where(bs =>
            bs.Approvals.Any(a =>
                (a.UserId == userId || (a.RoleId.HasValue && roleIds.Contains(a.RoleId.Value)))
                && a.Status == ApprovalStatus.Pending
                && a.ActivatedAt.HasValue
            )
        );

    internal IQueryable<FinishedGoodsTransferNote> FinishedGoodsTransferNotes => context.FinishedGoodsTransferNotes
        .Where(bs =>
            bs.Approvals.Any(a =>
                (a.UserId == userId || (a.RoleId.HasValue && roleIds.Contains(a.RoleId.Value)))
                && a.Status == ApprovalStatus.Pending
                && a.ActivatedAt.HasValue
            )
        );

    internal IQueryable<StockAdjustment> StockAdjustments => context.StockAdjustments
        .Where(bs =>
            bs.Approvals.Any(a =>
                (a.UserId == userId || (a.RoleId.HasValue && roleIds.Contains(a.RoleId.Value)))
                && a.Status == ApprovalStatus.Pending
                && a.ActivatedAt.HasValue
            )
        );

    internal IQueryable<AllocateProductionOrder> AllocateProductionOrders => context.AllocateProductionOrders
        .Where(item => item.Approvals.Any(stage =>
            (stage.UserId == userId
                || stage.RoleId.HasValue && roleIds.Contains(stage.RoleId.Value))
            && stage.Status == ApprovalStatus.Pending
            && stage.ActivatedAt.HasValue));

    internal IQueryable<CustomerQuotation> CustomerQuotations => context.CustomerQuotations
        .Where(item => item.Status == CustomerQuotationStatus.Sent
            && item.Approvals.Any(stage =>
                stage.ActivatedAt.HasValue
                && stage.Status == ApprovalStatus.Pending
                && (stage.UserId == userId
                    || stage.RoleId.HasValue && roleIds.Contains(stage.RoleId.Value))));

    internal IQueryable<SupplierPricingAgreement> SupplierPricingAgreements => context.SupplierPricingAgreements
        .Where(item => item.Status == SupplierPricingAgreementStatus.Pending
            && item.Approvals.Any(stage => stage.ActivatedAt.HasValue
                && stage.Status == ApprovalStatus.Pending
                && (stage.UserId == userId
                    || stage.RoleId.HasValue && roleIds.Contains(stage.RoleId.Value))));

    internal IQueryable<RndProject> RndProjects => context.RndProjects
        .Where(item =>
            item.Approvals.Any(a =>
                (a.UserId == userId || (a.RoleId.HasValue && roleIds.Contains(a.RoleId.Value)))
                && a.Status == ApprovalStatus.Pending
                && a.ActivatedAt.HasValue
            )
        );

    internal IQueryable<RndFormulation> RndFormulations => context.RndFormulations
        .Where(item => item.Status == RndFormulationStatus.InReview && item.Approvals.Any(stage =>
            stage.ActivatedAt.HasValue && stage.Status == ApprovalStatus.Pending
            && (stage.UserId == userId || stage.RoleId.HasValue && roleIds.Contains(stage.RoleId.Value))));

    internal IQueryable<RndTechnologyTransfer> RndTechnologyTransfers => context.RndTechnologyTransfers
        .Where(item => item.Status == RndTechnologyTransferStatus.ProtocolInReview
            && item.Approvals.Any(stage => stage.ActivatedAt.HasValue
                && stage.Status == ApprovalStatus.Pending
                && (stage.UserId == userId || stage.RoleId.HasValue && roleIds.Contains(stage.RoleId.Value))));

    internal IQueryable<StaffRequisition> StaffRequisitions => context.StaffRequisitions
        .Where(item => item.Approvals.Any(stage =>
            (stage.UserId == userId || stage.RoleId.HasValue && roleIds.Contains(stage.RoleId.Value))
            && stage.Status == ApprovalStatus.Pending && stage.ActivatedAt.HasValue));

    internal IQueryable<ProductionOrder> ProductionOrders => context.ProductionOrders
        .Where(item => item.Approvals.Any(stage =>
            (stage.UserId == userId || stage.RoleId.HasValue && roleIds.Contains(stage.RoleId.Value))
            && stage.Status == ApprovalStatus.Pending && stage.ActivatedAt.HasValue));

    internal IEnumerable<IQueryable<PendingApprovalKey>> Keys()
    {
        yield return StockDiscrepancies.Select(item => new PendingApprovalKey(nameof(StockDiscrepancy), item.Id));
        yield return ShipmentDiscrepancies.Select(item => new PendingApprovalKey(nameof(ShipmentDiscrepancy), item.Id));
        yield return PurchaseOrders.Select(item => new PendingApprovalKey(nameof(PurchaseOrder), item.Id));
        yield return Requisitions.Select(item => new PendingApprovalKey(item.RequisitionType == RequisitionType.Stock ? "StockRequisition"
                : item.RequisitionType == RequisitionType.Trial ? "TrialRequisition" : "PurchaseRequisition", item.Id));
        yield return BillingSheets.Select(item => new PendingApprovalKey(nameof(BillingSheet), item.Id));
        yield return OvertimeRequests.Select(item => new PendingApprovalKey(nameof(OvertimeRequest), item.Id));
        yield return LeaveRequests.Select(item => new PendingApprovalKey(nameof(LeaveRequest), item.Id));
        yield return PayrollRuns.Select(item => new PendingApprovalKey(nameof(PayrollRun), item.Id));
        yield return PerformanceReviews.Select(item => new PendingApprovalKey(nameof(PerformanceReview), item.Id));
        yield return Responses.Select(item => new PendingApprovalKey(nameof(Response), item.Id));
        yield return ProformaInvoices.Select(item => new PendingApprovalKey(nameof(ProformaInvoice), item.Id));
        yield return ShipmentDocuments.Select(item => new PendingApprovalKey(nameof(ShipmentDocument), item.Id));
        yield return JobRequests.Select(item => new PendingApprovalKey(nameof(JobRequest), item.Id));
        yield return ProductionExtraPackings.Select(item => new PendingApprovalKey(nameof(ProductionExtraPacking), item.Id));
        yield return FinishedGoodsTransferNotes.Select(item => new PendingApprovalKey(nameof(FinishedGoodsTransferNote), item.Id));
        yield return StockAdjustments.Select(item => new PendingApprovalKey(nameof(StockAdjustment), item.Id));
        yield return AllocateProductionOrders.Select(item => new PendingApprovalKey(nameof(AllocateProductionOrder), item.Id));
        yield return CustomerQuotations.Select(item => new PendingApprovalKey(nameof(CustomerQuotation), item.Id));
        yield return SupplierPricingAgreements.Select(item => new PendingApprovalKey(nameof(SupplierPricingAgreement), item.Id));
        yield return RndProjects.Select(item => new PendingApprovalKey(nameof(RndProject), item.Id));
        yield return RndFormulations.Select(item => new PendingApprovalKey(nameof(RndFormulation), item.Id));
        yield return RndTechnologyTransfers.Select(item => new PendingApprovalKey(nameof(RndTechnologyTransfer), item.Id));
        yield return StaffRequisitions.Select(item => new PendingApprovalKey(nameof(StaffRequisition), item.Id));
        yield return ProductionOrders.Select(item => new PendingApprovalKey(nameof(ProductionOrder), item.Id));
        yield return PaymentApprovalQueue.Query(context, userId, roleIds)
            .Select(item => new PendingApprovalKey(nameof(Payment), item.Id));
    }
}
