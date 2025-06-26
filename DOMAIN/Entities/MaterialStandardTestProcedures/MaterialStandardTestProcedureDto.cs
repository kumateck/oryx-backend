using DOMAIN.Entities.Attachments;
using SHARED;

namespace DOMAIN.Entities.MaterialStandardTestProcedures;

public class MaterialStandardTestProcedureDto : WithAttachment
{
    public string StpNumber { get; set; }
    public CollectionItemDto Material { get; set; }
    public string Description { get; set; }
}