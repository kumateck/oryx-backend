using DOMAIN.Entities.Base;

namespace DOMAIN.Entities.Designation;

public class DesignationDto: BaseDto
{
    
    public string Name { get; set; }
    
    public string Description { get; set; }
    
}