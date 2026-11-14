using DOMAIN.Entities.Base;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.ProductionSchedules;
using DOMAIN.Entities.Roles;
using DOMAIN.Entities.Warehouses;
using SHARED;

namespace DOMAIN.Entities.Requisitions;

public class RequisitionDto
{
    public int ContentRevision { get; set; }
    public Guid Id { get; set; }
    public string Code { get; set; }
    public RequisitionType RequisitionType { get; set; }
    public RequestStatus Status { get; set; }
    public CollectionItemDto RequestedBy { get; set; }
    public CollectionItemDto Department { get; set; }
    public List<RequisitionItemDto> Items { get; set; } = [];
    public bool Approved { get; set; }
    public DateTime? ExpectedDelivery { get; set; }
    public DateTime CreatedAt { get; set; }
    public ProductionScheduleProductDto ProductionScheduleProduct { get; set; }
    /// <summary>The production step the requisition was raised from.</summary>
    public Guid? ProductionActivityStepId { get; set; }
    public string Comments { get; set; }
    public Guid? WhitePlaceboPreparationId { get; set; }
    /// <summary>Who signs the printed stock requisition voucher; null for other requisition types.</summary>
    public StockIssueSignatoriesDto Signatories { get; set; }
}

public class RequisitionItemDto
{
    public Guid Id { get; set; }
    public MaterialDto Material { get; set; }
    public UnitOfMeasureDto UoM { get; set; }
    public decimal Quantity { get; set; }
    /// <summary>Original signed quantity when Quantity includes an audited post-issue correction.</summary>
    public decimal? ApprovedQuantity { get; set; }
    /// <summary>The BOM line's main material when the store chose to issue a substitute (or back) in its place.</summary>
    public CollectionItemDto SubstituteFor { get; set; }
    public decimal QuantityReceived { get; set; }
    public RequestStatus Status { get; set; }
    public List<BatchToSupply> Batches { get; set; } = [];
}


public class RequisitionItemListDto
{
    public Guid Id { get; set; }
    public CollectionItemDto Material { get; set; }
    public UnitOfMeasureDto UoM { get; set; }
    public decimal Quantity { get; set; }
    public decimal QuantityReceived { get; set; }
    public RequestStatus Status { get; set; }
}

public class MaterialBatchLocationsDto
{
    public MaterialBatchDto MaterialBatch { get; set; }
    public List<ShelfMaterialBatchDto> ShelfMaterialBatches { get; set; }
}

// public class LocationDetails
// {
//     public ShelfMaterialBatchDto ShelfMaterialBatch { get; set; }
//     public decimal Quantity { get; set; }
// }

public class RequisitionApprovalDto
{
    public CollectionItemDto User { get; set; }
    public RoleDto Role { get; set; }
    public bool Required { get; set; }
    public bool IsApproved { get; set; }
    public DateTime? ApprovalTime { get; set; }
    public string Comments { get; set; }
    public int Order { get; set; }
}

public class MaterialAlternativeBatchesDto
{
    public MaterialDto Material { get; set; }
    public decimal RequestedQuantity { get; set; }
    public List<BatchToSupply> CurrentReservedBatches { get; set; } = [];
    public List<AlternativeBatchDto> AlternativeBatches { get; set; } = [];
}

public class AlternativeBatchDto
{
    public MaterialWarehouseLocationShelfDto WarehouseLocationShelf { get; set; }
    public MaterialBatchListDto Batch { get; set; }
    public decimal QuantityAvailable { get; set; }
    public CollectionItemDto Warehouse { get; set; }
    public Guid? WarehouseLocationShelfId { get; set; }
}
