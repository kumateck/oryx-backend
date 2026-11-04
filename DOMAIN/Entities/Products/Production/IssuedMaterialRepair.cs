using System.ComponentModel.DataAnnotations;

namespace DOMAIN.Entities.Products.Production;

public enum IssuedMaterialRepairKind { Bom, Packaging }

// Append-only corrective document. The original approved requisition is never rewritten.
public sealed class IssuedMaterialRepair
{
    public Guid Id { get; set; }
    public Guid ProductionScheduleProductId { get; set; }
    public Guid RequisitionId { get; set; }
    public IssuedMaterialRepairKind Kind { get; set; }
    public Guid ActorId { get; set; }
    public DateTime AppliedAt { get; set; }
    [MaxLength(1000)] public string Reason { get; set; } = "";
    [MaxLength(64)] public string PreviewToken { get; set; } = "";
    public List<IssuedMaterialRepairLine> Lines { get; set; } = [];
    public List<IssuedMaterialRepairAllocation> Allocations { get; set; } = [];
}

public sealed class IssuedMaterialRepairLine
{
    public Guid Id { get; set; }
    public Guid IssuedMaterialRepairId { get; set; }
    public Guid RequisitionItemId { get; set; }
    public Guid MaterialId { get; set; }
    public Guid UoMId { get; set; }
    public string MaterialCode { get; set; } = "";
    public string MaterialName { get; set; } = "";
    public string UnitSymbol { get; set; } = "";
    public decimal ApprovedQuantity { get; set; }
    public decimal BeforeQuantity { get; set; }
    public decimal TargetQuantity { get; set; }
    public decimal Adjustment { get; set; }
}

public sealed class IssuedMaterialRepairAllocation
{
    public Guid Id { get; set; }
    public Guid IssuedMaterialRepairId { get; set; }
    public Guid MaterialId { get; set; }
    public Guid MaterialBatchId { get; set; }
    public Guid ShelfId { get; set; }
    public Guid ReservationId { get; set; }
    public Guid MovementId { get; set; }
    public Guid BinCardId { get; set; }
    public decimal Adjustment { get; set; }
    public decimal ShelfBefore { get; set; }
    public decimal ShelfAfter { get; set; }
    public decimal AssignedBefore { get; set; }
    public decimal AssignedAfter { get; set; }
}

public sealed class ApplyIssuedMaterialRepairRequest
{
    public Guid RequestId { get; set; }
    [Required, StringLength(64, MinimumLength = 64)] public string PreviewToken { get; set; } = "";
    [Required, StringLength(1000, MinimumLength = 3)] public string Reason { get; set; } = "";
}

public sealed class IssuedMaterialRepairPreviewDto
{
    public Guid BatchId { get; set; }
    public string BatchNumber { get; set; } = "";
    public IssuedMaterialRepairKind Kind { get; set; }
    public string RequisitionCode { get; set; } = "";
    public string PreviewToken { get; set; } = "";
    public bool CanApply { get; set; }
    public bool HasChanges { get; set; }
    public List<string> Blockers { get; set; } = [];
    public List<IssuedMaterialRepairRowDto> Rows { get; set; } = [];
    public List<IssuedMaterialRepairHistoryDto> History { get; set; } = [];
}

public sealed class IssuedMaterialRepairRowDto
{
    public Guid MaterialId { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string Unit { get; set; } = "";
    public decimal ApprovedQuantity { get; set; }
    public decimal BeforeQuantity { get; set; }
    public decimal TargetQuantity { get; set; }
    public decimal Adjustment { get; set; }
    public decimal AvailableQuantity { get; set; }
    public List<IssuedMaterialRepairLotDto> Lots { get; set; } = [];
}

public sealed class IssuedMaterialRepairLotDto
{
    public Guid MaterialBatchId { get; set; }
    public Guid ShelfId { get; set; }
    public string BatchNumber { get; set; } = "";
    public decimal Adjustment { get; set; }
}

public sealed class IssuedMaterialRepairHistoryDto
{
    public Guid Id { get; set; }
    public Guid ActorId { get; set; }
    public DateTime AppliedAt { get; set; }
    public string Reason { get; set; } = "";
    public List<IssuedMaterialRepairRowDto> Rows { get; set; } = [];
}

public sealed class IssuedMaterialRepairResultDto
{
    public Guid? RepairId { get; set; }
    public bool Applied { get; set; }
    public bool AlreadyApplied { get; set; }
}
