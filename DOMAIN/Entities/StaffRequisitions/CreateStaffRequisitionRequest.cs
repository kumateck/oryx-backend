using System.ComponentModel.DataAnnotations;

namespace DOMAIN.Entities.StaffRequisitions;

public class CreateStaffRequisitionRequest
{
    [Required, Range(1, int.MaxValue, ErrorMessage = "Staff must be greater than 0")] public int StaffRequired { get; set; }
    
}