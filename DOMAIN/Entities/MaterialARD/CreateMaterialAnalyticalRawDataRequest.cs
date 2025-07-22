using System.ComponentModel.DataAnnotations;

namespace DOMAIN.Entities.MaterialARD;

public class CreateMaterialAnalyticalRawDataRequest
{
    [Required] public string SpecNumber { get; set; }
    
    public string Description { get; set; }
    
    [Required] public Guid StpId { get; set; }
    
    [Required] public Guid FormId { get; set; }
    public Guid? MaterialBatchId { get; set; }

}