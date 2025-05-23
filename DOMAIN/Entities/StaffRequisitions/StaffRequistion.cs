using DOMAIN.Entities.Base;

namespace DOMAIN.Entities.StaffRequisitions;

public class StaffRequisition : BaseEntity
{
    public PositionType PositionType { get; set; }
    
    public int StaffRequired { get; set; }
    
    public DateTime RequestUrgency { get; set; }
    
    public string Justification { get; set; }
    
    public string Qualification { get; set; }
    
    public string EducationalQualification { get; set; }
    
    public string AdditionalRequirements { get; set; }
    
}

public enum PositionType
{
    Budgeted,
    NotBudgeted
}

public enum AppointmentType
{
    NewAppointment,
    Replacement
}