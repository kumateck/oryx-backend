using AutoMapper;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.MaterialSampling;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;

namespace APP.Mapper.Resolvers.MaterialBatch;

public class MaterialBatchSampleQuantityResolver(ApplicationDbContext dbContext)
    : IValueResolver<DOMAIN.Entities.Materials.Batch.MaterialBatch, object, decimal>
{
    public decimal Resolve(
        DOMAIN.Entities.Materials.Batch.MaterialBatch source,
        object destination,
        decimal destMember,
        ResolutionContext context
    )
    {
        if (
            context.TryGetItems(out var items)
            && items.TryGetValue("Samplings", out var samplingsObj)
            && samplingsObj is List<MaterialSampling> samplings
        )
        {
            return samplings.FirstOrDefault(s => s.MaterialBatchId == source.Id)?.SampleQuantity
                ?? 0;
        }

        return dbContext
            .MaterialSamplings.Where(s => s.MaterialBatchId == source.Id)
            .Select(s => s.SampleQuantity)
            .FirstOrDefault();
    }
}
