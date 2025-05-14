using DOMAIN.Entities.Base;

namespace DOMAIN.Entities.Holidays;

public class Holiday : BaseEntity
{
    public string Name { get; set; }
    
    public DateTime Date { get; set; }
    
    public string Description { get; set; }
    
}