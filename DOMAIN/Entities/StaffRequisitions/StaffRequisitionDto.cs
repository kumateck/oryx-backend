using DOMAIN.Entities.Base;
using DOMAIN.Entities.Designations;

namespace DOMAIN.Entities.StaffRequisitions;

public class StaffRequisitionDto : BaseDto
{
    public BudgetStatus BudgetStatus { get; set; }
    
    public int StaffRequired { get; set; }
    
    public string Qualification { get; set; }
    
    public string EducationalQualification { get; set; }
    
    public string AdditionalRequests { get; set; }
    
    public string DepartmentName { get; set; }
    
    public AppointmentType AppointmentType { get; set; }
    
    public StaffRequisitionStatus StaffRequisitionStatus { get; set; }
    
    public DateTime RequestUrgency { get; set; }
    
    public string Justification { get; set; }
    
    public string AdditionalRequirements { get; set; }
    
    public Guid DesignationId { get; set; }
    
    public DesignationDto Designation { get; set; }
    
}