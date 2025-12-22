using DOMAIN.Entities.Grns;
using DOMAIN.Entities.Users;
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
    public string IssueNumber { get; set; }
    public UserDto IssuedBy { get; set; }
    public DateTime? IssuedAt { get; set; }
}