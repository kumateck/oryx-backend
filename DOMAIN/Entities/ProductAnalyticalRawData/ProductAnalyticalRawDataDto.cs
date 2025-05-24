namespace DOMAIN.Entities.ProductAnalyticalRawData;

public class ProductAnalyticalRawDataDto
{
    public string StpNumber { get; set; }
    
    public string SpecNumber { get; set; }
    
    public Stage Stage { get; set; }
    
    public string Description { get; set; }
    
    public Guid StpId { get; set; }
    
    public Guid FormId { get; set; }

}