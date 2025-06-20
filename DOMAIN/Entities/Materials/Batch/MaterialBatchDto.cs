using DOMAIN.Entities.Base;
using DOMAIN.Entities.Checklists;
using DOMAIN.Entities.Procurement.Manufacturers;
using DOMAIN.Entities.Procurement.Suppliers;
using DOMAIN.Entities.ProductionSchedules.StockTransfers;
using DOMAIN.Entities.Products.Production;
using DOMAIN.Entities.Shipments;
using DOMAIN.Entities.Warehouses;
using SHARED;

namespace DOMAIN.Entities.Materials.Batch;

public class MaterialBatchDto
{
    public Guid Id { get; set; }
    public CollectionItemDto Material { get; set; }
    public string Code { get; set; }
    public string BatchNumber { get; set; }
    public BatchChecklistDto Checklist { get; set; }
    public MaterialBatchStockTransferSourceDto StockTransferSource { get; set; }
    public UnitOfMeasureDto UoM { get; set; }
    public int NumberOfContainers { get; set; }
    public PackageStyleDto ContainerPackageStyle { get; set; }
    public decimal QuantityPerContainer { get; set; }
    public BatchStatus Status { get; set; }  
    public DateTime DateReceived { get; set; }
    public DateTime? DateApproved { get; set; }
    public decimal QuantityAssigned { get; set; }
    public decimal QuantityUnassigned { get; set; }
    public decimal TotalQuantity { get; set; }        
    public decimal ConsumedQuantity { get; set; }  
    public decimal RemainingQuantity { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public DateTime? ManufacturingDate { get; set; }
    public DateTime? RetestDate { get; set; }
    public List<MaterialBatchEventDto> Events { get; set; } = [];
    public List<MaterialBatchMovementDto> Movements { get; set; } = [];
    public List<SrDto> SampleWeights { get; set; } = [];
    public List<MassMaterialBatchMovementDto> MassMovements { get; set; } = [];
    public List<CurrentLocationDto> Locations { get; set; } = [];
    public List<MaterialBatchReservedQuantityDto> ReservedQuantities { get; set; } = [];
    public decimal ReservedQuantity { get; set; }
}

public class DistributedMaterialBatchDto
{
    public Guid Id { get; set; }
    public CollectionItemDto Material { get; set; }
    public string Code { get; set; }
    public string BatchNumber { get; set; }
    public UnitOfMeasureDto UoM { get; set; }
    public int NumberOfContainers { get; set; }
    public PackageStyleDto ContainerPackageStyle { get; set; }
    public decimal QuantityPerContainer { get; set; }
    public BatchStatus Status { get; set; }  
    public DateTime DateReceived { get; set; }
    public DateTime? DateApproved { get; set; }
    public decimal QuantityAssigned { get; set; }
    public decimal QuantityUnassigned { get; set; }
    public decimal TotalQuantity { get; set; }        
    public decimal ConsumedQuantity { get; set; }  
    public decimal RemainingQuantity { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public DateTime? ManufacturingDate { get; set; }
    public DateTime? RetestDate { get; set; }
    public List<MaterialBatchEventDto> Events { get; set; } = [];
    public List<MaterialBatchMovementDto> Movements { get; set; } = [];
    public List<SrDto> SampleWeights { get; set; } = [];
    public List<MassMaterialBatchMovementDto> MassMovements { get; set; } = [];
    public List<CurrentLocationDto> Locations { get; set; } = [];
    public List<MaterialBatchReservedQuantityDto> ReservedQuantities { get; set; } = [];
    public decimal ReservedQuantity { get; set; }
}

public class BatchChecklistDto
{
    public DistributedRequisitionMaterialDto DistributedRequisitionMaterial { get; set; }
    public MaterialDto Material { get; set; }
    public DateTime? CheckedAt { get; set; }
    public ShipmentInvoiceDto ShipmentInvoice { get; set; }
    public SupplierDto Supplier { get; set; }
    public ManufacturerDto Manufacturer { get; set; }
    public bool CertificateOfAnalysisDelivered { get; set; }
    public bool VisibleLabelling { get; set; }
    public Intactness IntactnessStatus { get; set; }
    public ConsignmentCarrier ConsignmentCarrierStatus { get; set; }
}

public class MaterialBatchEventDto 
{
    public EventType Type { get; set; }
    public decimal Quantity { get; set; }     
    public CollectionItemDto User { get; set; }          
    public DateTime CreatedAt { get; set; }    
}

public class MaterialBatchMovementDto : BaseDto
{
    public CollectionItemDto Batch { get; set; }
    public CollectionItemDto FromLocation { get; set; }
    public CollectionItemDto ToLocation { get; set; }
    public decimal Quantity { get; set; }
    public DateTime MovedAt { get; set; }
    public CollectionItemDto MovedBy { get; set; }
    public MovementType MovementType { get; set; }  
}

public class MassMaterialBatchMovementDto : BaseDto
{
    public CollectionItemDto Batch { get; set; }
    public CollectionItemDto FromWarehouse { get; set; }
    public CollectionItemDto ToWarehouse { get; set; }
    public decimal Quantity { get; set; }
    public DateTime MovedAt { get; set; }
    public CollectionItemDto MovedBy { get; set; }
    public MovementType MovementType { get; set; }  
}

public class BatchLocation
{
    public WarehouseDto ConsumptionLocation { get; set; }
    public MaterialBatchDto Batch { get; set; }
    public decimal QuantityToUse { get; set; }
}
public class CurrentLocation
{
    public Warehouse Location { get; set; }
    public decimal QuantityAtLocation { get; set; }
}

public class CurrentLocationDto
{
    public CollectionItemDto Location { get; set; }
    public decimal QuantityAtLocation { get; set; }
}

public class MaterialStockByWarehouseDto
{
    public CollectionItemDto Warehouse { get; set; }
    public decimal TotalQuantity { get; set; }
}

public class MaterialStockByDepartmentDto
{
    public CollectionItemDto Department { get; set; }
    public decimal TotalQuantity { get; set; }
}

public class BatchToSupply
{
    public MaterialBatchDto Batch { get; set; }
    public decimal QuantityToTake { get; set; }
}

public class MaterialBatchReservedQuantityDto : BaseDto
{
    public CollectionItemDto Warehouse { get; set; }
    public UnitOfMeasureDto UoM { get; set; }
    public decimal Quantity { get; set; }
}


