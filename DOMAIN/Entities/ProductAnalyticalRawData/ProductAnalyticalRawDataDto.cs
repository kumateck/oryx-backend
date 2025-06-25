using DOMAIN.Entities.Attachments;
using DOMAIN.Entities.Forms;
using SHARED;

namespace DOMAIN.Entities.ProductAnalyticalRawData;

public class ProductAnalyticalRawDataDto : WithAttachment
{
    public string StpNumber { get; set; }
    
    public string SpecNumber { get; set; }
    
    public Stage Stage { get; set; }
    
    public string Description { get; set; }
    
    public FormDto Form { get; set; }
    public string ProductName { get; set; }

}