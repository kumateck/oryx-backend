using System.ComponentModel.DataAnnotations;

namespace DOMAIN.Entities.ProductStandardTestProcedures;

public class CreateProductStandardTestProcedureRequest
{
    [Required]
    public string StpNumber { get; set; }

    [Required]
    public List<Guid> ProductIds { get; set; }
    public string Description { get; set; }
}

public class UpdateProductStandardTestProcedureRequest
{
    [Required]
    public string StpNumber { get; set; }
    public string Description { get; set; }
}
