using System.ComponentModel.DataAnnotations;

namespace DOMAIN.Entities.StandardTestProcedures;

public class CreateStandardTestProcedureRequest
{
    [Required] public string StpNumber { get; set; }
    
    [Required] public Guid MaterialId { get; set; }
    
    public string Description { get; set; }
}