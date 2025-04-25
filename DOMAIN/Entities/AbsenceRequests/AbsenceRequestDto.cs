
using DOMAIN.Entities.Employees;
using DOMAIN.Entities.LeaveTypes;

namespace DOMAIN.Entities.AbsenceRequests;

public class AbsenceRequestDto
{
   public Guid Id { get; set; }
   
   public Guid LeaveTypeId { get; set; }
   
   public LeaveTypeDto LeaveType { get; set; }
   
   public DateTime StartDate { get; set; }
    
   public DateTime EndDate { get; set; }
    
   public Guid EmployeeId { get; set; }
   
   public EmployeeDto Employee { get; set; }

}