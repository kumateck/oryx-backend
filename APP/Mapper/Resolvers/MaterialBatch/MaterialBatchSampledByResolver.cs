using AutoMapper;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.MaterialSampling;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;

namespace APP.Mapper.Resolvers.MaterialBatch;

public class MaterialBatchSampledByResolver(ApplicationDbContext dbContext)
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
            var s = samplings.FirstOrDefault(s => s.MaterialBatchId == source.Id);
            return s?.CreatedBy != null ? $"{s.CreatedBy.FirstName} {s.CreatedBy.LastName}" : null;
        }

        var sampling = dbContext
            .MaterialSamplings.Include(s => s.CreatedBy)
            .FirstOrDefault(s => s.MaterialBatchId == source.Id);

        return sampling?.CreatedBy != null
            ? $"{sampling.CreatedBy.FirstName} {sampling.CreatedBy.LastName}"
            : null;
    }
}
