using AutoMapper;
using DOMAIN.Entities.Warehouses;
using INFRASTRUCTURE.Context;

namespace APP.Mapper.Resolvers;

public class DistributedRequisitionItemResolverDistributedQuantity(ApplicationDbContext dbContext) : IValueResolver<DistributedRequisitionItem, DistributedRequisitionItemDto, decimal>
{
    public decimal Resolve(DistributedRequisitionItem source, DistributedRequisitionItemDto destination, decimal destMember,
        ResolutionContext context)
    {
        return dbContext.DistributeMaterials
            .Where(d => d.DistributedRequisitionItemId == source.Id && d.Status == DistributeMaterialStatus.Pending)
            .Sum(d => d.Quantity);
    }
}

public class DistributedRequisitionItemResolverAssignedQuantity(ApplicationDbContext dbContext) : IValueResolver<DistributedRequisitionItem, DistributedRequisitionItemDto, decimal>
{
    public decimal Resolve(DistributedRequisitionItem source, DistributedRequisitionItemDto destination, decimal destMember,
        ResolutionContext context)
    {
        return dbContext.DistributeMaterials
            .Where(d => d.DistributedRequisitionItemId == source.Id && d.Status == DistributeMaterialStatus.Distributed)
            .Sum(d => d.Quantity);
    }
}