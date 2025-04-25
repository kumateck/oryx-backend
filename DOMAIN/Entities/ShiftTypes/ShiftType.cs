using DOMAIN.Entities.Base;

namespace DOMAIN.Entities.ShiftTypes;

public class ShiftType: BaseEntity
{
    public string ShiftName { get; set; }
    
    public RotationType RotationType { get; set; }
    
    public DateTime StartTime { get; set; }
    
    public DateTime EndTime { get; set; }
    
    public List<DayOfWeek> ApplicableDays { get; set; }
    
}

public enum RotationType
{
    Fixed,
    Rotational
}