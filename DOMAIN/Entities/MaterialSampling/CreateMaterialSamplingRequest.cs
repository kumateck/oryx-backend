using System.ComponentModel.DataAnnotations;

namespace DOMAIN.Entities.MaterialSampling;

public class CreateMaterialSamplingRequest
{
   [Required] public Guid GrnId { get; set; }
   
   [Required] public string ArNumber { get; set; }
   
   [Required] public string SampleQuantity { get; set; }
}