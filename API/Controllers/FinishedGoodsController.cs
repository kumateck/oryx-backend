using APP.Extensions;
using APP.Services.FinishedGoods;
using APP.Utils;
using DOMAIN.Entities.FinishedGoods;
using DOMAIN.Entities.ProductionOrders;
using DOMAIN.Entities.ProformaInvoices;
using INFRASTRUCTURE.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/finished-goods")]
[Tags("FinishedGoods")]
public class FinishedGoodsController(ApplicationDbContext context, APP.IRepository.IApprovalRepository approvals) : ControllerBase
{
    private FinishedGoodsWorkflow Workflow => new(context, approvals);
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [HttpPost("proformas/{id:guid}/verify-po")]
    [Authorize(PermissionKeys.CanVerifyFinishedGoodsCustomerPo)]
    public async Task<IResult> VerifyPo(Guid id, VerifyFinishedGoodsCustomerPoRequest request) { var r = await Workflow.VerifyPo(id, request); return r.IsSuccess ? TypedResults.NoContent() : r.ToProblemDetails(); }
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [HttpPost("allocations/{id:guid}/clearance")]
    [Authorize(PermissionKeys.CanClearFinishedGoodsDispatch)]
    public async Task<IResult> Clear(Guid id, ClearFinishedGoodsDispatchRequest request) { var r = await Workflow.Clear(id, request); return r.IsSuccess ? TypedResults.NoContent() : r.ToProblemDetails(); }
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [HttpPost("allocations/{id:guid}/dispatch")]
    [Authorize(PermissionKeys.CanDispatchFinishedGoods)]
    public async Task<IResult> Dispatch(Guid id, DispatchFinishedGoodsRequest request) { var r = await Workflow.Dispatch(id, request); return r.IsSuccess ? TypedResults.NoContent() : r.ToProblemDetails(); }
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [HttpPost("allocations/{id:guid}/delivery")]
    [Authorize(PermissionKeys.CanDeliverFinishedGoods)]
    public async Task<IResult> Deliver(Guid id, DeliverFinishedGoodsRequest request) { var r = await Workflow.Deliver(id, request); return r.IsSuccess ? TypedResults.NoContent() : r.ToProblemDetails(); }
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [HttpPost("transfers/{id:guid}/hold")]
    [Authorize(PermissionKeys.CanHoldFinishedGoods)]
    public async Task<IResult> Hold(Guid id, RecallEvidenceRequest request) { var r = await Workflow.Hold(id, new FinishedGoodsHoldRequest { Hold = true, Reason = request.Evidence }); return r.IsSuccess ? TypedResults.NoContent() : r.ToProblemDetails(); }
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [HttpPost("transfers/{id:guid}/release-hold")]
    [Authorize(PermissionKeys.CanReleaseFinishedGoodsHold)]
    public async Task<IResult> ReleaseHold(Guid id, RecallEvidenceRequest request) { var r = await Workflow.Hold(id, new FinishedGoodsHoldRequest { Hold = false, Reason = request.Evidence }); return r.IsSuccess ? TypedResults.NoContent() : r.ToProblemDetails(); }
    [HttpGet("recalls")]
    [Authorize(PermissionKeys.CanViewProductRecalls)]
    [ProducesResponseType(typeof(Paginateable<IEnumerable<FinishedGoodsRecallSummaryDto>>), StatusCodes.Status200OK)]
    public async Task<IResult> Recalls(string searchQuery = null, int page = 1, int pageSize = 30, DOMAIN.Entities.Products.Division? division = null) => TypedResults.Ok(await Workflow.Recalls(searchQuery, page, pageSize, division));
    [HttpGet("recalls/{id:guid}")]
    [Authorize(PermissionKeys.CanViewProductRecalls)]
    [ProducesResponseType(typeof(FinishedGoodsRecallDetailDto), StatusCodes.Status200OK)]
    public async Task<IResult> RecallDetail(Guid id) { var r = await Workflow.RecallDetail(id); return r.IsSuccess ? TypedResults.Ok(r.Value) : r.ToProblemDetails(); }
    [HttpGet("recalls/{id:guid}/print")]
    [Authorize(PermissionKeys.CanPrintProductRecall)]
    [ProducesResponseType(typeof(FinishedGoodsRecallDetailDto), StatusCodes.Status200OK)]
    public async Task<IResult> PrintRecall(Guid id) { var r = await Workflow.RecallDetail(id); return r.IsSuccess ? TypedResults.Ok(r.Value) : r.ToProblemDetails(); }
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [HttpPost("recalls")]
    [Authorize(PermissionKeys.CanCreateProductRecall)]
    public async Task<IResult> Recall(CreateFinishedGoodsRecallRequest request) { var r = await Workflow.Recall(request); return r.IsSuccess ? TypedResults.NoContent() : r.ToProblemDetails(); }
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [HttpPost("recalls/{id:guid}/contacts")]
    [Authorize(PermissionKeys.CanNotifyProductRecall)]
    public async Task<IResult> Contact(Guid id, RecordRecallContactRequest request) { var r = await Workflow.Contact(id, request); return r.IsSuccess ? TypedResults.NoContent() : r.ToProblemDetails(); }
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [HttpPost("recalls/{id:guid}/recoveries")]
    [Authorize(PermissionKeys.CanRecordProductRecallRecovery)]
    public async Task<IResult> Recover(Guid id, RecordRecallRecoveryRequest request) { var r = await Workflow.Recover(id, request); return r.IsSuccess ? TypedResults.NoContent() : r.ToProblemDetails(); }
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [HttpPost("recalls/{id:guid}/recoveries/{recoveryId:guid}/dispose")]
    [Authorize(PermissionKeys.CanDisposeProductRecallRecovery)]
    public async Task<IResult> DisposeRecovery(Guid id, Guid recoveryId, RecallEvidenceRequest request) { var r = await Workflow.DisposeRecovery(id, recoveryId, request); return r.IsSuccess ? TypedResults.NoContent() : r.ToProblemDetails(); }
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [HttpPost("recalls/{id:guid}/close")]
    [Authorize(PermissionKeys.CanCloseProductRecall)]
    public async Task<IResult> CloseRecall(Guid id, CloseFinishedGoodsRecallRequest request) { var r = await Workflow.CloseRecall(id, request); return r.IsSuccess ? TypedResults.NoContent() : r.ToProblemDetails(); }

    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [HttpPost("invoices/{id:guid}/void")]
    [Authorize(PermissionKeys.CanVoidFinishedGoodsInvoice)]
    public async Task<IResult> VoidInvoice(Guid id, RecallEvidenceRequest request) { var r = await Workflow.VoidInvoice(id, request); return r.IsSuccess ? TypedResults.NoContent() : r.ToProblemDetails(); }
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [HttpPost("allocations/{id:guid}/cancel")]
    [Authorize(PermissionKeys.CanCancelFinishedGoodsAllocation)]
    public async Task<IResult> Cancel(Guid id, RecallEvidenceRequest request) { var r = await Workflow.Cancel(id, request); return r.IsSuccess ? TypedResults.NoContent() : r.ToProblemDetails(); }
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [HttpPost("recalls/{id:guid}/dispose-stock")]
    [Authorize(PermissionKeys.CanDisposeRecalledFinishedGoodsStock)]
    public async Task<IResult> DisposeOnSite(Guid id, DisposeRecalledStockRequest request) { var r = await Workflow.DisposeOnSite(id, request); return r.IsSuccess ? TypedResults.NoContent() : r.ToProblemDetails(); }

    [HttpPost("transfers/{id:guid}/reconcile")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [Authorize(PermissionKeys.CanReconcileFinishedGoodsStock)]
    public async Task<IResult> Reconcile(Guid id, ReconcileFinishedGoodsStockRequest request) { var r = await Workflow.Reconcile(id, request); return r.IsSuccess ? TypedResults.NoContent() : r.ToProblemDetails(); }

    [HttpPost("allocations/{id:guid}/load")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [Authorize(PermissionKeys.CanLoadFinishedGoods)]
    public async Task<IResult> Load(Guid id, LoadFinishedGoodsRequest request) { var r = await Workflow.Load(id, request); return r.IsSuccess ? TypedResults.NoContent() : r.ToProblemDetails(); }

    [HttpGet("allocations/{id:guid}/documents")]
    [ProducesResponseType(typeof(FinishedGoodsDocumentsDto), StatusCodes.Status200OK)]
    [Authorize(PermissionKeys.CanViewInvoice)]
    public async Task<IResult> Documents(Guid id) { var r = await Workflow.Documents(id); return r.IsSuccess ? TypedResults.Ok(r.Value) : r.ToProblemDetails(); }

    [HttpGet("recall-batches")]
    [ProducesResponseType(typeof(List<FinishedGoodsBatchOptionDto>), StatusCodes.Status200OK)]
    [Authorize(PermissionKeys.CanViewProductRecalls)]
    public async Task<IResult> RecallBatches(string searchQuery = null) => TypedResults.Ok(await Workflow.RecallBatches(searchQuery));
    [HttpGet("transfers/{id:guid}/reconciliation")]
    [ProducesResponseType(typeof(List<FinishedGoodsReconciliationAllocationDto>), StatusCodes.Status200OK)]
    [Authorize(PermissionKeys.CanReconcileFinishedGoodsStock)]
    public async Task<IResult> ReconciliationOptions(Guid id) => TypedResults.Ok(await Workflow.ReconciliationOptions(id));

    [HttpPost("allocations/{id:guid}/costing")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [Authorize(PermissionKeys.CanConfirmFinishedGoodsCosting)]
    public async Task<IResult> ConfirmCosting(Guid id, ConfirmFinishedGoodsCostingRequest request) { var r = await Workflow.ConfirmCosting(id, request); return r.IsSuccess ? TypedResults.NoContent() : r.ToProblemDetails(); }
    [HttpPost("recalls/{id:guid}/settlements")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [Authorize(PermissionKeys.CanRecordProductRecallSettlement)]
    public async Task<IResult> RecordSettlement(Guid id, RecordProductRecallSettlementRequest request) { var r = await Workflow.RecordSettlement(id, request); return r.IsSuccess ? TypedResults.NoContent() : r.ToProblemDetails(); }

    [HttpGet("allocations/{id:guid}/waybill-print")]
    [ProducesResponseType(typeof(FinishedGoodsWaybillPrintDto), StatusCodes.Status200OK)]
    [Authorize(PermissionKeys.CanPrintFinishedGoodsWaybill)]
    public async Task<IResult> PrintWaybill(Guid id) { var r = await Workflow.WaybillPrint(id); return r.IsSuccess ? TypedResults.Ok(r.Value) : r.ToProblemDetails(); }
    [HttpGet("recalls/{id:guid}/settlement-options")]
    [ProducesResponseType(typeof(List<FinishedGoodsSettlementOptionDto>), StatusCodes.Status200OK)]
    [Authorize(PermissionKeys.CanRecordProductRecallSettlement)]
    public async Task<IResult> SettlementOptions(Guid id) => TypedResults.Ok(await Workflow.SettlementOptions(id));
    [HttpGet("recalls/{id:guid}/replacement-orders")]
    [ProducesResponseType(typeof(List<FinishedGoodsBatchOptionDto>), StatusCodes.Status200OK)]
    [Authorize(PermissionKeys.CanRecordProductRecallSettlement)]
    public async Task<IResult> ReplacementOrders(Guid id, Guid invoiceId, string searchQuery = null) => TypedResults.Ok(await Workflow.ReplacementOrders(id, invoiceId, searchQuery));

    [HttpPost("recalls/{id:guid}/reopen")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [Authorize(PermissionKeys.CanReopenProductRecall)]
    public async Task<IResult> ReopenRecall(Guid id, RecallEvidenceRequest request) { var r = await Workflow.ReopenRecall(id, request); return r.IsSuccess ? TypedResults.NoContent() : r.ToProblemDetails(); }
    [HttpPost("recalls/{id:guid}/authority-decision")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [Authorize(PermissionKeys.CanUpdateProductRecallDecision)]
    public async Task<IResult> UpdateRecallDecision(Guid id, UpdateFinishedGoodsRecallDecisionRequest request) { var r = await Workflow.UpdateRecallDecision(id, request); return r.IsSuccess ? TypedResults.NoContent() : r.ToProblemDetails(); }

}
