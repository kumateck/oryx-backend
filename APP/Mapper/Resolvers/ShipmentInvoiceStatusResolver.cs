using AutoMapper;
using DOMAIN.Entities.Shipments;
using INFRASTRUCTURE.Context;

namespace APP.Mapper.Resolvers;

public class ShipmentInvoiceStatusResolver(ApplicationDbContext dbContext) : IValueResolver<ShipmentInvoice, ShipmentInvoiceDto, bool>
{
    public bool Resolve(ShipmentInvoice source, ShipmentInvoiceDto destination, bool destMember,
        ResolutionContext context)
    {
        return dbContext.ShipmentDocuments.Any(s => s.ShipmentInvoiceId == source.Id);
    }
}

public class ShipmentInvoiceListStatusResolver(ApplicationDbContext dbContext) : IValueResolver<ShipmentInvoice, ShipmentInvoiceListDto, bool>
{
    public bool Resolve(ShipmentInvoice source, ShipmentInvoiceListDto destination, bool destMember,
        ResolutionContext context)
    {
        return dbContext.ShipmentDocuments.Any(s => s.ShipmentInvoiceId == source.Id);
    }
}