using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;

namespace DOMAIN.Entities.CompanyWorkingDays;

public class CompanyWorkingDays : BaseEntity
{
    public DayOfWeek Day { get; set; }
    
    public bool IsWorkingDay { get; set; }
    
    [StringLength(8)] public string StartTime { get; set; }
    
    [StringLength(8)] public string EndTime { get; set; }
}