using System.ComponentModel.DataAnnotations;

namespace DOMAIN.Entities.MaterialStandardTestProcedures;

public class AddRemoveMaterialToStpRequest
{
    [Required]
    public string StpNumber { get; set; }
    public List<Guid> MaterialIdsToAdd { get; set; } = [];
    public List<Guid> MaterialIdsToRemove { get; set; } = [];
}
