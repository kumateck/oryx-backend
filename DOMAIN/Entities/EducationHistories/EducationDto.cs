using System.ComponentModel.DataAnnotations;

namespace DOMAIN.Entities.EducationHistories;

public class EducationDto
{
    [StringLength(200)] [Required] public string SchoolName { get; set; }
    
    [Required] public DateTime StartDate { get; set; }
    
    [Required] public DateTime EndDate { get; set; }
    
    [StringLength(100)] [Required] public string Major { get; set; }
    
    [StringLength(100)] [Required] public string QualificationEarned { get; set; }

}