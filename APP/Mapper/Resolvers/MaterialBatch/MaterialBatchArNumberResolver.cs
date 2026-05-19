using AutoMapper;
using DOMAIN.Entities.MaterialSampling;
using INFRASTRUCTURE.Context;

namespace APP.Mapper.Resolvers.MaterialBatch;

public class MaterialBatchArNumberResolver(ApplicationDbContext dbContext)
    : IValueResolver<DOMAIN.Entities.Materials.Batch.MaterialBatch, object, string>
{
    public string Resolve(
        DOMAIN.Entities.Materials.Batch.MaterialBatch source,
        object destination,
        string destMember,
        ResolutionContext context
    )
    {
        if (
            context.TryGetItems(out var items)
            && items.TryGetValue("Samplings", out var samplingsObj)
            && samplingsObj is List<MaterialSampling> samplings
        )
        {
            return samplings.FirstOrDefault(s => s.MaterialBatchId == source.Id)?.ArNumber;
        }

        return dbContext
            .MaterialSamplings.Where(s => s.MaterialBatchId == source.Id)
            .Select(s => s.ArNumber)
            .FirstOrDefault();
    }
}
