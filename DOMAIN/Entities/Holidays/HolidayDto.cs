using DOMAIN.Entities.Base;

namespace DOMAIN.Entities.Holidays;

public class HolidayDto : BaseDto
{
    public string Name { get; set; } 
    
    public DateTime Date { get; set; }
    
    public string Description { get; set; }
}