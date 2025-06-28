using DOMAIN.Entities.Attachments;
using DOMAIN.Entities.Forms;
using DOMAIN.Entities.ProductStandardTestProcedures;
using SHARED;

namespace DOMAIN.Entities.ProductAnalyticalRawData;

public class ProductAnalyticalRawDataDto : WithAttachment
{
    public string SpecNumber { get; set; }
    public Stage Stage { get; set; }
    public string Description { get; set; }
    public CollectionItemDto Form { get; set; }
    public ProductStandardTestProcedureDto ProductStandardTestProcedure { get; set; }
}