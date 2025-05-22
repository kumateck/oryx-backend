using System.ComponentModel.DataAnnotations;

namespace DOMAIN.Entities.ShiftTypes;

public class CreateShiftTypeRequest
{
    [Required] [MaxLength(100)] public string ShiftName { get; set; }
    
    [Required] public RotationType RotationType { get; set; }
    
    [Required, StringLength(7)] public string StartTime { get; set; }
    
    [Required, StringLength(7)] public string EndTime { get; set; }
    
    [Required] public List<DayOfWeek> ApplicableDays { get; set; }
}