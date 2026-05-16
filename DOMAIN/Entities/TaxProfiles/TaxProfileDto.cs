using DOMAIN.Entities.Base;
using SHARED;

namespace DOMAIN.Entities.TaxProfiles;

public class TaxProfileDto : BaseDto
{
    public string Code { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public bool IsActive { get; set; }
    public string AttributesJson { get; set; }
    public CollectionItemDto Country { get; set; }
}