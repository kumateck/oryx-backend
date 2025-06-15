using DOMAIN.Entities.Grns;

namespace DOMAIN.Entities.MaterialSampling;

public class MaterialSamplingDto
{
    public GrnDto GrnDto { get; set; }
    
    public string ArNumber { get; set; }
    
    public Guid GrnId { get; set; }
    
    public string SampleQuantity { get; set; }
    
    public DateTime SampleDate { get; set; } 
}