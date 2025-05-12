using DOMAIN.Entities.ShiftSchedules;

namespace DOMAIN.Entities.Holidays;

public class HolidayDto
{
    public string Name { get; set; } 
    
    public DateTime Date { get; set; }
    
    public List<ShiftScheduleDto> Shifts { get; set; }
    
    public string Description { get; set; }
}