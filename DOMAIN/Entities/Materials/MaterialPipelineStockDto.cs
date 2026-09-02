using DOMAIN.Entities.Base;
using DOMAIN.Entities.Requisitions;

namespace DOMAIN.Entities.Materials;

public enum MaterialPipelineStage
{
    PurchaseRequisition = 0,
    Sourcing = 1,
    Quotation = 2,
    PurchaseOrder = 3,
    Shipment = 4,
    AtPort = 5,
    Cleared = 6,
    InTransit = 7,
    Arrived = 8,
    WarehouseReceiving = 9,
    QualityCheck = 10,
    GrnGenerated = 11,
}

public class MaterialPipelineStockDto
{
    public Guid Id { get; set; }
    public string Reference { get; set; }
    public MaterialPipelineStage Stage { get; set; }
    public string Status { get; set; }
    public decimal Quantity { get; set; }
    public UnitOfMeasureDto Uom { get; set; }
    public ProcurementSource? Route { get; set; }
    public string SupplierName { get; set; }
    public DateTime? ExpectedAvailabilityDate { get; set; }
    public DateTime LastUpdatedAt { get; set; }
    public List<string> Departments { get; set; } = [];
}
