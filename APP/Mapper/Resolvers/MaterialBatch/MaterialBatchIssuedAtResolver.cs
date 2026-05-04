using AutoMapper;
using DOMAIN.Entities.Materials.Batch;
using INFRASTRUCTURE.Context;

namespace APP.Mapper.Resolvers.MaterialBatch;

public class MaterialBatchIssuedAtResolver
    : IValueResolver<DOMAIN.Entities.Materials.Batch.MaterialBatch, object, DateTime?>
{
    public DateTime? Resolve(
        DOMAIN.Entities.Materials.Batch.MaterialBatch source,
        object destination,
        DateTime? destMember,
        ResolutionContext context
    )
    {
        return source.IssuedAt;
    }
}
