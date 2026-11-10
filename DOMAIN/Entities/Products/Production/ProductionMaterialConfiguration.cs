using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Materials;

namespace DOMAIN.Entities.Products.Production;

/// <summary>The requirements actually adopted by a batch, independent of later master edits.</summary>
public class ProductionMaterialConfiguration : BaseEntity
{
    public Guid ProductionScheduleProductId { get; set; }
    public MaterialKind Kind { get; set; }
    public Guid DepartmentId { get; set; }
    public Guid StorageWarehouseId { get; set; }
    public Guid ProductionWarehouseId { get; set; }
    public int Revision { get; set; }
    [MaxLength(64)] public string SourceHash { get; set; } = string.Empty;
    public string RequirementsJson { get; set; } = "[]";
}

/// <summary>Immutable operational repair evidence, including the previous approval cycle.</summary>
public class ProductionMaterialRepair : BaseEntity
{
    public Guid ActorId { get; set; }
    public Guid ProductionScheduleProductId { get; set; }
    public MaterialKind Kind { get; set; }
    [MaxLength(64)] public string PreviewHash { get; set; } = string.Empty;
    [MaxLength(2000)] public string Reason { get; set; } = string.Empty;
    public string BeforeJson { get; set; } = "{}";
    public string AfterJson { get; set; } = "{}";
    public string ResultJson { get; set; } = "{}";
}

/// <summary>Quantities use the department's stock unit. Source fields preserve the adopted definition.</summary>
public sealed record ProductionMaterialRequirement
{
    public Guid SourceMaterialId { get; init; }
    public Guid MaterialId { get; init; }
    public string Name { get; init; } = string.Empty;
    public Guid UnitId { get; init; }
    public string Unit { get; init; } = string.Empty;
    public decimal Quantity { get; init; }
    public decimal BaseQuantity { get; init; }
    public Guid BaseUnitId { get; init; }
    public string BaseUnit { get; init; } = string.Empty;
    public decimal PrescribedQuantity { get; init; }
    public decimal Allowance { get; init; }
    public bool UseDensityConversion { get; init; }
    public decimal DensityGramsPerMilliliter { get; init; }
    public decimal UnitCapacity { get; init; }
    public Guid? DirectLinkMaterialId { get; init; }
}

public sealed record ProductionMaterialRepairLine
{
    public Guid MaterialId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Unit { get; init; } = string.Empty;
    public decimal Before { get; init; }
    public decimal After { get; init; }
    public decimal ReservedBefore { get; init; }
    public decimal ReservedAfter { get; init; }
    public decimal Shortage { get; init; }
}

public sealed record ProductionMaterialRepairPreview
{
    public Guid ActivityId { get; init; }
    public Guid BatchId { get; init; }
    public MaterialKind Kind { get; init; }
    public string PreviewHash { get; init; } = string.Empty;
    public string SourceHash { get; init; } = string.Empty;
    public string Baseline { get; init; } = string.Empty;
    public bool HasChanges { get; init; }
    public bool ApprovalRequired { get; init; }
    public Guid? RequisitionId { get; init; }
    public List<ProductionMaterialRepairLine> Lines { get; init; } = [];
    public List<string> Blockers { get; init; } = [];
}

public sealed record ApplyProductionMaterialRepair
{
    public Guid RequestId { get; init; }
    [Required, StringLength(64, MinimumLength = 64)] public string PreviewHash { get; init; } = string.Empty;
    [Required, StringLength(2000, MinimumLength = 5)] public string Reason { get; init; } = string.Empty;
}

public sealed record ProductionMaterialRepairResult
{
    public Guid RepairId { get; init; }
    public bool Changed { get; init; }
    public bool ApprovalRequired { get; init; }
    public int Revision { get; init; }
}
