namespace DOMAIN.Entities.Designations;

public class CreateDesignationRequest
{
    public string Name { get; set; }
    
    public string Description { get; set; }
    
    public Guid DepartmentId { get; set; }
}