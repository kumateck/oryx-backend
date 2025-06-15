using DOMAIN.Entities.Base;
using DOMAIN.Entities.Grns;

namespace DOMAIN.Entities.MaterialSampling;

public class MaterialSampling : BaseEntity
{
    public string ArNumber { get; set; }
    
    public Guid GrnId { get; set; }
    
    public string SampleQuantity { get; set; }
    
    public DateTime SampleDate { get; set; } = DateTime.UtcNow;
    public Grn Grn { get; set; }
}