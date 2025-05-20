using DOMAIN.Entities.Attachments;
using DOMAIN.Entities.Materials;

namespace DOMAIN.Entities.StandardTestProcedures;

public class StandardTestProcedureDto : WithAttachment
{
    public string StpNumber { get; set; }
    
    public Guid MaterialId { get; set; }
    
    public Material Material { get; set; }
    
    public string Description { get; set; }
}