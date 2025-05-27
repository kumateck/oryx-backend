using DOMAIN.Entities.Base;
using DOMAIN.Entities.MaterialStandardTestProcedures;

namespace DOMAIN.Entities.MaterialAnalyticalRawData;

public class MaterialAnalyticalRawDataDto : BaseDto
{
    public string StpNumber { get; set; }
    
    public string SpecNumber { get; set; }
    
    public string Description { get; set; }
    
    public Guid StpId { get; set; }
    
    public Guid FormId { get; set; }
    
    public MaterialStandardTestProcedure MaterialStandardTestProcedure { get; set; }

}