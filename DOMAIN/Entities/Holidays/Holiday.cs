using DOMAIN.Entities.Base;
using DOMAIN.Entities.ShiftSchedules;

namespace DOMAIN.Entities.Holidays;

public class Holiday : BaseEntity
{
    public string Name { get; set; }
    
    public DateTime Date { get; set; }
    
    public List<ShiftSchedule> ShiftSchedules { get; set; }
    
    public string Description { get; set; }
    
}