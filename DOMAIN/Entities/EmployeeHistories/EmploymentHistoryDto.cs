using System.ComponentModel.DataAnnotations;

namespace DOMAIN.Entities.EmployeeHistories;

public class EmploymentHistoryDto
{
    [StringLength(150)] public string CompanyName { get; set; }
    
    public DateTime StartDate { get; set; }
    
    public DateTime EndDate { get; set; }
    
    [StringLength(100)] public string Position { get; set; }

}