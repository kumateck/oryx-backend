using System.ComponentModel.DataAnnotations;

namespace DOMAIN.Entities.LeaveTypes;

public class CreateLeaveTypeRequest
{
    [Required] [StringLength(200)] public string Name { get; set; }
    
    [Required] public bool IsPaid { get; set; }
    
    [Required] public bool DeductFromBalance {get; set;}
    
    public int? DeductionLimit { get; set; }
    
    [Range(1, 366, ErrorMessage = "Days must be between 1 and 366.")] [Required]
    public int NumberOfDays { get; set; }
    
    [Required] public bool IsActive { get; set; }
    
    [Required] public List<Guid> DesignationList { get; set; }
}