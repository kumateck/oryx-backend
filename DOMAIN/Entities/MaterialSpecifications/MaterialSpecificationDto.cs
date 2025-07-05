using DOMAIN.Entities.Base;
using DOMAIN.Entities.Materials;

namespace DOMAIN.Entities.MaterialSpecifications;

public class MaterialSpecificationDto : BaseDto
{
    public string SpecificationNumber { get; set; }
    public string RevisionNumber { get; set; }
    public string SupercedesNumber { get; set; }
    public DateTime EffectiveDate { get; set; }
    public DateTime ReviewDate { get; set; }
    public List<TestSpecification> TestSpecifications { get; set; }
    public Guid MaterialId { get; set; }
    public MaterialDto Material { get; set; }
}