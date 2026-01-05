using System.ComponentModel.DataAnnotations;

namespace DOMAIN.Entities.MaterialStandardTestProcedures;

public class CreateMaterialStandardTestProcedureRequest
{
    [Required] public string StpNumber { get; set; }
    [Required] public List<Guid> MaterialIds { get; set; }
    public string Description { get; set; }
}