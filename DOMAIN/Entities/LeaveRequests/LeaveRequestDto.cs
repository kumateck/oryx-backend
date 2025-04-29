

using DOMAIN.Entities.Employees;
using DOMAIN.Entities.LeaveTypes;

namespace DOMAIN.Entities.LeaveRequests;

public class LeaveRequestDto
{
   public Guid Id { get; set; }
   
   public Guid LeaveTypeId { get; set; } 
   
   public LeaveTypeDto LeaveType { get; set; }
   
   public DateTime StartDate { get; set; }
    
   public DateTime EndDate { get; set; }
    
   public string ContactPerson { get; set; }
    
   public string ContactPersonNumber { get; set; }
   
   public RequestCategory RequestCategory { get; set; }
   
   public LeaveStatus RequestStatus { get; set; }
   
   public int UnpaidDays { get; set; }
   
   public int PaidDays { get; set; }
    
   public Guid EmployeeId { get; set; }
   
   public EmployeeDto Employee { get; set; }

}