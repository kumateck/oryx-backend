using System.ComponentModel.DataAnnotations;

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
    
    public List<Guid> DesignationList { get; set; }

}