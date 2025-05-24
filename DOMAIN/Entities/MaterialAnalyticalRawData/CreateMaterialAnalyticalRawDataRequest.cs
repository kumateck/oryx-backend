using System.ComponentModel.DataAnnotations;

namespace DOMAIN.Entities.MaterialAnalyticalRawData;

public class CreateMaterialAnalyticalRawDataRequest
{
    [Required] public string StpNumber { get; set; }
    
    [Required] public string SpecNumber { get; set; }
    
    public string Description { get; set; }
    
    [Required] public Guid StpId { get; set; }
    
    [Required] public Guid FormId { get; set; }

}