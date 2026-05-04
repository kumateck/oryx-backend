using AutoMapper;
using DOMAIN.Entities.Users;
using INFRASTRUCTURE.Context;

namespace APP.Mapper.Resolvers.MaterialBatch;

public class MaterialBatchUserIssuedByResolver(ApplicationDbContext dbContext)
    : IValueResolver<DOMAIN.Entities.Materials.Batch.MaterialBatch, object, UserDto>
{
    public UserDto Resolve(
        DOMAIN.Entities.Materials.Batch.MaterialBatch source,
        object destination,
        UserDto destMember,
        ResolutionContext context
    )
    {
        if (source.IssuedBy != null)
            return context.Mapper.Map<UserDto>(source.IssuedBy);

        if (source.IssuedById == null)
            return null;

        var user = dbContext.Users.Find(source.IssuedById);
        return context.Mapper.Map<UserDto>(user);
    }
}
