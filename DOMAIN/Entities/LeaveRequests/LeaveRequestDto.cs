using System.ComponentModel.DataAnnotations;

namespace DOMAIN.Entities.LeaveRequests;

public class LeaveRequestDto
{
   [Required] public Guid LeaveTypeId { get; set; }

}