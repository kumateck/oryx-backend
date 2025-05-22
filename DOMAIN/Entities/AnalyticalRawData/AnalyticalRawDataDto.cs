using DOMAIN.Entities.Base;
using DOMAIN.Entities.MaterialStandardTestProcedures;

namespace DOMAIN.Entities.AnalyticalRawData;

public class AnalyticalRawDataDto : BaseDto
{
    public string StpNumber { get; set; }
    
    public string SpecNumber { get; set; }
    
    public string Description { get; set; }
    
    public Guid StpId { get; set; }
    
    public Guid FormId { get; set; }

}