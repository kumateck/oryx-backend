using DOMAIN.Entities.Attachments;
using DOMAIN.Entities.MaterialStandardTestProcedures;
using DOMAIN.Entities.UniformityOfWeights;
using SHARED;

namespace DOMAIN.Entities.MaterialAnalyticalRawData;

public class MaterialAnalyticalRawDataDto : WithAttachment
{
    public string SpecNumber { get; set; }
    
    public string Description { get; set; }
    
    public MaterialStandardTestProcedureDto MaterialStandardTestProcedure { get; set; }
    
    public CollectionItemDto Form { get; set; }
    public UniformityOfWeightDto UniformityOfWeight { get; set; }
}