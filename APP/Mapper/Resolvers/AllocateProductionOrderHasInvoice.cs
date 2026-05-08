using AutoMapper;
using DOMAIN.Entities.ProductionOrders;
using INFRASTRUCTURE.Context;

namespace APP.Mapper.Resolvers;

public class AllocateProductionOrderHasInvoice(ApplicationDbContext dbContext)
    : IValueResolver<AllocateProductionOrder, AllocateProductionOrderDto, bool>
{
    public bool Resolve(
        AllocateProductionOrder source,
        AllocateProductionOrderDto destination,
        bool destMember,
        ResolutionContext context
    )
    {
        return dbContext.ProformaInvoices.Any(i => i.AllocateProductionOrderId == source.Id);
    }
}

public class AllocateProductionOrderHasWayBill(ApplicationDbContext dbContext)
    : IValueResolver<AllocateProductionOrder, AllocateProductionOrderDto, bool>
{
    public bool Resolve(
        AllocateProductionOrder source,
        AllocateProductionOrderDto destination,
        bool destMember,
        ResolutionContext context
    )
    {
        return dbContext.ProductionOrderWaybills.Any(i => i.AllocateProductionOrderId == source.Id);
    }
}

public class AllocateProductionOrderInvoiceCode(ApplicationDbContext dbContext)
    : IValueResolver<AllocateProductionOrder, AllocateProductionOrderDto, string>
{
    public string Resolve(
        AllocateProductionOrder source,
        AllocateProductionOrderDto destination,
        string destMember,
        ResolutionContext context
    )
    {
        return dbContext
            .ProformaInvoices.FirstOrDefault(i => i.AllocateProductionOrderId == source.Id)
            ?.Code;
    }
}
