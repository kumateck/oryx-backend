namespace DOMAIN.Entities.Base;

public class ResourceDto
{
    public Guid? Id { get; set; }
    public string Name { get; set; }
    public string Type { get; set; } // e.g., Machine, Labor, etc.
    public bool IsAvailable { get; set; }
}