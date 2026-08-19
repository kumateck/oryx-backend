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
        MaterialSpecification materialSpec;

        if (
            context.TryGetItems(out var items)
            && items.TryGetValue("MaterialSpecifications", out var specsObj)
            && specsObj is Dictionary<Guid, MaterialSpecification> specsByMaterialId
        )
        {
            specsByMaterialId.TryGetValue(source.Id, out materialSpec);
        }
        else
        {
            materialSpec = dbContext.MaterialSpecifications
                .FirstOrDefault(m => m.MaterialId == source.Id);
        }

        if (materialSpec == null) return null;

        return new MaterialSpecificationReducedDto
        {
            Id = materialSpec.Id,
            SpecificationNumber = materialSpec.SpecificationNumber
        };
    }
}