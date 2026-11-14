using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Users;
using SHARED;

namespace DOMAIN.Entities.Requisitions;

/// <summary>
/// Evidence that the store issuing a stock requisition replaced a BOM line's material with another option the BOM
/// allows (the main material or one of its substitutes). Rows are never edited; each change adds a row.
/// </summary>
public class StockRequisitionMaterialChoice : BaseEntity
{
    public Guid RequisitionId { get; set; }
    public Requisition Requisition { get; set; }
    public Guid RequisitionItemId { get; set; }
    public Guid ProductionScheduleProductId { get; set; }
    /// <summary>The BOM line's main material, which identifies the line.</summary>
    public Guid SourceMaterialId { get; set; }
    public Material SourceMaterial { get; set; }
    public Guid FromMaterialId { get; set; }
    public Material FromMaterial { get; set; }
    public decimal FromQuantity { get; set; }
    public Guid FromUnitId { get; set; }
    public Guid ToMaterialId { get; set; }
    public Material ToMaterial { get; set; }
    public decimal ToQuantity { get; set; }
    public Guid ToUnitId { get; set; }
    public Guid ChosenById { get; set; }
    public User ChosenBy { get; set; }
    public DateTime ChosenAt { get; set; }
    [StringLength(500)] public string Reason { get; set; } = string.Empty;
}

/// <summary>A BOM line on a stock requisition that the store can issue as one of several materials.</summary>
public class StockRequisitionMaterialOptionsDto
{
    public Guid RequisitionItemId { get; set; }
    public CollectionItemDto SourceMaterial { get; set; }
    public Guid CurrentMaterialId { get; set; }
    /// <summary>The applied configuration revision; send it back when changing the material.</summary>
    public int Revision { get; set; }
    public bool CanChange { get; set; }
    /// <summary>Why the material can no longer be changed, when <see cref="CanChange"/> is false.</summary>
    public string Reason { get; set; }
    public List<StockRequisitionMaterialOptionDto> Options { get; set; } = [];
}

public class StockRequisitionMaterialOptionDto
{
    public CollectionItemDto Material { get; set; }
    public bool IsMain { get; set; }
    public bool IsCurrent { get; set; }
    /// <summary>Quantity this option needs for the batch, in <see cref="Unit"/> (the department stock unit).</summary>
    public decimal Quantity { get; set; }
    public UnitOfMeasureDto Unit { get; set; }
    /// <summary>Usable stock in the issuing store not held by other batches, plus what this batch already holds of it.</summary>
    public decimal Available { get; set; }
    public bool Covers { get; set; }
    /// <summary>The earliest expiry or retest date among the usable stock; null when none is dated.</summary>
    public DateTime? EarliestExpiry { get; set; }
    /// <summary>The batches this option would be issued from, earliest expiry first.</summary>
    public List<StockRequisitionMaterialOptionBatchDto> Batches { get; set; } = [];
}

public class StockRequisitionMaterialOptionBatchDto
{
    public Guid MaterialBatchId { get; set; }
    public string BatchNumber { get; set; }
    public decimal Quantity { get; set; }
    public DateTime? Expiry { get; set; }
}

public class ChooseStockRequisitionMaterialRequest
{
    public Guid SourceMaterialId { get; set; }
    public Guid MaterialId { get; set; }
    public int Revision { get; set; }
    [StringLength(500)] public string Reason { get; set; } = string.Empty;
}
