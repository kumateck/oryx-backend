using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.Materials;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;

namespace APP.Mapper.Resolvers;

public class ReservedMaterialStockResolver(ApplicationDbContext dbContext) : 
    IValueResolver<Material, MaterialDto, decimal>
{
    public decimal Resolve(Material source, MaterialDto destination, decimal destMember, ResolutionContext context)
    {
        if (
            context.TryGetItems(out var items)
            && items.TryGetValue("ReservedQuantities", out var reservedObj)
            && reservedObj is Dictionary<Guid, decimal> reservedByMaterialId
        )
        {
            return reservedByMaterialId.GetValueOrDefault(source.Id, 0);
        }

        // Live holds only (not released to the shelf, not issued to the floor): the rule every stock screen uses.
        var reservations = APP.Repository.ProductionMaterialAllocator.UnissuedReservations(dbContext)
            .Where(m => m.MaterialBatch.MaterialId == source.Id);
        return ReservationTotals.Sum(reservations, dbContext.Database.IsNpgsql());
    }
}
