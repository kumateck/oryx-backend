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
internal sealed class PendingApprovalQueries(ApplicationDbContext context, Guid userId, List<Guid> roleIds, ApprovalDepartmentScope scope = null)
{
    private readonly ApprovalDepartmentScope departmentScope = scope ?? ApprovalDepartmentScope.None;

    /// <summary>Queries for this user, limited to their own production department where <see cref="ApprovalDepartmentScope"/> applies.</summary>
    internal static async Task<PendingApprovalQueries> For(ApplicationDbContext context, Guid userId, List<Guid> roleIds) =>
        new(context, userId, roleIds, await ApprovalDepartmentScope.For(context, userId, roleIds));

    internal IQueryable<StockDiscrepancy> StockDiscrepancies => departmentScope.Apply(context.StockDiscrepancies
        .Where(item => !item.Approved && !item.Rejected && item.Approvals.Any(stage =>
            stage.Status == ApprovalStatus.Pending && stage.ActivatedAt.HasValue &&
            (stage.UserId == userId ||
                (stage.RoleId.HasValue && roleIds.Contains(stage.RoleId.Value))))),
        x => x.Approvals, x => x.CreatedBy.Department);

    internal IQueryable<StockRequisitionIssue> StockRequisitionIssues => departmentScope.Apply(context.StockRequisitionIssues
        .Where(item => !item.Approved && !item.Rejected && item.Approvals.Any(stage =>
            stage.Status == ApprovalStatus.Pending && stage.ActivatedAt.HasValue &&
            (stage.UserId == userId ||
                (stage.RoleId.HasValue && roleIds.Contains(stage.RoleId.Value))))),
        x => x.Approvals, x => x.IssuedBy.Department);

    internal IQueryable<ShipmentDiscrepancy> ShipmentDiscrepancies => departmentScope.Apply(context.ShipmentDiscrepancies
        .Where(item => !item.Approved && !item.Rejected && item.Approvals.Any(stage =>
            stage.Status == ApprovalStatus.Pending && stage.ActivatedAt.HasValue &&
            (stage.UserId == userId ||
                (stage.RoleId.HasValue && roleIds.Contains(stage.RoleId.Value))))),
        x => x.Approvals, x => x.CreatedBy.Department);

    internal IQueryable<PurchaseOrder> PurchaseOrders => departmentScope.Apply(context.PurchaseOrders
        .Where(po =>
            po.Approvals.Any(a =>
                (a.UserId == userId || (a.RoleId.HasValue && roleIds.Contains(a.RoleId.Value)))
                && a.Status == ApprovalStatus.Pending
                && a.ActivatedAt.HasValue
            )
        ),
        x => x.Approvals, x => x.CreatedBy.Department);

    internal IQueryable<Requisition> Requisitions => departmentScope.Apply(context.Requisitions
        .Where(po =>
            po.Approvals.Any(a =>
                (a.UserId == userId || (a.RoleId.HasValue && roleIds.Contains(a.RoleId.Value)))
                && a.Status == ApprovalStatus.Pending
                && a.ActivatedAt.HasValue
            )
        ),
        x => x.Approvals, x => x.CreatedBy.Department);

    internal IQueryable<BillingSheet> BillingSheets => departmentScope.Apply(context.BillingSheets
        .Where(bs =>
            bs.Approvals.Any(a =>
                (a.UserId == userId || (a.RoleId.HasValue && roleIds.Contains(a.RoleId.Value)))
                && a.Status == ApprovalStatus.Pending
                && a.ActivatedAt.HasValue
            )
        ),
        x => x.Approvals, x => x.CreatedBy.Department);

    internal IQueryable<OvertimeRequest> OvertimeRequests => departmentScope.Apply(context.OvertimeRequests
        .Where(bs =>
            bs.Approvals.Any(a =>
                (a.UserId == userId || (a.RoleId.HasValue && roleIds.Contains(a.RoleId.Value)))
                && a.Status == ApprovalStatus.Pending
                && a.ActivatedAt.HasValue
            )
        ),
        x => x.Approvals, x => x.CreatedBy.Department);

    internal IQueryable<LeaveRequest> LeaveRequests => departmentScope.Apply(context.LeaveRequests
        .Where(bs =>
            bs.Approvals.Any(a =>
                (a.UserId == userId || (a.RoleId.HasValue && roleIds.Contains(a.RoleId.Value)))
                && a.Status == ApprovalStatus.Pending
                && a.ActivatedAt.HasValue
            )
        ),
        x => x.Approvals, x => x.CreatedBy.Department);

    internal IQueryable<PayrollRun> PayrollRuns => departmentScope.Apply(context.PayrollRuns
        .Where(bs =>
            bs.Approvals.Any(a =>
                (a.UserId == userId || (a.RoleId.HasValue && roleIds.Contains(a.RoleId.Value)))
                && a.Status == ApprovalStatus.Pending
                && a.ActivatedAt.HasValue
            )
        ),
        x => x.Approvals, x => x.CreatedBy.Department);

    internal IQueryable<PerformanceReview> PerformanceReviews => departmentScope.Apply(context.PerformanceReviews
        .Where(bs =>
            bs.Approvals.Any(a =>
                (a.UserId == userId || (a.RoleId.HasValue && roleIds.Contains(a.RoleId.Value)))
                && a.Status == ApprovalStatus.Pending
                && a.ActivatedAt.HasValue
            )
        ),
        x => x.Approvals, x => x.Employee.Department);

    internal IQueryable<Response> Responses => departmentScope.Apply(context.Responses
        .Where(bs =>
            bs.Approvals.Any(a =>
                a.ApprovalRound == bs.Approvals.Max(item => item.ApprovalRound)
                &&
                (a.UserId == userId || (a.RoleId.HasValue && roleIds.Contains(a.RoleId.Value)))
                && a.Status == ApprovalStatus.Pending
                && a.ActivatedAt.HasValue
            )
        ),
        x => x.Approvals.Where(a => a.ApprovalRound == x.Approvals.Max(i => i.ApprovalRound)), x => x.CreatedBy.Department);

    internal IQueryable<ProformaInvoice> ProformaInvoices => departmentScope.Apply(context.ProformaInvoices
        .Where(bs =>
            bs.Approvals.Any(a =>
                (a.UserId == userId || (a.RoleId.HasValue && roleIds.Contains(a.RoleId.Value)))
                && a.Status == ApprovalStatus.Pending
                && a.ActivatedAt.HasValue
            )
        ),
        x => x.Approvals, x => x.CreatedBy.Department);

    internal IQueryable<ShipmentDocument> ShipmentDocuments => departmentScope.Apply(context.ShipmentDocuments
        .Where(bs =>
            bs.Approvals.Any(a =>
                (a.UserId == userId || (a.RoleId.HasValue && roleIds.Contains(a.RoleId.Value)))
                && a.Status == ApprovalStatus.Pending
                && a.ActivatedAt.HasValue
            )
        ),
        x => x.Approvals, x => x.CreatedBy.Department);

    internal IQueryable<JobRequest> JobRequests => departmentScope.Apply(context.JobRequests
        .Where(bs =>
            bs.Approvals.Any(a =>
                (a.UserId == userId || (a.RoleId.HasValue && roleIds.Contains(a.RoleId.Value)))
                && a.Status == ApprovalStatus.Pending
                && a.ActivatedAt.HasValue
            )
        ),
        x => x.Approvals, x => x.CreatedBy.Department);

    internal IQueryable<ProductionExtraPacking> ProductionExtraPackings => departmentScope.Apply(context.ProductionExtraPackings
        .Where(bs =>
            bs.Approvals.Any(a =>
                (a.UserId == userId || (a.RoleId.HasValue && roleIds.Contains(a.RoleId.Value)))
                && a.Status == ApprovalStatus.Pending
                && a.ActivatedAt.HasValue
            )
        ),
        x => x.Approvals, x => x.CreatedBy.Department);

    internal IQueryable<FinishedGoodsTransferNote> FinishedGoodsTransferNotes => departmentScope.Apply(context.FinishedGoodsTransferNotes
        .Where(bs =>
            bs.Approvals.Any(a =>
                (a.UserId == userId || (a.RoleId.HasValue && roleIds.Contains(a.RoleId.Value)))
                && a.Status == ApprovalStatus.Pending
                && a.ActivatedAt.HasValue
            )
        ),
        x => x.Approvals, x => x.CreatedBy.Department);

    internal IQueryable<StockAdjustment> StockAdjustments => departmentScope.Apply(context.StockAdjustments
        .Where(bs =>
            bs.Approvals.Any(a =>
                (a.UserId == userId || (a.RoleId.HasValue && roleIds.Contains(a.RoleId.Value)))
                && a.Status == ApprovalStatus.Pending
                && a.ActivatedAt.HasValue
            )
        ),
        x => x.Approvals, x => x.CreatedBy.Department);

    internal IQueryable<AllocateProductionOrder> AllocateProductionOrders => departmentScope.Apply(context.AllocateProductionOrders
        .Where(item => item.Approvals.Any(stage =>
            (stage.UserId == userId
                || stage.RoleId.HasValue && roleIds.Contains(stage.RoleId.Value))
            && stage.Status == ApprovalStatus.Pending
            && stage.ActivatedAt.HasValue)),
        x => x.Approvals, x => x.CreatedBy.Department);

    internal IQueryable<CustomerQuotation> CustomerQuotations => departmentScope.Apply(context.CustomerQuotations
        .Where(item => item.Status == CustomerQuotationStatus.Sent
            && item.Approvals.Any(stage =>
                stage.ActivatedAt.HasValue
                && stage.Status == ApprovalStatus.Pending
                && (stage.UserId == userId
                    || stage.RoleId.HasValue && roleIds.Contains(stage.RoleId.Value)))),
        x => x.Approvals, x => x.CreatedBy.Department);

    internal IQueryable<SupplierPricingAgreement> SupplierPricingAgreements => departmentScope.Apply(context.SupplierPricingAgreements
        .Where(item => item.Status == SupplierPricingAgreementStatus.Pending
            && item.Approvals.Any(stage => stage.ActivatedAt.HasValue
                && stage.Status == ApprovalStatus.Pending
                && (stage.UserId == userId
                    || stage.RoleId.HasValue && roleIds.Contains(stage.RoleId.Value)))),
        x => x.Approvals, x => x.CreatedBy.Department);

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

    internal IQueryable<StaffRequisition> StaffRequisitions => departmentScope.Apply(context.StaffRequisitions
        .Where(item => item.Approvals.Any(stage =>
            (stage.UserId == userId || stage.RoleId.HasValue && roleIds.Contains(stage.RoleId.Value))
            && stage.Status == ApprovalStatus.Pending && stage.ActivatedAt.HasValue)),
        x => x.Approvals, x => x.CreatedBy.Department);

    internal IQueryable<ProductionOrder> ProductionOrders => departmentScope.Apply(context.ProductionOrders
        .Where(item => item.Approvals.Any(stage =>
            (stage.UserId == userId || stage.RoleId.HasValue && roleIds.Contains(stage.RoleId.Value))
            && stage.Status == ApprovalStatus.Pending && stage.ActivatedAt.HasValue)),
        x => x.Approvals, x => x.CreatedBy.Department);

    internal IQueryable<Payment> Payments => departmentScope.Apply(PaymentApprovalQueue.Query(context, userId, roleIds),
        x => x.Approvals, x => x.RecordedBy.Department);

    internal IEnumerable<IQueryable<PendingApprovalKey>> Keys()
    {
        yield return StockDiscrepancies.Select(item => new PendingApprovalKey(nameof(StockDiscrepancy), item.Id));
        yield return ShipmentDiscrepancies.Select(item => new PendingApprovalKey(nameof(ShipmentDiscrepancy), item.Id));
        yield return StockRequisitionIssues.Select(item => new PendingApprovalKey(nameof(StockRequisitionIssue), item.Id));
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
        yield return Payments.Select(item => new PendingApprovalKey(nameof(Payment), item.Id));
    }
}
