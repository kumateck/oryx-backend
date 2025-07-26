using DOMAIN.Entities.Base;
using DOMAIN.Entities.MaterialARD;
using DOMAIN.Entities.Materials;

namespace DOMAIN.Entities.MaterialSpecifications;

public class MaterialSpecificationDto : BaseDto
{
    public string SpecificationNumber { get; set; }
    public string RevisionNumber { get; set; }
    public string SupersedesNumber { get; set; }
    public DateTime EffectiveDate { get; set; }
    public DateTime ReviewDate { get; set; }
    public Guid MaterialAnalyticalRawDataId { get; set; }
    public MaterialAnalyticalRawDataDto MaterialAnalyticalRawData { get; set; }
    public Guid MaterialId { get; set; }
    public MaterialDto Material { get; set; }
}