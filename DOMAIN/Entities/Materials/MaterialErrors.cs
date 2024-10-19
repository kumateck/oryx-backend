using SHARED;

namespace DOMAIN.Entities.Materials;

public static class MaterialErrors
{
    public static Error NotFound(Guid materialId) =>
        Error.NotFound("Materials.NotFound", $"Material with ID {materialId} not found.");
    
    public static Error InsufficientStock =>
        Error.Validation("Materials.InsufficientStock", $"Material requested has insufficient stock level");
    public static Error BelowMinimumStock(Guid materialId) => 
        Error.Failure("Materials.MinStock", $"Processing this requisition would result in stock falling below the minimum stock level for material {materialId}");
}
