using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Grns;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.Users;

namespace DOMAIN.Entities.MaterialSampling;

public class MaterialSampling : BaseEntity
{
    [StringLength(1000000)] public string ArNumber { get; set; }
    [StringLength(1000000)] public string IssueNumber { get; set; }
    public Guid? IssuedById { get; set; }
    public User IssuedBy { get; set; }
    public DateTime? IssuedAt { get; set; }
    public Guid GrnId { get; set; }
    public Grn Grn { get; set; }
    public Guid? ChemicalArdId { get; set; }
    public Guid? MicrobialArdId { get; set; }
    public bool MicrobialRequired { get; set; }
    public Guid MaterialBatchId { get; set; }
    public MaterialBatch MaterialBatch { get; set; }
    public decimal SampleQuantity { get; set; }
    public DateTime SampleDate { get; set; } = DateTime.UtcNow;
}