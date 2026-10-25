using DOMAIN.Entities.Approvals;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Warehouses;

namespace DOMAIN.Entities.StockAdjustments;

public class StockDiscrepancy : BaseEntity, IRequireApproval
{
    public Guid ShelfMaterialBatchId { get; set; }
    public ShelfMaterialBatch ShelfMaterialBatch { get; set; }
    public decimal SystemQuantitySnapshot { get; set; }
    public decimal PhysicalQuantity { get; set; }
    public StockAdjustmentReasonCode ReasonCode { get; set; }
    public string Notes { get; set; }
    public bool Approved { get; set; }
    public bool Rejected { get; set; }
    public DateTime? ReconciledAt { get; set; }
    public Guid? ReconciledById { get; set; }
    public List<StockDiscrepancyApproval> Approvals { get; set; } = [];
}

public class StockDiscrepancyApproval : ResponsibleApprovalStage
{
    public Guid Id { get; set; }
    public Guid StockDiscrepancyId { get; set; }
    public StockDiscrepancy StockDiscrepancy { get; set; }
    public Guid ApprovalId { get; set; }
    public Approval Approval { get; set; }
}

public sealed class CreateStockDiscrepancy
{
    public Guid ShelfMaterialBatchId { get; set; }
    public decimal PhysicalQuantity { get; set; }
    public StockAdjustmentReasonCode ReasonCode { get; set; }
    public string Notes { get; set; } = string.Empty;
}

public sealed class StockDiscrepancyDto
{
    public Guid Id { get; set; }
    public Guid ShelfMaterialBatchId { get; set; }
    public string BatchNumber { get; set; } = string.Empty;
    public string MaterialName { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public decimal SystemQuantitySnapshot { get; set; }
    public decimal PhysicalQuantity { get; set; }
    public decimal Variance { get; set; }
    public StockAdjustmentReasonCode ReasonCode { get; set; }
    public string Notes { get; set; } = string.Empty;
    public bool Approved { get; set; }
    public bool Rejected { get; set; }
    public DateTime? ReconciledAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
