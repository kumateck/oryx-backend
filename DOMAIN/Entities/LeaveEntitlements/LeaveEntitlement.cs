using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;

namespace DOMAIN.Entities.LeaveEntitlements;

public class LeaveEntitlement: BaseEntity
{
    public Guid EmployeeId { get; set; }
    
    public int Year { get; set; }
    
    public int DaysAllowed { get; set; }
    
}