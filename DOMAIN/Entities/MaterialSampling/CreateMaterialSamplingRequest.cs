using System.ComponentModel.DataAnnotations;

namespace DOMAIN.Entities.MaterialSampling;

public class CreateMaterialSamplingRequest
{
   [Required] public Guid GrnId { get; set; }
   
   [Required] public string ArNumber { get; set; }
   public Guid MaterialBatchId { get; set; }
   
   [Required] public decimal SampleQuantity { get; set; }
}