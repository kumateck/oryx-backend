using AutoMapper;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;

namespace APP.Mapper.Resolvers.MaterialBatch;

public class MaterialBatchManufacturerNameResolver(ApplicationDbContext dbContext)
    : IValueResolver<DOMAIN.Entities.Materials.Batch.MaterialBatch, object, string>
{
    public string Resolve(
        DOMAIN.Entities.Materials.Batch.MaterialBatch source,
        object destination,
        string destMember,
        ResolutionContext context
    )
    {
        if (source.Checklist?.Manufacturer != null)
            return source.Checklist.Manufacturer.Name;

        if (source.ChecklistId == null)
            return null;

        return dbContext
            .Checklists.Include(c => c.Manufacturer)
            .Where(c => c.Id == source.ChecklistId)
            .Select(c => c.Manufacturer.Name)
            .FirstOrDefault();
    }
}
