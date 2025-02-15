using DOMAIN.Entities.Base;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.Procurement.Manufacturers;
using DOMAIN.Entities.Procurement.Suppliers;
using DOMAIN.Entities.Shipments;
using DOMAIN.Entities.Warehouses;

namespace DOMAIN.Entities.Checklists;

public class Checklist: BaseEntity
{
    public Guid DistributedRequisitionMaterialId { get; set; }
    public DistributedRequisitionMaterial DistributedRequisitionMaterial{ get; set; }
    public Guid MaterialId { get; set; }
    public Material Material { get; set; }
    public DateTime? CheckedAt { get; set; }
    public Guid ShipmentInvoiceId { get; set; }
    public ShipmentInvoice ShipmentInvoice { get; set; }
    public Guid SupplierId { get; set; }
    public Supplier Supplier { get; set; }
    public Guid ManufacturerId { get; set; }
    public Manufacturer Manufacturer { get; set; }
    public bool CertificateOfAnalysisDelivered { get; set; }
    public bool VisibleLabelling { get; set; }
    public Intactness IntactnessStatus { get; set; }
    public ConsignmentCarrier ConsignmentCarrierStatus { get; set; }
    public List<MaterialBatch> MaterialBatches { get; set; }
}

public class ChecklistDto
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
    public List<MaterialBatchDto> MaterialBatches { get; set; }
}

public class CreateChecklistRequest
{
    public Guid DistributedRequisitionMaterialId { get; set; }
    public Guid MaterialId { get; set; }
    public DateTime? CheckedAt { get; set; }
    public Guid ShipmentInvoiceId { get; set; }
    public Guid SupplierId { get; set; }
    public Guid ManufacturerId { get; set; }
    public bool CertificateOfAnalysisDelivered { get; set; }
    public bool VisibleLabelling { get; set; }
    public Intactness IntactnessStatus { get; set; }
    public ConsignmentCarrier ConsignmentCarrierStatus { get; set; }
    public List<CreateMaterialBatchRequest> MaterialBatches { get; set; }
}

public enum ConsignmentCarrier
{
    Dirty,
    Clean
}

public enum Intactness
{
    Good,
    Bad
}