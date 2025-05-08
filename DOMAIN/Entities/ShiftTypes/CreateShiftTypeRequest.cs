using System.ComponentModel.DataAnnotations;

namespace DOMAIN.Entities.ShiftTypes;

public class CreateShiftTypeRequest
{
    [Required] [MaxLength(100)] public string ShiftName { get; set; }
    
    [Required] public RotationType RotationType { get; set; }
    
    [Required] public DateTime StartTime { get; set; }
    
    [Required] public DateTime EndTime { get; set; }
    
    [Required] public List<DayOfWeek> ApplicableDays { get; set; }
}