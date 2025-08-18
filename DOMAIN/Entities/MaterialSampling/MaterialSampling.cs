using DOMAIN.Entities.Base;
using DOMAIN.Entities.Grns;
using DOMAIN.Entities.Materials.Batch;

namespace DOMAIN.Entities.MaterialSampling;

public class MaterialSampling : BaseEntity
{
    public string ArNumber { get; set; }
    public Guid GrnId { get; set; }
    public Grn Grn { get; set; }
    public Guid MaterialBatchId { get; set; }
    public MaterialBatch MaterialBatch { get; set; }
    public decimal SampleQuantity { get; set; }
    public DateTime SampleDate { get; set; } = DateTime.UtcNow;
}