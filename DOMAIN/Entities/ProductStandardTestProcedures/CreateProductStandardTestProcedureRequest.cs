using System.ComponentModel.DataAnnotations;

namespace DOMAIN.Entities.ProductStandardTestProcedures;

public class CreateProductStandardTestProcedureRequest
{
    [Required] public string StpNumber { get; set; }
    
    [Required] public Guid ProductId { get; set; }
}