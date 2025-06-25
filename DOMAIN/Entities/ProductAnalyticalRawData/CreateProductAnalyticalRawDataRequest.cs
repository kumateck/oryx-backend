using System.ComponentModel.DataAnnotations;

namespace DOMAIN.Entities.ProductAnalyticalRawData;

public class CreateProductAnalyticalRawDataRequest
{
    [Required] public string StpNumber { get; set; }
    
    [Required] public string SpecNumber { get; set; }
    
    [Required] public Stage Stage { get; set; }
    
    public string Description { get; set; }
    
    [Required] public Guid StpId { get; set; }
    public Guid? BatchManufacturingRecordId { get; set; }
    [Required] public Guid FormId { get; set; }
}