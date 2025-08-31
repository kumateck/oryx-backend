using AutoMapper;
using DOMAIN.Entities.Shipments;
using INFRASTRUCTURE.Context;

namespace APP.Mapper.Resolvers;

public class HasBillingSheetResolver(ApplicationDbContext dbContext) : IValueResolver<ShipmentDocument, ShipmentDocumentDto, bool>
{
    public bool Resolve(ShipmentDocument source, ShipmentDocumentDto destination, bool destMember, ResolutionContext context)
    {
        return dbContext.BillingSheets.Any(x => x.InvoiceId == source.ShipmentInvoiceId);
    }
}