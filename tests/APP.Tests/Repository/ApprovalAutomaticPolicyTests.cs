using APP.Repository;
using DOMAIN.Entities.Approvals;
using DOMAIN.Entities.Employees;
using DOMAIN.Entities.Forms;
using DOMAIN.Entities.JobRequests;
using DOMAIN.Entities.LeaveRequests;
using DOMAIN.Entities.LeaveTypes;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.OvertimeRequests;
using DOMAIN.Entities.Procurement.Suppliers;
using DOMAIN.Entities.ProductionOrders;
using DOMAIN.Entities.ProductionSchedules;
using DOMAIN.Entities.ProformaInvoices;
using DOMAIN.Entities.PurchaseOrders;
using DOMAIN.Entities.Shipments;
using DOMAIN.Entities.StaffRequisitions;
using DOMAIN.Entities.StockAdjustments;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Repository;

file class AutomaticPolicyCurrentUser : ICurrentUserService
{
    public Guid? UserId => null;
    public Guid? DepartmentId => null;
    public string DepartmentType => string.Empty;
}

public class ApprovalAutomaticPolicyTests
{
    [Fact]
    public async Task CreateInitialApprovalsAsync_AppliesFinalLeaveAndOvertimeStates()
    {
        await using var context = CreateContext();
        var employee = new Employee { Id = Guid.NewGuid() };
        var leaveType = new LeaveType { Id = Guid.NewGuid() };
        var leave = new LeaveRequest
        {
            Id = Guid.NewGuid(),
            EmployeeId = employee.Id,
            Employee = employee,
            LeaveTypeId = leaveType.Id,
            LeaveType = leaveType,
            LeaveStatus = LeaveStatus.Pending,
        };
        var overtime = new OvertimeRequest
        {
            Id = Guid.NewGuid(),
            DepartmentId = Guid.NewGuid(),
            Status = OvertimeStatus.Pending,
            ApprovalStatus = ApprovalStatus.Pending,
        };
        context.AddRange(employee, leaveType, leave, overtime);
        await context.SaveChangesAsync();
        var repository = CreateRepository(context);

        await repository.CreateInitialApprovalsAsync(nameof(LeaveRequest), leave.Id);
        await repository.CreateInitialApprovalsAsync(nameof(OvertimeRequest), overtime.Id);

        Assert.True(leave.Approved);
        Assert.Equal(LeaveStatus.Approved, leave.LeaveStatus);
        Assert.True(overtime.Approved);
        Assert.Equal(OvertimeStatus.Approved, overtime.Status);
        Assert.Equal(ApprovalStatus.Approved, overtime.ApprovalStatus);
        Assert.Equal(2, context.ApprovalActionLogs.Count());
        Assert.All(context.ApprovalActionLogs, item => Assert.Null(item.UserId));
    }

    [Fact]
    public async Task CreateInitialApprovalsAsync_AutoApprovesAllocation_WhenWorkflowIsMissing()
    {
        await using var context = CreateContext();
        var allocation = new AllocateProductionOrder
        {
            Id = Guid.NewGuid(),
            ProductionOrderId = Guid.NewGuid(),
        };
        context.AllocateProductionOrders.Add(allocation);
        await context.SaveChangesAsync();

        await CreateRepository(context).CreateInitialApprovalsAsync(
            nameof(AllocateProductionOrder), allocation.Id);

        Assert.True(allocation.Approved);
        Assert.Empty(context.AllocateProductionOrderApprovals);
        Assert.Single(context.ApprovalActionLogs);
    }

    [Fact]
    public async Task CreateInitialApprovalsAsync_CreatesAndCompletesConfiguredAllocationApproval()
    {
        await using var context = CreateContext();
        var approverId = Guid.NewGuid();
        var allocation = new AllocateProductionOrder
        {
            Id = Guid.NewGuid(),
            ProductionOrderId = Guid.NewGuid(),
        };
        var approval = new Approval
        {
            Id = Guid.NewGuid(),
            ItemType = nameof(AllocateProductionOrder),
            ApprovalStages = [],
        };
        approval.ApprovalStages.Add(new ApprovalStage
        {
            Id = Guid.NewGuid(),
            ApprovalId = approval.Id,
            Approval = approval,
            Order = 1,
            Required = true,
            UserId = approverId,
        });
        context.AddRange(allocation, approval);
        await context.SaveChangesAsync();
        var repository = CreateRepository(context);

        await repository.CreateInitialApprovalsAsync(
            nameof(AllocateProductionOrder), allocation.Id);
        var pending = Assert.Single(context.AllocateProductionOrderApprovals);
        Assert.Equal(ApprovalStatus.Pending, pending.Status);

        var result = await repository.ApproveItem(
            nameof(AllocateProductionOrder),
            allocation.Id,
            approverId,
            [Guid.NewGuid()],
            "Approved for dispatch");

        Assert.True(result.IsSuccess);
        Assert.True(allocation.Approved);
        Assert.Equal(ApprovalStatus.Approved, pending.Status);
        Assert.Equal(approverId, pending.ApprovedById);
        Assert.Single(context.ApprovalActionLogs);
    }

    [Fact]
    public async Task CreateInitialApprovalsAsync_AutoApprovesStaffRequisition_WhenWorkflowIsMissing()
    {
        // Regression: StaffRequisition implements IRequireApproval and has full
        // approve/reject support, but nothing ever called CreateInitialApprovalsAsync
        // for it - every staff requisition would have zero approval records and
        // never appear anywhere a reviewer could act on it, configured workflow or not.
        await using var context = CreateContext();
        var staffRequisition = new StaffRequisition
        {
            Id = Guid.NewGuid(),
            DepartmentId = Guid.NewGuid(),
            DesignationId = Guid.NewGuid(),
        };
        context.StaffRequisitions.Add(staffRequisition);
        await context.SaveChangesAsync();

        await CreateRepository(context).CreateInitialApprovalsAsync(
            nameof(StaffRequisition), staffRequisition.Id);

        Assert.True(staffRequisition.Approved);
        Assert.Equal(StaffRequisitionStatus.Approved, staffRequisition.StaffRequisitionStatus);
        Assert.Empty(context.StaffRequisitionApprovals);
        Assert.Single(context.ApprovalActionLogs);
    }

    [Fact]
    public async Task CreateInitialApprovalsAsync_CreatesConfiguredStaffRequisitionApproval()
    {
        await using var context = CreateContext();
        var approverId = Guid.NewGuid();
        var staffRequisition = new StaffRequisition
        {
            Id = Guid.NewGuid(),
            DepartmentId = Guid.NewGuid(),
            DesignationId = Guid.NewGuid(),
        };
        var approval = new Approval
        {
            Id = Guid.NewGuid(),
            ItemType = nameof(StaffRequisition),
            ApprovalStages = [],
        };
        approval.ApprovalStages.Add(new ApprovalStage
        {
            Id = Guid.NewGuid(),
            ApprovalId = approval.Id,
            Approval = approval,
            Order = 1,
            Required = true,
            UserId = approverId,
        });
        context.AddRange(staffRequisition, approval);
        await context.SaveChangesAsync();

        await CreateRepository(context).CreateInitialApprovalsAsync(
            nameof(StaffRequisition), staffRequisition.Id);

        var pending = Assert.Single(context.StaffRequisitionApprovals);
        Assert.Equal(ApprovalStatus.Pending, pending.Status);
        Assert.Equal(approverId, pending.UserId);
        Assert.False(staffRequisition.Approved);
        Assert.Equal(StaffRequisitionStatus.Pending, staffRequisition.StaffRequisitionStatus);
    }

    [Fact]
    public async Task CreateInitialApprovalsAsync_CreatesConfiguredLeaveRequestApproval()
    {
        await using var context = CreateContext();
        var approverId = Guid.NewGuid();
        var employee = new Employee { Id = Guid.NewGuid() };
        var leaveType = new LeaveType { Id = Guid.NewGuid() };
        var leave = new LeaveRequest
        {
            Id = Guid.NewGuid(),
            EmployeeId = employee.Id,
            Employee = employee,
            LeaveTypeId = leaveType.Id,
            LeaveType = leaveType,
            LeaveStatus = LeaveStatus.Pending,
        };
        var approval = ConfiguredApproval(nameof(LeaveRequest), approverId);
        context.AddRange(employee, leaveType, leave, approval);
        await context.SaveChangesAsync();

        await CreateRepository(context).CreateInitialApprovalsAsync(nameof(LeaveRequest), leave.Id);

        var pending = Assert.Single(context.LeaveRequestApprovals);
        Assert.Equal(ApprovalStatus.Pending, pending.Status);
        Assert.Equal(approverId, pending.UserId);
        Assert.False(leave.Approved);
        Assert.Equal(LeaveStatus.Pending, leave.LeaveStatus);
        Assert.Empty(context.ApprovalActionLogs);
    }

    [Fact]
    public async Task CreateInitialApprovalsAsync_CreatesConfiguredOvertimeRequestApproval()
    {
        await using var context = CreateContext();
        var approverId = Guid.NewGuid();
        var overtime = new OvertimeRequest
        {
            Id = Guid.NewGuid(),
            DepartmentId = Guid.NewGuid(),
            Status = OvertimeStatus.Pending,
            ApprovalStatus = ApprovalStatus.Pending,
        };
        var approval = ConfiguredApproval(nameof(OvertimeRequest), approverId);
        context.AddRange(overtime, approval);
        await context.SaveChangesAsync();

        await CreateRepository(context).CreateInitialApprovalsAsync(nameof(OvertimeRequest), overtime.Id);

        var pending = Assert.Single(context.OvertimeRequestApprovals);
        Assert.Equal(ApprovalStatus.Pending, pending.Status);
        Assert.Equal(approverId, pending.UserId);
        Assert.False(overtime.Approved);
        Assert.Equal(OvertimeStatus.Pending, overtime.Status);
        Assert.Empty(context.ApprovalActionLogs);
    }

    [Fact]
    public async Task CreateInitialApprovalsAsync_AutoApprovesProductionOrder_WhenWorkflowIsMissing()
    {
        await using var context = CreateContext();
        var order = new ProductionOrder
        {
            Id = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            Status = ProductionOrderStatus.Pending,
        };
        context.ProductionOrders.Add(order);
        await context.SaveChangesAsync();

        await CreateRepository(context).CreateInitialApprovalsAsync(nameof(ProductionOrder), order.Id);

        Assert.True(order.Approved);
        Assert.Empty(context.ProductionOrderApprovals);
        Assert.Single(context.ApprovalActionLogs);
    }

    [Fact]
    public async Task CreateInitialApprovalsAsync_CreatesConfiguredProductionOrderApproval()
    {
        await using var context = CreateContext();
        var approverId = Guid.NewGuid();
        var order = new ProductionOrder
        {
            Id = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            Status = ProductionOrderStatus.Pending,
        };
        var approval = ConfiguredApproval(nameof(ProductionOrder), approverId);
        context.AddRange(order, approval);
        await context.SaveChangesAsync();

        await CreateRepository(context).CreateInitialApprovalsAsync(nameof(ProductionOrder), order.Id);

        var pending = Assert.Single(context.ProductionOrderApprovals);
        Assert.Equal(ApprovalStatus.Pending, pending.Status);
        Assert.Equal(approverId, pending.UserId);
        Assert.False(order.Approved);
    }

    [Fact]
    public async Task CreateInitialApprovalsAsync_AutoApprovesBillingSheet_WhenWorkflowIsMissing()
    {
        await using var context = CreateContext();
        var sheet = new BillingSheet
        {
            Id = Guid.NewGuid(),
            InvoiceId = Guid.NewGuid(),
            Status = BillingSheetStatus.New,
        };
        context.BillingSheets.Add(sheet);
        await context.SaveChangesAsync();

        await CreateRepository(context).CreateInitialApprovalsAsync(nameof(BillingSheet), sheet.Id);

        Assert.True(sheet.Approved);
        Assert.Equal(BillingSheetStatus.Pending, sheet.Status);
        Assert.Empty(context.BillingSheetApprovals);
        Assert.Single(context.ApprovalActionLogs);
    }

    [Fact]
    public async Task CreateInitialApprovalsAsync_CreatesConfiguredBillingSheetApproval()
    {
        await using var context = CreateContext();
        var approverId = Guid.NewGuid();
        var sheet = new BillingSheet
        {
            Id = Guid.NewGuid(),
            InvoiceId = Guid.NewGuid(),
            Status = BillingSheetStatus.New,
        };
        var approval = ConfiguredApproval(nameof(BillingSheet), approverId);
        context.AddRange(sheet, approval);
        await context.SaveChangesAsync();

        await CreateRepository(context).CreateInitialApprovalsAsync(nameof(BillingSheet), sheet.Id);

        var pending = Assert.Single(context.BillingSheetApprovals);
        Assert.Equal(ApprovalStatus.Pending, pending.Status);
        Assert.Equal(approverId, pending.UserId);
        Assert.False(sheet.Approved);
        Assert.Equal(BillingSheetStatus.New, sheet.Status);
    }

    [Fact]
    public async Task CreateInitialApprovalsAsync_AutoApprovesPurchaseOrder_WhenWorkflowIsMissing()
    {
        // PurchaseOrder.Supplier is auto-included and required, so the InMemory
        // provider excludes the whole row from any query if no Supplier exists.
        await using var context = CreateContext();
        var supplier = new Supplier { Id = Guid.NewGuid() };
        var order = new PurchaseOrder
        {
            Id = Guid.NewGuid(),
            SourceRequisitionId = Guid.NewGuid(),
            SupplierId = supplier.Id,
            Status = PurchaseOrderStatus.New,
        };
        context.AddRange(supplier, order);
        await context.SaveChangesAsync();

        await CreateRepository(context).CreateInitialApprovalsAsync(nameof(PurchaseOrder), order.Id);

        Assert.True(order.Approved);
        Assert.Equal(PurchaseOrderStatus.Approved, order.Status);
        Assert.Empty(context.PurchaseOrderApprovals);
        Assert.Single(context.ApprovalActionLogs);
    }

    [Fact]
    public async Task CreateInitialApprovalsAsync_CreatesConfiguredPurchaseOrderApproval()
    {
        await using var context = CreateContext();
        var approverId = Guid.NewGuid();
        var order = new PurchaseOrder
        {
            Id = Guid.NewGuid(),
            SourceRequisitionId = Guid.NewGuid(),
            SupplierId = Guid.NewGuid(),
            Status = PurchaseOrderStatus.New,
        };
        var approval = ConfiguredApproval(nameof(PurchaseOrder), approverId);
        context.AddRange(order, approval);
        await context.SaveChangesAsync();

        await CreateRepository(context).CreateInitialApprovalsAsync(nameof(PurchaseOrder), order.Id);

        var pending = Assert.Single(context.PurchaseOrderApprovals);
        Assert.Equal(ApprovalStatus.Pending, pending.Status);
        Assert.Equal(approverId, pending.UserId);
        Assert.False(order.Approved);
        Assert.Equal(PurchaseOrderStatus.New, order.Status);
    }

    [Fact]
    public async Task CreateInitialApprovalsAsync_AutoApprovesShipmentDocument_WhenWorkflowIsMissing()
    {
        await using var context = CreateContext();
        var shipment = new ShipmentDocument { Id = Guid.NewGuid(), Status = ShipmentStatus.New };
        context.ShipmentDocuments.Add(shipment);
        await context.SaveChangesAsync();

        await CreateRepository(context).CreateInitialApprovalsAsync(nameof(ShipmentDocument), shipment.Id);

        Assert.True(shipment.Approved);
        Assert.Empty(context.ShipmentDocumentApprovals);
        Assert.Single(context.ApprovalActionLogs);
    }

    [Fact]
    public async Task CreateInitialApprovalsAsync_CreatesConfiguredShipmentDocumentApproval()
    {
        await using var context = CreateContext();
        var approverId = Guid.NewGuid();
        var shipment = new ShipmentDocument { Id = Guid.NewGuid(), Status = ShipmentStatus.New };
        var approval = ConfiguredApproval(nameof(ShipmentDocument), approverId);
        context.AddRange(shipment, approval);
        await context.SaveChangesAsync();

        await CreateRepository(context).CreateInitialApprovalsAsync(nameof(ShipmentDocument), shipment.Id);

        var pending = Assert.Single(context.ShipmentDocumentApprovals);
        Assert.Equal(ApprovalStatus.Pending, pending.Status);
        Assert.Equal(approverId, pending.UserId);
        Assert.False(shipment.Approved);
    }

    [Fact]
    public async Task CreateInitialApprovalsAsync_AutoApprovesJobRequest_WhenWorkflowIsMissing()
    {
        await using var context = CreateContext();
        var job = new JobRequest
        {
            Id = Guid.NewGuid(),
            DepartmentId = Guid.NewGuid(),
            IssuedById = Guid.NewGuid(),
        };
        context.JobRequests.Add(job);
        await context.SaveChangesAsync();

        await CreateRepository(context).CreateInitialApprovalsAsync(nameof(JobRequest), job.Id);

        Assert.True(job.Approved);
        Assert.Empty(context.JobRequestApprovals);
        Assert.Single(context.ApprovalActionLogs);
    }

    [Fact]
    public async Task CreateInitialApprovalsAsync_CreatesConfiguredJobRequestApproval()
    {
        await using var context = CreateContext();
        var approverId = Guid.NewGuid();
        var job = new JobRequest
        {
            Id = Guid.NewGuid(),
            DepartmentId = Guid.NewGuid(),
            IssuedById = Guid.NewGuid(),
        };
        var approval = ConfiguredApproval(nameof(JobRequest), approverId);
        context.AddRange(job, approval);
        await context.SaveChangesAsync();

        await CreateRepository(context).CreateInitialApprovalsAsync(nameof(JobRequest), job.Id);

        var pending = Assert.Single(context.JobRequestApprovals);
        Assert.Equal(ApprovalStatus.Pending, pending.Status);
        Assert.Equal(approverId, pending.UserId);
        Assert.False(job.Approved);
    }

    [Fact]
    public async Task CreateInitialApprovalsAsync_AutoApprovesProductionExtraPacking_WhenWorkflowIsMissing()
    {
        // ProductionExtraPacking has a query filter joined through Material, so the
        // InMemory provider excludes the row from any query if no Material exists.
        await using var context = CreateContext();
        var material = new Material { Id = Guid.NewGuid() };
        var packing = new ProductionExtraPacking
        {
            Id = Guid.NewGuid(),
            ProductionScheduleProductId = Guid.NewGuid(),
            MaterialId = material.Id,
            UoMId = Guid.NewGuid(),
            Status = ProductionExtraPackingStatus.Pending,
        };
        context.AddRange(material, packing);
        await context.SaveChangesAsync();

        await CreateRepository(context).CreateInitialApprovalsAsync(nameof(ProductionExtraPacking), packing.Id);

        Assert.True(packing.Approved);
        Assert.Equal(ProductionExtraPackingStatus.InProgress, packing.Status);
        Assert.Empty(context.ProductionExtraPackingApprovals);
        Assert.Single(context.ApprovalActionLogs);
    }

    [Fact]
    public async Task CreateInitialApprovalsAsync_CreatesConfiguredProductionExtraPackingApproval()
    {
        await using var context = CreateContext();
        var approverId = Guid.NewGuid();
        var packing = new ProductionExtraPacking
        {
            Id = Guid.NewGuid(),
            ProductionScheduleProductId = Guid.NewGuid(),
            MaterialId = Guid.NewGuid(),
            UoMId = Guid.NewGuid(),
            Status = ProductionExtraPackingStatus.Pending,
        };
        var approval = ConfiguredApproval(nameof(ProductionExtraPacking), approverId);
        context.AddRange(packing, approval);
        await context.SaveChangesAsync();

        await CreateRepository(context).CreateInitialApprovalsAsync(nameof(ProductionExtraPacking), packing.Id);

        var pending = Assert.Single(context.ProductionExtraPackingApprovals);
        Assert.Equal(ApprovalStatus.Pending, pending.Status);
        Assert.Equal(approverId, pending.UserId);
        Assert.False(packing.Approved);
        Assert.Equal(ProductionExtraPackingStatus.Pending, packing.Status);
    }

    [Fact]
    public async Task CreateInitialApprovalsAsync_AutoApprovesFinishedGoodsTransferNote_WhenWorkflowIsMissing()
    {
        await using var context = CreateContext();
        var note = new FinishedGoodsTransferNote
        {
            Id = Guid.NewGuid(),
            TransferNoteNumber = "FGTN-1",
            BatchManufacturingRecordId = Guid.NewGuid(),
        };
        context.FinishedGoodsTransferNotes.Add(note);
        await context.SaveChangesAsync();

        await CreateRepository(context).CreateInitialApprovalsAsync(nameof(FinishedGoodsTransferNote), note.Id);

        Assert.True(note.Approved);
        Assert.Empty(context.FinishedGoodsTransferNoteApprovals);
        Assert.Single(context.ApprovalActionLogs);
    }

    [Fact]
    public async Task CreateInitialApprovalsAsync_CreatesConfiguredFinishedGoodsTransferNoteApproval()
    {
        await using var context = CreateContext();
        var approverId = Guid.NewGuid();
        var note = new FinishedGoodsTransferNote
        {
            Id = Guid.NewGuid(),
            TransferNoteNumber = "FGTN-2",
            BatchManufacturingRecordId = Guid.NewGuid(),
        };
        var approval = ConfiguredApproval(nameof(FinishedGoodsTransferNote), approverId);
        context.AddRange(note, approval);
        await context.SaveChangesAsync();

        await CreateRepository(context).CreateInitialApprovalsAsync(nameof(FinishedGoodsTransferNote), note.Id);

        var pending = Assert.Single(context.FinishedGoodsTransferNoteApprovals);
        Assert.Equal(ApprovalStatus.Pending, pending.Status);
        Assert.Equal(approverId, pending.UserId);
        Assert.False(note.Approved);
    }

    [Fact]
    public async Task CreateInitialApprovalsAsync_AutoApprovesStockAdjustment_WhenWorkflowIsMissing()
    {
        await using var context = CreateContext();
        var adjustment = new StockAdjustment
        {
            Id = Guid.NewGuid(),
            AdjustmentNumber = "ADJ-1",
            TargetType = StockAdjustmentTarget.Material,
        };
        context.StockAdjustments.Add(adjustment);
        await context.SaveChangesAsync();

        await CreateRepository(context).CreateInitialApprovalsAsync(nameof(StockAdjustment), adjustment.Id);

        Assert.True(adjustment.Approved);
        Assert.Empty(context.StockAdjustmentApprovals);
        Assert.Single(context.ApprovalActionLogs);
    }

    [Fact]
    public async Task CreateInitialApprovalsAsync_CreatesConfiguredStockAdjustmentApproval()
    {
        await using var context = CreateContext();
        var approverId = Guid.NewGuid();
        var adjustment = new StockAdjustment
        {
            Id = Guid.NewGuid(),
            AdjustmentNumber = "ADJ-2",
            TargetType = StockAdjustmentTarget.Material,
        };
        var approval = ConfiguredApproval(nameof(StockAdjustment), approverId);
        context.AddRange(adjustment, approval);
        await context.SaveChangesAsync();

        await CreateRepository(context).CreateInitialApprovalsAsync(nameof(StockAdjustment), adjustment.Id);

        var pending = Assert.Single(context.StockAdjustmentApprovals);
        Assert.Equal(ApprovalStatus.Pending, pending.Status);
        Assert.Equal(approverId, pending.UserId);
        Assert.False(adjustment.Approved);
    }

    [Fact]
    public async Task CreateInitialApprovalsAsync_AutoApprovesProformaInvoice_WhenWorkflowIsMissing()
    {
        // The auto-approve path Includes AllocateProductionOrder, a required
        // navigation; the InMemory provider excludes the row entirely if it's missing.
        await using var context = CreateContext();
        var allocation = new AllocateProductionOrder { Id = Guid.NewGuid(), ProductionOrderId = Guid.NewGuid() };
        var invoice = new ProformaInvoice
        {
            Id = Guid.NewGuid(),
            AllocateProductionOrderId = allocation.Id,
            Status = ProformaInvoiceStatus.Pending,
        };
        context.AddRange(allocation, invoice);
        await context.SaveChangesAsync();

        await CreateRepository(context).CreateInitialApprovalsAsync(nameof(ProformaInvoice), invoice.Id);

        Assert.True(invoice.Approved);
        Assert.Empty(context.ProformaInvoiceApprovals);
        Assert.Single(context.ApprovalActionLogs);
    }

    [Fact]
    public async Task CreateInitialApprovalsAsync_CreatesConfiguredProformaInvoiceApproval()
    {
        await using var context = CreateContext();
        var approverId = Guid.NewGuid();
        var invoice = new ProformaInvoice
        {
            Id = Guid.NewGuid(),
            AllocateProductionOrderId = Guid.NewGuid(),
            Status = ProformaInvoiceStatus.Pending,
        };
        var approval = ConfiguredApproval(nameof(ProformaInvoice), approverId);
        context.AddRange(invoice, approval);
        await context.SaveChangesAsync();

        await CreateRepository(context).CreateInitialApprovalsAsync(nameof(ProformaInvoice), invoice.Id);

        var pending = Assert.Single(context.ProformaInvoiceApprovals);
        Assert.Equal(ApprovalStatus.Pending, pending.Status);
        Assert.Equal(approverId, pending.UserId);
        Assert.False(invoice.Approved);
    }

    [Fact]
    public async Task CreateInitialApprovalsAsync_AutoApprovesResponse_WhenWorkflowIsMissing()
    {
        // Response has a query filter joined through Form, so the InMemory provider
        // excludes the row from any query - including this one - if no Form exists.
        await using var context = CreateContext();
        var form = new Form { Id = Guid.NewGuid() };
        var response = new Response { Id = Guid.NewGuid(), FormId = form.Id };
        context.AddRange(form, response);
        await context.SaveChangesAsync();

        await CreateRepository(context).CreateInitialApprovalsAsync(nameof(Response), response.Id);

        Assert.True(response.Approved);
        Assert.False(response.Rejected);
        Assert.Empty(context.ResponseApprovals);
        Assert.Single(context.ApprovalActionLogs);
    }

    [Fact]
    public async Task CreateInitialApprovalsAsync_CreatesConfiguredResponseApproval()
    {
        await using var context = CreateContext();
        var approverId = Guid.NewGuid();
        var form = new Form { Id = Guid.NewGuid() };
        var response = new Response { Id = Guid.NewGuid(), FormId = form.Id };
        var approval = ConfiguredApproval(nameof(Response), approverId);
        context.AddRange(form, response, approval);
        await context.SaveChangesAsync();

        await CreateRepository(context).CreateInitialApprovalsAsync(nameof(Response), response.Id);

        var pending = Assert.Single(context.ResponseApprovals);
        Assert.Equal(ApprovalStatus.Pending, pending.Status);
        Assert.Equal(approverId, pending.UserId);
        Assert.Equal(1, pending.ApprovalRound);
        Assert.False(response.Approved);
        Assert.False(response.Rejected);
        Assert.Empty(context.ApprovalActionLogs);
    }

    private static Approval ConfiguredApproval(string itemType, Guid approverId)
    {
        var approval = new Approval
        {
            Id = Guid.NewGuid(),
            ItemType = itemType,
            ApprovalStages = [],
        };
        approval.ApprovalStages.Add(new ApprovalStage
        {
            Id = Guid.NewGuid(),
            ApprovalId = approval.Id,
            Approval = approval,
            Order = 1,
            Required = true,
            UserId = approverId,
        });
        return approval;
    }

    [Fact]
    public async Task CreateApproval_RejectsUnassignedStage()
    {
        await using var context = CreateContext();
        var request = new CreateApprovalRequest
        {
            ItemType = "TestDocument",
            ApprovalStages =
            [
                new CreateApprovalStageRequest
                {
                    Order = 1,
                    Required = true,
                },
            ],
        };

        var result = await CreateRepository(context).CreateApproval(request, Guid.NewGuid());

        Assert.False(result.IsSuccess);
        Assert.Equal("Approval.StageAssignment", Assert.Single(result.Errors).Code);
        Assert.Empty(context.Approvals);
    }

    private static ApprovalRepository CreateRepository(ApplicationDbContext context) =>
        new(
            context,
            null!,
            null!,
            null!,
            NullLogger<ApprovalRepository>.Instance,
            null!,
            new NoOpProductionActivityStepEventPublisher()
        );

    private static ApplicationDbContext CreateContext() =>
        new(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options,
            new AutomaticPolicyCurrentUser()
        );
}
