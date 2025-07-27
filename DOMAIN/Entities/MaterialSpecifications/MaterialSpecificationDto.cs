using DOMAIN.Entities.Base;
using DOMAIN.Entities.Forms;
using DOMAIN.Entities.MaterialARD;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Users;

namespace DOMAIN.Entities.MaterialSpecifications;

public class MaterialSpecificationDto : BaseDto
{
    public string SpecificationNumber { get; set; }
    public string RevisionNumber { get; set; }
    public string SupersedesNumber { get; set; }
    public DateTime EffectiveDate { get; set; }
    public DateTime ReviewDate { get; set; }
    public Guid FormId { get; set; }
    public FormDto Form { get; set; } 
    public DateTime DueDate {get;set;}
    public string Description {get;set;}
    public Guid UserId { get; set; }
    public UserDto User { get; set; }
    public Guid MaterialAnalyticalRawDataId { get; set; }
    public MaterialAnalyticalRawDataDto MaterialAnalyticalRawData { get; set; }
    public Guid MaterialId { get; set; }
    public MaterialDto Material { get; set; }
}