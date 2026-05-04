using AutoMapper;
using DOMAIN.Entities.MaterialSampling;
using INFRASTRUCTURE.Context;

namespace APP.Mapper.Resolvers.MaterialBatch;

public class MaterialBatchSampledOnResolver(ApplicationDbContext dbContext)
    : IValueResolver<DOMAIN.Entities.Materials.Batch.MaterialBatch, object, DateTime?>
{
    public DateTime? Resolve(
        DOMAIN.Entities.Materials.Batch.MaterialBatch source,
        object destination,
        DateTime? destMember,
        ResolutionContext context
    )
    {
        if (
            context.Items.TryGetValue("Samplings", out var samplingsObj)
            && samplingsObj is List<MaterialSampling> samplings
        )
        {
            return samplings.FirstOrDefault(s => s.MaterialBatchId == source.Id)?.SampleDate;
        }

        return dbContext
            .MaterialSamplings.Where(s => s.MaterialBatchId == source.Id)
            .Select(s => s.SampleDate)
            .FirstOrDefault();
    }
}
