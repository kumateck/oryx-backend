using DOMAIN.Entities.Base;

namespace DOMAIN.Entities.ShiftTypes;

public class ShiftTypeDto: BaseDto
{
    public string ShiftName { get; set; }
    
    public RotationType RotationType { get; set; }
    
    public DateTime StartTime { get; set; }
    
    public DateTime EndTime { get; set; }
    
    public List<DayOfWeek> ApplicableDays { get; set; }
}