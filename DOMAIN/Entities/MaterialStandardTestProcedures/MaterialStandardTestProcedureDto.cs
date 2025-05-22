using DOMAIN.Entities.Attachments;
using DOMAIN.Entities.Materials;

namespace DOMAIN.Entities.MaterialStandardTestProcedures;

public class MaterialStandardTestProcedureDto : WithAttachment
{
    public Guid Id { get; set; }
    
    public string StpNumber { get; set; }
    
    public Guid MaterialId { get; set; }
    
    public Material Material { get; set; }
    
    public string Description { get; set; }
}