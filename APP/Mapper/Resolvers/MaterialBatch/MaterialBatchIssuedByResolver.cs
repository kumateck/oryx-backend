using AutoMapper;
using DOMAIN.Entities.Materials.Batch;
using INFRASTRUCTURE.Context;
using SHARED;

namespace APP.Mapper.Resolvers.MaterialBatch;

public class MaterialBatchIssuedByResolver(ApplicationDbContext dbContext)
    : IValueResolver<DOMAIN.Entities.Materials.Batch.MaterialBatch, object, CollectionItemDto>
{
    public CollectionItemDto Resolve(
        DOMAIN.Entities.Materials.Batch.MaterialBatch source,
        object destination,
        CollectionItemDto destMember,
        ResolutionContext context
    )
    {
        if (source.IssuedBy != null)
            return context.Mapper.Map<CollectionItemDto>(source.IssuedBy);

        if (source.IssuedById == null)
            return null;

        var user = dbContext.Users.Find(source.IssuedById);
        return context.Mapper.Map<CollectionItemDto>(user);
    }
}
