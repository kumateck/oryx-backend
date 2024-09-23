using SHARED;

namespace DOMAIN.Entities.Materials;

public static class MaterialErrors
{
    public static Error NotFound(Guid materialId) =>
        Error.NotFound("Materials.NotFound", $"Material with ID {materialId} not found.");
}
