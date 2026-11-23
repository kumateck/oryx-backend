using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Products.Production;

namespace DOMAIN.Entities.FinishedGoods;

public class FinishedGoodsRecall : BaseEntity
{
    public Guid BatchManufacturingRecordId { get; set; }
    public BatchManufacturingRecord BatchManufacturingRecord { get; set; }
    [MaxLength(100)] public string Code { get; set; }
    [MaxLength(2000)] public string Reason { get; set; }
    [MaxLength(100)] public string RegulatoryClassification { get; set; }
    [MaxLength(1000)] public string AuthorityReference { get; set; }
    public bool Closed { get; set; }
    public DateTime? ClosedAt { get; set; }
    public Guid? ClosedById { get; set; }
    public string ClosureReport { get; set; }
    public decimal UnrecoverableYield { get; set; }
    public string UnrecoverableReason { get; set; }
    public List<FinishedGoodsRecallContact> Contacts { get; set; } = [];
    public List<FinishedGoodsRecallRecovery> Recoveries { get; set; } = [];
}
public class FinishedGoodsRecallContact : BaseEntity
{
    public Guid FinishedGoodsRecallId { get; set; }
    public FinishedGoodsRecall FinishedGoodsRecall { get; set; }
    public Guid AllocationId { get; set; }
    public DateTime NotifiedAt { get; set; }
    public DateTime? AcknowledgedAt { get; set; }
    public bool EffectivenessConfirmed { get; set; }
    public string Evidence { get; set; }
}
public class FinishedGoodsRecallRecovery : BaseEntity
{
    public Guid FinishedGoodsRecallId { get; set; }
    public FinishedGoodsRecall FinishedGoodsRecall { get; set; }
    public Guid AllocationId { get; set; }
    public Guid FinishedGoodsTransferNoteId { get; set; }
    public decimal Quantity { get; set; }
    public string Evidence { get; set; }
    public string Condition { get; set; }
    public bool Disposed { get; set; }
    public DateTime? DisposedAt { get; set; }
    public Guid? DisposedById { get; set; }
    public string DispositionEvidence { get; set; }
}
public class CreateFinishedGoodsRecallRequest
{
    public Guid BatchManufacturingRecordId { get; set; }
    public string Code { get; set; }
    public string Reason { get; set; }
    public string RegulatoryClassification { get; set; }
    public string AuthorityReference { get; set; }
}
public class RecordRecallContactRequest
{
    public Guid AllocationId { get; set; }
    public bool Acknowledged { get; set; }
    public bool EffectivenessConfirmed { get; set; }
    public string Evidence { get; set; }
}
public class RecordRecallRecoveryRequest
{
    public Guid AllocationId { get; set; }
    public Guid FinishedGoodsTransferNoteId { get; set; }
    public decimal Quantity { get; set; }
    public string Evidence { get; set; }
    public string Condition { get; set; }
}
public class RecallEvidenceRequest { public string Evidence { get; set; } }
public class FinishedGoodsRecallSummaryDto
{
    public Guid Id { get; set; }
    public string Code { get; set; }
    public Guid BatchManufacturingRecordId { get; set; }
    public string BatchNumber { get; set; }
    public string ProductName { get; set; }
    public string Reason { get; set; }
    public string RegulatoryClassification { get; set; }
    public string AuthorityReference { get; set; }
    public bool Closed { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public string ClosureReport { get; set; }
    public decimal UnrecoverableYield { get; set; }
    public string UnrecoverableReason { get; set; }
}
public class FinishedGoodsTraceDto
{
    public Guid AllocationId { get; set; }
    public Guid TransferNoteId { get; set; }
    public string TransferNoteNumber { get; set; }
    public string BatchNumber { get; set; }
    public string ProductName { get; set; }
    public string PackingStyle { get; set; }
    public string Customer { get; set; }
    public string Contact { get; set; }
    public string DeliveryAddress { get; set; }
    public string Stage { get; set; }
    public decimal Packs { get; set; }
    public decimal Yield { get; set; }
    public string Waybill { get; set; }
    public string Vehicle { get; set; }
    public DateTime? DispatchedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public bool Notified { get; set; }
    public bool Acknowledged { get; set; }
    public bool EffectivenessConfirmed { get; set; }
    public decimal RecoveredYield { get; set; }
}
public class FinishedGoodsRecallDetailDto : FinishedGoodsRecallSummaryDto
{
    public decimal TransferVariance { get; set; }
    public List<FinishedGoodsRecallHistoryDto> History { get; set; } = [];
    public List<FinishedGoodsRecallSettlementDto> Settlements { get; set; } = [];
    public List<FinishedGoodsTraceDto> Consignments { get; set; } = [];
    public List<FinishedGoodsRecallRecoveryDto> Recoveries { get; set; } = [];
    public List<FinishedGoodsRecallStockDto> Stocks { get; set; } = [];
    public decimal OnSiteYield { get; set; }
    public decimal DispatchedYield { get; set; }
    public decimal RecoveredYield { get; set; }
}
public class FinishedGoodsRecallRecoveryDto
{
    public Guid Id { get; set; }
    public Guid AllocationId { get; set; }
    public Guid FinishedGoodsTransferNoteId { get; set; }
    public decimal Quantity { get; set; }
    public string Evidence { get; set; }
    public string Condition { get; set; }
    public bool Disposed { get; set; }
    public string DispositionEvidence { get; set; }
}
public class FinishedGoodsHoldRequest { public bool Hold { get; set; } public string Reason { get; set; } }

public class DisposeRecalledStockRequest
{
    public Guid TransferNoteId { get; set; }
    public decimal Quantity { get; set; }
    public string Evidence { get; set; }
}
public class CloseFinishedGoodsRecallRequest
{
    public string Evidence { get; set; }
    public decimal UnrecoverableYield { get; set; }
    public string UnrecoverableReason { get; set; }
}

public class ReconcileFinishedGoodsStockRequest
{
    public decimal CountedPhysicalYield { get; set; }
    public decimal VerifiedUnitsPerPack { get; set; }
    public decimal VerifiedPacksPerShipper { get; set; }
    public string VerifiedPackingStyleName { get; set; }
    public string Evidence { get; set; }
    public List<ReconcileFinishedGoodsDeparture> Departures { get; set; } = [];
}
public class ReconcileFinishedGoodsDeparture
{
    public Guid AllocationId { get; set; }
    public DateTime DepartedAt { get; set; }
    public string Evidence { get; set; }
}

public class FinishedGoodsRecallStockDto
{
    public Guid TransferNoteId { get; set; }
    public string TransferNoteNumber { get; set; }
    public string PackingStyle { get; set; }
    public decimal UnitsPerPack { get; set; }
    public decimal Physical { get; set; }
    public decimal Reserved { get; set; }
    public decimal QuarantinedReturns { get; set; }
}

public class FinishedGoodsRecallSettlement : BaseEntity
{
    public Guid FinishedGoodsRecallId { get; set; }
    public FinishedGoodsRecall FinishedGoodsRecall { get; set; }
    public Guid InvoiceId { get; set; }
    public DOMAIN.Entities.Invoices.Invoice Invoice { get; set; }
    public string Kind { get; set; }
    public decimal Amount { get; set; }
    public Guid CurrencyId { get; set; }
    public DOMAIN.Entities.Currencies.Currency Currency { get; set; }
    [MaxLength(100)] public string Reference { get; set; }
    public string Evidence { get; set; }
    public Guid? ReplacementProductionOrderId { get; set; }
}
public class RecordProductRecallSettlementRequest
{
    public Guid InvoiceId { get; set; }
    public string Kind { get; set; }
    public decimal Amount { get; set; }
    public Guid CurrencyId { get; set; }
    public string Reference { get; set; }
    public string Evidence { get; set; }
    public Guid? ReplacementProductionOrderId { get; set; }
}
public class FinishedGoodsRecallSettlementDto
{
    public Guid Id { get; set; }
    public string InvoiceCode { get; set; }
    public string Kind { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; }
    public string Reference { get; set; }
    public string Evidence { get; set; }
    public Guid? ReplacementProductionOrderId { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class FinishedGoodsRecallHistoryDto
{
    public Guid Id { get; set; }
    public string Event { get; set; }
    public string Evidence { get; set; }
    public string Actor { get; set; }
    public DateTime At { get; set; }
}

public sealed class UpdateFinishedGoodsRecallDecisionRequest
{
    public string RegulatoryClassification { get; set; }
    public string AuthorityReference { get; set; }
    public string Evidence { get; set; }
}
