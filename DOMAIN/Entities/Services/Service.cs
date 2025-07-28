using DOMAIN.Entities.Base;
using DOMAIN.Entities.ServiceProviders;

namespace DOMAIN.Entities.Services;

public class Service : BaseEntity
{
    public string Name { get; set; }
    public string Code { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; } = true;
    public string Description { get; set; }
    public List<ServiceProvider> ServiceProviders { get; set; }
}