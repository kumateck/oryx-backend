using DOMAIN.Entities.Base;
using DOMAIN.Entities.Designations;

namespace DOMAIN.Entities.LeaveTypes;

public class LeaveType : BaseEntity
{
    public string Name { get; set; }
    
    public bool IsPaid { get; set; }
    
    public bool DeductFromBalance {get; set;}
    
    public int? DeductionLimit { get; set; }
    
    public int NumberOfDays { get; set; }
    
    public bool IsActive { get; set; }
    public List<Designation> Designations { get; set; }
}