using DOMAIN.Entities.Attachments;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Products;

namespace DOMAIN.Entities.ProductStandardTestProcedures;

public class ProductStandardTestProcedureDto : WithAttachment
{
    public string StpNumber { get; set; }
    
    public Guid ProductId { get; set; }
    
    public Product Product { get; set; }
}