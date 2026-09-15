using APP.Services.ProductionActivityStepEventPublisher;
using DOMAIN.Entities.Approvals;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Customers;
using DOMAIN.Entities.Forms;
using DOMAIN.Entities.JobRequests;
using DOMAIN.Entities.LeaveRequests;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.OvertimeRequests;
using DOMAIN.Entities.Payroll;
using DOMAIN.Entities.Performance;
using DOMAIN.Entities.ProductionOrders;
using DOMAIN.Entities.ProductionSchedules;
using DOMAIN.Entities.ProformaInvoices;
using DOMAIN.Entities.PurchaseOrders;
using DOMAIN.Entities.Requisitions;
using DOMAIN.Entities.RndProjects;
using DOMAIN.Entities.RndFormulations;
using DOMAIN.Entities.RndTechnologyTransfers;
using DOMAIN.Entities.Shipments;
using DOMAIN.Entities.StaffRequisitions;
using DOMAIN.Entities.StockAdjustments;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

internal static class AutomaticApprovalProcessor
{
    internal static async Task ApplyAsync(
        ApplicationDbContext context,
        string modelType,
        Guid modelId,
        string reason,
        IProductionActivityStepEventPublisher stepEventPublisher)
    {
        switch (modelType)
        {
            case "RawStockRequisition":
            case "PackageStockRequisition":
            case "PurchaseRequisition":
            case "TrialRequisition":
            case "Requisition":
                var requisition = await context.Requisitions
                    .Include(item => item.Items)
                    .SingleOrDefaultAsync(item => item.Id == modelId);
                EnsureFound(requisition, modelType, modelId);
                requisition.Approved = true;
                requisition.Status = RequestStatus.Pending;
                foreach (var item in requisition.Items)
                    item.Status = RequestStatus.Pending;
                break;

            case nameof(BillingSheet):
                var billingSheet = await context.BillingSheets
                    .SingleOrDefaultAsync(item => item.Id == modelId);
                EnsureFound(billingSheet, modelType, modelId);
                billingSheet.Approved = true;
                billingSheet.Status = BillingSheetStatus.Pending;
                break;

            case nameof(PurchaseOrder):
                var purchaseOrder = await context.PurchaseOrders
                    .SingleOrDefaultAsync(item => item.Id == modelId);
                EnsureFound(purchaseOrder, modelType, modelId);
                purchaseOrder.Approved = true;
                purchaseOrder.Status = PurchaseOrderStatus.Approved;
                break;

            case nameof(LeaveRequest):
                var leaveRequest = await context.LeaveRequests
                    .SingleOrDefaultAsync(item => item.Id == modelId);
                EnsureFound(leaveRequest, modelType, modelId);
                leaveRequest.Approved = true;
                leaveRequest.LeaveStatus = LeaveStatus.Approved;
                break;

            case nameof(OvertimeRequest):
                var overtimeRequest = await context.OvertimeRequests
                    .SingleOrDefaultAsync(item => item.Id == modelId);
                EnsureFound(overtimeRequest, modelType, modelId);
                overtimeRequest.Approved = true;
                overtimeRequest.Status = OvertimeStatus.Approved;
                overtimeRequest.ApprovalStatus = ApprovalStatus.Approved;
                break;

            case nameof(PayrollRun):
                var payrollRun = await context.PayrollRuns
                    .SingleOrDefaultAsync(item => item.Id == modelId);
                EnsureFound(payrollRun, modelType, modelId);
                payrollRun.Approved = true;
                payrollRun.Status = PayrollRunStatus.Approved;
                break;

            case nameof(Response):
                var response = await context.Responses
                    .Include(item => item.MaterialBatch)
                    .Include(item => item.BatchManufacturingRecord)
                        .ThenInclude(item => item.ProductionScheduleProduct)
                    .SingleOrDefaultAsync(item => item.Id == modelId);
                EnsureFound(response, modelType, modelId);
                var result = await ResponseFinalApproval.ApplyAsync(context, response, null);
                if (!result.IsSuccess)
                    throw new InvalidOperationException(result.Error.Description);
                if (response.ProductionActivityStepId.HasValue)
                {
                    await stepEventPublisher.PublishStatusChanged(
                        response.ProductionActivityStepId.Value,
                        ProductionStatus.Completed,
                        null
                    );
                }
                break;

            case nameof(ProformaInvoice):
                var invoice = await context.ProformaInvoices
                    .Include(item => item.AllocateProductionOrder)
                    .SingleOrDefaultAsync(item => item.Id == modelId);
                EnsureFound(invoice, modelType, modelId);
                invoice.Approved = true;
                if (invoice.AllocateProductionOrder is not null)
                    invoice.AllocateProductionOrder.Approved = true;
                break;

            case nameof(ShipmentDocument):
                var shipment = await context.ShipmentDocuments
                    .SingleOrDefaultAsync(item => item.Id == modelId);
                EnsureFound(shipment, modelType, modelId);
                shipment.Approved = true;
                break;

            case nameof(AllocateProductionOrder):
                var allocation = await context.AllocateProductionOrders
                    .SingleOrDefaultAsync(item => item.Id == modelId);
                EnsureFound(allocation, modelType, modelId);
                allocation.Approved = true;
                break;

            case nameof(StaffRequisition):
                var staff = await context.StaffRequisitions
                    .SingleOrDefaultAsync(item => item.Id == modelId);
                EnsureFound(staff, modelType, modelId);
                staff.Approved = true;
                staff.StaffRequisitionStatus = StaffRequisitionStatus.Approved;
                break;

            case nameof(ProductionOrder):
                var productionOrder = await context.ProductionOrders
                    .SingleOrDefaultAsync(item => item.Id == modelId);
                EnsureFound(productionOrder, modelType, modelId);
                productionOrder.Approved = true;
                break;

            case nameof(CustomerQuotation):
                var quotation = await context.CustomerQuotations
                    .SingleOrDefaultAsync(item => item.Id == modelId);
                EnsureFound(quotation, modelType, modelId);
                quotation.Approved = true;
                quotation.Status = CustomerQuotationStatus.Accepted;
                break;

            case nameof(RndProject):
                var rndProject = await context.RndProjects
                    .SingleOrDefaultAsync(item => item.Id == modelId);
                EnsureFound(rndProject, modelType, modelId);
                rndProject.Approved = true;
                rndProject.Status = RndProjectStatus.InDevelopment;
                break;

            case nameof(RndFormulation):
                var formulation = await context.RndFormulations
                    .SingleOrDefaultAsync(item => item.Id == modelId);
                EnsureFound(formulation, modelType, modelId);
                formulation.Approved = true;
                formulation.Status = RndFormulationStatus.Approved;
                break;

            case nameof(RndTechnologyTransfer):
                var rndTransfer = await context.RndTechnologyTransfers
                    .SingleOrDefaultAsync(item => item.Id == modelId);
                EnsureFound(rndTransfer, modelType, modelId);
                rndTransfer.Approved = true;
                rndTransfer.Status = RndTechnologyTransferStatus.ProtocolApproved;
                rndTransfer.ProtocolApprovedAt = DateTime.UtcNow;
                rndTransfer.ProtocolApprovalComments = reason;
                break;

            case nameof(JobRequest):
                var job = await context.JobRequests
                    .SingleOrDefaultAsync(item => item.Id == modelId);
                EnsureFound(job, modelType, modelId);
                job.Approved = true;
                break;

            case nameof(ProductionExtraPacking):
                var extraPacking = await context.ProductionExtraPackings
                    .SingleOrDefaultAsync(item => item.Id == modelId);
                EnsureFound(extraPacking, modelType, modelId);
                extraPacking.Approved = true;
                extraPacking.Status = ProductionExtraPackingStatus.InProgress;
                break;

            case nameof(FinishedGoodsTransferNote):
                var transferNote = await context.FinishedGoodsTransferNotes
                    .SingleOrDefaultAsync(item => item.Id == modelId);
                EnsureFound(transferNote, modelType, modelId);
                transferNote.Approved = true;
                break;

            case nameof(StockAdjustment):
                var adjustment = await context.StockAdjustments
                    .SingleOrDefaultAsync(item => item.Id == modelId);
                EnsureFound(adjustment, modelType, modelId);
                adjustment.Approved = true;
                break;

            case nameof(PerformanceReview):
                var performanceReview = await context.PerformanceReviews
                    .SingleOrDefaultAsync(item => item.Id == modelId);
                EnsureFound(performanceReview, modelType, modelId);
                performanceReview.Approved = true;
                performanceReview.Status = PerformanceReviewStatus.Completed;
                break;

            default:
                throw new NotSupportedException(
                    $"Automatic approval is not supported for model type '{modelType}'.");
        }

        await context.ApprovalActionLogs.AddAsync(new ApprovalActionLog
        {
            ModelId = modelId,
            Status = ApprovalStatus.Approved,
            Comments = reason,
        });
        await context.SaveChangesAsync();
    }

    private static void EnsureFound(object item, string modelType, Guid modelId)
    {
        if (item is null)
            throw new InvalidOperationException($"{modelType} {modelId} was not found.");
    }
}
