using DOMAIN.Entities.Materials;

namespace DOMAIN.Entities.Base;

public class CreateItemRequest
{
    public string Name { get; set; } 
    public string Description { get; set; }
    public string Type { get; set; } // e.g., Machine, Labor, etc.
    public bool IsAvailable { get; set; }
    public MaterialKind? MaterialKind { get; set; }
}