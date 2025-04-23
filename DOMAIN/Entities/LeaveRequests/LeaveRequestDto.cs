using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.LeaveTypes;

namespace DOMAIN.Entities.LeaveRequests;

public class LeaveRequestDto
{
   public Guid Id { get; set; }
   
   public Guid LeaveTypeId { get; set; } 
   
   public DateTime StartDate { get; set; }
    
   public DateTime EndDate { get; set; }
    
   public string ContactPerson { get; set; }
    
   public string ContactPersonNumber { get; set; }
    
   public Guid EmployeeId { get; set; }

}