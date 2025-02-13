using AutoMapper;
using DOMAIN.Entities.Shipments;

namespace APP.Mapper.Resolvers;

public class ShipmentInvoiceResolver : IValueResolver<ShipmentInvoice, ShipmentInvoiceDto, ShipmentInvoiceStatus>
{
    public ShipmentInvoiceStatus Resolve(ShipmentInvoice source, ShipmentInvoiceDto destination, ShipmentInvoiceStatus destMember,
        ResolutionContext context)
    {
        if (!source.Items.Select(i => i.PurchaseOrderId).Distinct().Any())
            return ShipmentInvoiceStatus.None;

        return source.Items.Select(i => i.PurchaseOrderId).Distinct().Count() > 1 ? ShipmentInvoiceStatus.Partial : ShipmentInvoiceStatus.Full;
    }
}