using APP.Extensions;
using DOMAIN.Entities.Approvals;
using Xunit;

namespace APP.Tests.Repository;

public class ApprovalWorkflowGuardTests
{
    public static TheoryData<Type> ConfigurableDocumentTypes =>
        new()
        {
            typeof(DOMAIN.Entities.Requisitions.Requisition),
            typeof(DOMAIN.Entities.PurchaseOrders.BillingSheet),
            typeof(DOMAIN.Entities.PurchaseOrders.PurchaseOrder),
            typeof(DOMAIN.Entities.LeaveRequests.LeaveRequest),
            typeof(DOMAIN.Entities.OvertimeRequests.OvertimeRequest),
            typeof(DOMAIN.Entities.Payroll.PayrollRun),
            typeof(DOMAIN.Entities.Performance.PerformanceReview),
            typeof(DOMAIN.Entities.Forms.Response),
            typeof(DOMAIN.Entities.ProformaInvoices.ProformaInvoice),
            typeof(DOMAIN.Entities.Shipments.ShipmentDocument),
            typeof(DOMAIN.Entities.ProductionOrders.AllocateProductionOrder),
            typeof(DOMAIN.Entities.StaffRequisitions.StaffRequisition),
            typeof(DOMAIN.Entities.ProductionOrders.ProductionOrder),
            typeof(DOMAIN.Entities.RndProjects.RndProject),
            typeof(DOMAIN.Entities.JobRequests.JobRequest),
            typeof(DOMAIN.Entities.ProductionSchedules.ProductionExtraPacking),
            typeof(DOMAIN.Entities.Materials.Batch.FinishedGoodsTransferNote),
            typeof(DOMAIN.Entities.StockAdjustments.StockAdjustment),
            typeof(DOMAIN.Entities.Payments.Payment),
        };

    [Theory]
    [MemberData(nameof(ConfigurableDocumentTypes))]
    public void ConfigurableApprovalDocument_UsesSharedApprovalContract(Type documentType)
    {
        Assert.True(typeof(IRequireApproval).IsAssignableFrom(documentType));
    }

    [Fact]
    public void PendingDocument_CannotProceed()
    {
        var result = new ApprovalDocumentStub { Approved = false }
            .EnsureApprovedForProgression("Test document");

        Assert.True(result.IsFailure);
        Assert.Equal("Approval.Required", result.Error.Code);
    }

    [Fact]
    public void ApprovedDocument_CanProceed()
    {
        var result = new ApprovalDocumentStub { Approved = true }
            .EnsureApprovedForProgression("Test document");

        Assert.True(result.IsSuccess);
    }

    private sealed class ApprovalDocumentStub : IRequireApproval
    {
        public bool Approved { get; set; }
    }
}
