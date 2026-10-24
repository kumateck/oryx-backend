using DOMAIN.Entities.Base;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Procurement.Distribution;
using SHARED;

namespace APP.Services.Procurement;

internal static class MaterialDistributionPageResponse
{
    private static DistributionMaterial Material(MaterialDto material) => material == null ? null
        : new(material.Id, material.Name, material.Code, material.Description, material.Kind);
    private static DistributionLabel Unit(UnitOfMeasureDto unit) => unit == null ? null
        : new(unit.Id, unit.Name, null, null, unit.Symbol);
    private static DistributionLabel Label(CollectionItemDto item) => item == null ? null
        : new(item.Id, item.Name, item.Code, item.Description, item.Symbol);

    // Calculation uses the same DTOs as confirmation, but only this small contract is serialized.
    public static DistributionPageSection From(MaterialDistributionSection section) => new(
        Material(section.Material), section.TotalQuantity, Unit(section.UoM),
        section.Items.Select(share => new DistributionShare(
            share.Department == null ? null : new(share.Department.Id, share.Department.Name,
                share.Department.Code, share.Department.Description, null),
            new(share.RequisitionItem.Id, share.RequisitionItem.Quantity,
                share.RequisitionItem.QuantityReceived, share.RequisitionItem.Status,
                Unit(share.RequisitionItem.UoM)),
            share.QuantityRequested, share.QuantityAllocated, share.QuantityRemaining,
            share.Distributions.Select(source => {
                var line = source.ShipmentInvoiceItem;
                var currency = line.Currency == null ? null
                    : new DistributionLabel(line.Currency.Id, line.Currency.Name, null, null, line.Currency.Symbol);
                return new DistributionSource(source.Quantity, new(line.Id, Material(line.Material),
                    Unit(line.UoM), Label(line.Manufacturer), line.PurchaseOrder == null ? null
                        : new(line.PurchaseOrder.Id, line.PurchaseOrder.Code, currency),
                    line.ExpectedQuantity, line.ReceivedQuantity, line.Price, line.PriceUoM, currency));
            }).ToList())).ToList());
}
