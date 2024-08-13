using DOMAIN.Entities.Base;

namespace DOMAIN.Entities.Organizations;

public class Organization : BaseEntity
{
    public string Name { get; set; }
    public string Description { get; set; }
}