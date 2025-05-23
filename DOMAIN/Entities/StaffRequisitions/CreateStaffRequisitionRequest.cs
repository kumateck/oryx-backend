using System.ComponentModel.DataAnnotations;

namespace DOMAIN.Entities.StaffRequisitions;

public class CreateStaffRequisitionRequest
{
    [Required] public PositionType PositionType { get; set; }
    
    [Required, Range(1, int.MaxValue, ErrorMessage = "Staff must be greater than 0")] public int StaffRequired { get; set; }
    
    [Required, StringLength(255)] public string EducationalBackground { get; set; }
    
    [Required, StringLength(255)] public string Qualification { get; set; }
    
    
    public string AdditionalRequests { get; set; }
    
    
}