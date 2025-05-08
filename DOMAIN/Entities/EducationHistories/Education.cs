using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace DOMAIN.Entities.EducationHistories;

[Owned]
public class Education
{
    
    [StringLength(200)] public string SchoolName { get; set; }
    
    public DateTime StartDate { get; set; }
    
    public DateTime EndDate { get; set; }
    
    [StringLength(100)] public string Major { get; set; }
    
    [StringLength(100)] public string QualificationEarned { get; set; }

}