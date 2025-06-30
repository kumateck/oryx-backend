using DOMAIN.Entities.Base;

namespace DOMAIN.Entities.ShiftTypes;

public class ShiftTypeDto: BaseDto
{
    public string ShiftName { get; set; }
    
    public RotationType RotationType { get; set; }
    
    public string StartTime { get; set; }
    
    public string EndTime { get; set; }
    
    public List<DayOfWeek> ApplicableDays { get; set; }
}

public class MinimalShiftTypeDto
{
    public Guid ShiftTypeId { get; set; }
    
    public string ShiftName { get; set; }
}