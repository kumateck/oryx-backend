using DOMAIN.Entities.Base;
using DOMAIN.Entities.Forms;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.MaterialARD;
using Microsoft.EntityFrameworkCore;

namespace DOMAIN.Entities.MaterialSpecifications;

public class MaterialSpecification : BaseEntity
{
    public string SpecificationNumber { get; set; }
    public string RevisionNumber { get; set; }
    public string SupersedesNumber { get; set; }
    public DateTime EffectiveDate { get; set; }
    public DateTime ReviewDate { get; set; }
    
    public Guid FormId { get; set; }
    public Form Form { get; set; } 
    public List<TestSpecification> TestSpecifications { get; set; } = [];
    
    public Guid MaterialAnalyticalRawDataId { get; set; }
    public MaterialAnalyticalRawData MaterialAnalyticalRawData { get; set; }
    public Guid MaterialId { get; set; }
    public Material Material { get; set; }
    
}

[Owned] 
public class TestSpecification 
{
    public int SrNumber { get; set; }
    public string Name { get; set; }
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
