using DOMAIN.Entities.Base;
using DOMAIN.Entities.Materials;

namespace DOMAIN.Entities.MaterialStandardTestProcedures;

public class MaterialStandardTestProcedure : BaseEntity
{
    public string StpNumber { get; set; }
    
    public Guid MaterialId { get; set; }
    
    public Material Material { get; set; }
    
    public string Description { get; set; }
    
}