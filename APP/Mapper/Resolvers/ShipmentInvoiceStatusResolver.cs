using AutoMapper;
using DOMAIN.Entities.Shipments;
using INFRASTRUCTURE.Context;

namespace APP.Mapper.Resolvers;

public class ShipmentInvoiceStatusResolver(ApplicationDbContext dbContext) : IValueResolver<ShipmentInvoice, ShipmentInvoiceDto, ShipmentStatus>
{
    public ShipmentStatus Resolve(ShipmentInvoice source, ShipmentInvoiceDto destination, ShipmentStatus destMember,
        ResolutionContext context)
    {
        return dbContext.ShipmentDocuments.FirstOrDefault(s => s.ShipmentInvoiceId == source.Id)?.Status 
               ?? ShipmentStatus.New;
    }
}