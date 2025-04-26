using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;

namespace DOMAIN.Entities.ShiftSchedules;

public class ShiftScheduleDto: BaseDto
{
   [Required] [MaxLength(150)] public string ScheduleName { get; set; }

}