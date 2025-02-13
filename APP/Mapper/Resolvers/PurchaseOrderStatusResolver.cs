using AutoMapper;
using DOMAIN.Entities.PurchaseOrders;
using INFRASTRUCTURE.Context;

namespace APP.Mapper.Resolvers;

public class PurchaseOrderStatusResolver(ApplicationDbContext context)
    : IValueResolver<PurchaseOrder, PurchaseOrderDto, PurchaseOrderAttachmentStatus>
{
    public PurchaseOrderAttachmentStatus Resolve(
        PurchaseOrder source, 
        PurchaseOrderDto destination,
        PurchaseOrderAttachmentStatus destMember, 
        ResolutionContext context1)
    {
        var totalItems = source.Items.Count;

        if (totalItems == 0)
            return PurchaseOrderAttachmentStatus.None;

        var linkedItemsCount = context.ShipmentInvoicesItems
            .Count(sii => sii.PurchaseOrderId == source.Id);

        if (linkedItemsCount == 0)
            return PurchaseOrderAttachmentStatus.None; // Not linked at all

        return linkedItemsCount < totalItems ? PurchaseOrderAttachmentStatus.Partial : // Some items linked, some not
            PurchaseOrderAttachmentStatus.Full; // All items linked
    }
}
