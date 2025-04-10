using System.ComponentModel.DataAnnotations;

namespace DOMAIN.Entities.EducationHistories;

public class EducationDto
{
    [StringLength(200)] public string SchoolName { get; set; }
    
    public DateTime StartDate { get; set; }
    
    public DateTime EndDate { get; set; }
    
    [StringLength(100)] public string Major { get; set; }
    
    [StringLength(100)] public string QualificationEarned { get; set; }

}