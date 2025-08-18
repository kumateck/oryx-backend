using DOMAIN.Entities.Grns;
using SHARED;

namespace DOMAIN.Entities.MaterialSampling;

public class MaterialSamplingDto
{
    public GrnDto GrnDto { get; set; }
    public CollectionItemDto MaterialBatch { get; set; }
    
    public string ArNumber { get; set; }
    
    public Guid GrnId { get; set; }
    
    public decimal SampleQuantity { get; set; }
    
    public DateTime SampleDate { get; set; } 
}