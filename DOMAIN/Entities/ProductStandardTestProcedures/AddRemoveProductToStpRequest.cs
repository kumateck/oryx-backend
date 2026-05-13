using System.ComponentModel.DataAnnotations;

namespace DOMAIN.Entities.ProductStandardTestProcedures;

public class AddRemoveProductToStpRequest
{
    [Required]
    public string StpNumber { get; set; }
    public List<Guid> ProductIdsToAdd { get; set; } = [];
    public List<Guid> ProductIdsToRemove { get; set; } = [];
}
