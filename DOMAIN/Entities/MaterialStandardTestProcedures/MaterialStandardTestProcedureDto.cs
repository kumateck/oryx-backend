using DOMAIN.Entities.Attachments;
using DOMAIN.Entities.Materials;

namespace DOMAIN.Entities.MaterialStandardTestProcedures;

public class MaterialStandardTestProcedureDto : WithAttachment
{
    public string StpNumber { get; set; }
    
    public Guid MaterialId { get; set; }
    public string MaterialName { get; set; }
    
    public string Description { get; set; }
}