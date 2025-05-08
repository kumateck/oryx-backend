using AutoMapper;
using DOMAIN.Entities.PurchaseOrders;
using DOMAIN.Entities.Requisitions;
using INFRASTRUCTURE.Context;

namespace APP.Mapper.Resolvers;

public class CanReassignPurchaseOrderItemResolver(ApplicationDbContext dbContext) : IValueResolver<PurchaseOrderItem, PurchaseOrderItemDto, bool>
{
    public bool Resolve(PurchaseOrderItem source, PurchaseOrderItemDto destination, bool destMember, ResolutionContext context)
    {
        return dbContext.SupplierQuotationItems.Count(i =>
            i.Status == SupplierQuotationItemStatus.NotUsed && i.MaterialId == source.MaterialId && i.Quantity == source.Quantity 
            && i.UoMId == source.UoMId) > 0;
    }
}