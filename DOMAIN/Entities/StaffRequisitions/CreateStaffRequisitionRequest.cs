using System.ComponentModel.DataAnnotations;

namespace DOMAIN.Entities.StaffRequisitions;

public class CreateStaffRequisitionRequest
{
    [Required] public BudgetStatus BudgetStatus { get; set; }
    
    [Required, Range(1, int.MaxValue, ErrorMessage = "Staff required must be greater than 0")] public int StaffRequired { get; set; }
    
    [Required, StringLength(255)] public string EducationalQualification { get; set; }
    
    [Required, StringLength(255)] public string Qualification { get; set; }
    
    [Required] public Guid DesignationId { get; set; }
    
    public string AdditionalRequests { get; set; }
    
    [Required] public AppointmentType AppointmentType { get; set; }
    
    [Required] public DateTime RequestUrgency { get; set; }
    
    public string Justification { get; set; }
    
    public string AdditionalRequirements { get; set; }
    
    public string DepartmentName { get; set; }
}