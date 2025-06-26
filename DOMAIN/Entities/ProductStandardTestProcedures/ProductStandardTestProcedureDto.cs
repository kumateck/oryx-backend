using DOMAIN.Entities.Attachments;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Products;
using SHARED;

namespace DOMAIN.Entities.ProductStandardTestProcedures;

public class ProductStandardTestProcedureDto : WithAttachment
{
    public string StpNumber { get; set; }
    public CollectionItemDto Product { get; set; }
}