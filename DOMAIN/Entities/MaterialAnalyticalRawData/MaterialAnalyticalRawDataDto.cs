using DOMAIN.Entities.Attachments;
using DOMAIN.Entities.MaterialStandardTestProcedures;
using SHARED;

namespace DOMAIN.Entities.MaterialAnalyticalRawData;

public class MaterialAnalyticalRawDataDto : WithAttachment
{
    public string StpNumber { get; set; }
    
    public string SpecNumber { get; set; }
    
    public string Description { get; set; }
    
    public MaterialStandardTestProcedureDto MaterialStandardTestProcedure { get; set; }
    
    public CollectionItemDto Form { get; set; }
    
}