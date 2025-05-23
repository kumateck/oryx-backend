using DOMAIN.Entities.Base;

namespace DOMAIN.Entities.StaffRequisitions;

public class StaffRequisitionDto : BaseDto
{
    public PositionType PositionType { get; set; }
    
    public int StaffRequired { get; set; }
    
    public string Qualification { get; set; }
    
    public string EducationalBackground { get; set; }
    
    public string AdditionalRequests { get; set; }
    
    
}