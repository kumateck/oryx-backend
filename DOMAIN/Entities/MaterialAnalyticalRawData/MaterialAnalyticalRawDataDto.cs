using DOMAIN.Entities.Attachments;
using DOMAIN.Entities.Base;

namespace DOMAIN.Entities.MaterialAnalyticalRawData;

public class MaterialAnalyticalRawDataDto : WithAttachment
{
    public string StpNumber { get; set; }
    
    public string SpecNumber { get; set; }
    
    public string Description { get; set; }
    
    public string MaterialName { get; set; }
    
    public Guid StpId { get; set; }
    
    public Guid FormId { get; set; }
    public string FormName { get; set; }
    
}