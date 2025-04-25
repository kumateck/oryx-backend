using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Designations;

namespace DOMAIN.Entities.LeaveTypes;

public class LeaveTypeDto
{
    
    public Guid Id { get; set; }
    
    public string Name { get; set; }
    
    public bool IsPaid { get; set; }
    
    public bool DeductFromBalance {get; set;}
    
    public int? DeductionLimit { get; set; }
    
    public int NumberOfDays { get; set; }
    
    public bool IsActive { get; set; }

    public List<DesignationDto> Designations { get; set; } = [];

}