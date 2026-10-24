using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Requisitions;

namespace DOMAIN.Entities.Procurement.Distribution;

// Dedicated read contract: no inventory, storage hierarchy, or user DTOs.
public record DistributionLabel(Guid? Id, string Name, string Code, string Description, string Symbol);
public record DistributionMaterial(Guid Id, string Name, string Code, string Description, MaterialKind Kind);
public record DistributionRequest(Guid Id, decimal Quantity, decimal QuantityReceived,
    RequestStatus Status, DistributionLabel UoM);
public record DistributionOrder(Guid? Id, string Code, DistributionLabel Currency);
public record DistributionInvoiceLine(Guid Id, DistributionMaterial Material, DistributionLabel UoM,
    DistributionLabel Manufacturer, DistributionOrder PurchaseOrder, decimal ExpectedQuantity,
    decimal ReceivedQuantity, decimal Price, string PriceUoM, DistributionLabel Currency);
public record DistributionSource(decimal Quantity, DistributionInvoiceLine ShipmentInvoiceItem);
public record DistributionShare(DistributionLabel Department, DistributionRequest RequisitionItem,
    decimal QuantityRequested, decimal QuantityAllocated, decimal QuantityRemaining,
    List<DistributionSource> Distributions);
public record DistributionPageSection(DistributionMaterial Material, decimal TotalQuantity,
    DistributionLabel UoM, List<DistributionShare> Items);
public record MaterialDistributionPageDto(List<DistributionPageSection> Sections);
