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
        // Yield on packing lists still waiting for approval. It is already counted in AllocatedQuantity
        // (stock is taken when a packing list is created), so this is shown, never subtracted again.
        return dbContext.AllocateProductionOrders
            .Where(a => !a.Approved)
            .SelectMany(a => a.Products)
            .SelectMany(p => p.FulfilledQuantities)
            .Where(f => f.FinishedGoodsTransferNoteId == source.Id)
            .Sum(f => f.YieldQuantity);
    }
}