using AutoMapper;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.MaterialSpecifications;
using INFRASTRUCTURE.Context;

namespace APP.Mapper.Resolvers;

public class MaterialSpecificationResolver(ApplicationDbContext dbContext) : IValueResolver<Material, MaterialDto, MaterialSpecificationReducedDto>
{
    public MaterialSpecificationReducedDto Resolve(Material source, MaterialDto destination, MaterialSpecificationReducedDto destMember,
        ResolutionContext context)
    {
        var materialSpec = dbContext.MaterialSpecifications
            .FirstOrDefault(m => m.MaterialId == source.Id);

        if (materialSpec == null) return null;

        return new MaterialSpecificationReducedDto
        {
            Id = materialSpec.Id,
            SpecificationNumber = materialSpec.SpecificationNumber
        };
    }
}