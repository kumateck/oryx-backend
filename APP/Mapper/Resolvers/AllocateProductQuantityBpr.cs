using AutoMapper;
using DOMAIN.Entities.ProductionOrders;
using DOMAIN.Entities.Products.Production;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;

namespace APP.Mapper.Resolvers;

public class AllocateProductQuantityBpr(ApplicationDbContext dbContext, IMapper mapper) : IValueResolver<AllocateProductQuantity, AllocateProductQuantityDto, BatchPackagingRecordDto>
{
    public BatchPackagingRecordDto Resolve(AllocateProductQuantity source, AllocateProductQuantityDto destination,
        BatchPackagingRecordDto destMember, ResolutionContext context)
    {
        var bpr = dbContext.BatchPackagingRecords
            .AsSplitQuery()
            .Include(b => b.ProductPacking)
            .ThenInclude(b => b.PackingLists)
            .ThenInclude(b => b.Uom)
            .FirstOrDefault(p =>
                p.ProductionActivityStepId ==
                source.FinishedGoodsTransferNote.BatchManufacturingRecord.ProductionActivityStepId);
        return bpr == null ? null : mapper.Map<BatchPackagingRecordDto>(bpr);
    }
}