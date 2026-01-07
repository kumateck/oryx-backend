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
        return dbContext.MaterialBatchReservedQuantities
                .AsSplitQuery()
                .IgnoreQueryFilters()
                .Include(m => m.Warehouse)
                .Include(materialBatchReservedQuantity => materialBatchReservedQuantity.UoM)
                .Where(m => m.MaterialBatch.MaterialId == source.Id && !m.DeletedAt.HasValue)
                .Sum(m => m.Quantity);
    }
}