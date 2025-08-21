using AutoMapper;
using DOMAIN.Entities.Materials.Batch;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;

namespace APP.Mapper.Resolvers;

public class PendingAllocatedQuantityResolver(ApplicationDbContext dbContext) : IValueResolver<FinishedGoodsTransferNote, FinishedGoodsTransferNoteDto, decimal>
{
    public decimal Resolve(FinishedGoodsTransferNote source, FinishedGoodsTransferNoteDto destination, decimal destMember,
        ResolutionContext context)
    {
        return dbContext.AllocateProductionOrders
            .AsSplitQuery()
            .Include(a => a.Products)
            .ThenInclude(p => p.FulfilledQuantities)
            .Where(a => a.Products
                .Any(pr => pr.FulfilledQuantities
                    .Any(f => f.FinishedGoodsTransferNoteId == source.Id)))
            .SelectMany(a => a.Products) 
            .SelectMany(p => p.FulfilledQuantities)
            .Where(f => f.FinishedGoodsTransferNoteId == source.Id) 
            .Sum(f => f.Quantity); 
    }
}