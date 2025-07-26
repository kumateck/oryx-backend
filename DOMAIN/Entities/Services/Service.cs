using DOMAIN.Entities.Base;

namespace DOMAIN.Entities.Services;

public class Service : BaseEntity
{
    public string Name { get; set; }
    public string Code { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; } = true;
    public string Description { get; set; }
}