using AutoMapper;
using DOMAIN.Entities.ProductionSchedules;
using INFRASTRUCTURE.Context;

namespace APP.Mapper.Resolvers.MaterialBatch;

public class BatchNumberResolverProductionSchedule(ApplicationDbContext dbContext)
    : IValueResolver<ProductionScheduleProduct, ProductionScheduleProductDto, string>
{
    public string Resolve(
        ProductionScheduleProduct source,
        ProductionScheduleProductDto destination,
        string destMember,
        ResolutionContext context
    )
    {
        return source.BatchNumber
            ?? dbContext
                .BatchManufacturingRecords.FirstOrDefault(b =>
                    b.ProductionScheduleProductId == source.Id
                )
                ?.BatchNumber;
    }
}
