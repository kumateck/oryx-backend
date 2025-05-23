using DOMAIN.Entities.Base;

namespace DOMAIN.Entities.StaffRequisitions;

public class StaffRequisition : BaseEntity
{
    public PositionType PositionType { get; set; }
    
    public int StaffRequired { get; set; }
    
    public string RequestUrgency { get; set; }
    
    public string Comment { get; set; }
    
    public string Qualification { get; set; }
    
    public string EducationalBackground { get; set; }
    
    public string AdditionalRequests { get; set; }
}

public enum PositionType
{
    Budgeted,
    NotBudgeted
}