using DOMAIN.Entities.Base;
using DOMAIN.Entities.Materials;
using Microsoft.EntityFrameworkCore;

namespace DOMAIN.Entities.MaterialSpecifications;

public class MaterialSpecification : BaseEntity
{
    public string SpecificationNumber { get; set; }
    public string RevisionNumber { get; set; }
    public string SupersedesNumber { get; set; }
    public DateTime EffectiveDate { get; set; }
    public DateTime ReviewDate { get; set; }
    public List<TestSpecification> TestSpecifications { get; set; } = [];
    public Guid MaterialId { get; set; }
    public Material Material { get; set; }
    
}

[Owned] 
public class TestSpecification 
{
    public int SrNumber { get; set; }
    public string TestName { get; set; }
    public string ReleaseSpecification { get; set; }
    public MaterialSpecificationReference Reference { get; set; }
    
}

public enum MaterialSpecificationReference
{
    BP,
    USP,
    PhInt, 
    InHouse
}

// public enum TestType
// {
//     WeightOf20Tablets,
//     UniformityOfWeight,
//     DisintegrationTime,
//     Friability,
//     Dissolution,
//     Assay
// }