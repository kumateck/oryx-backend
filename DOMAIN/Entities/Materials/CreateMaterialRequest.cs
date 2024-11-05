namespace DOMAIN.Entities.Materials;

public class CreateMaterialRequest
{
    public string Name { get; set; }
    public string Description { get; set; }
    public Guid? MaterialCategoryId { get; set; }
    public MaterialKind Kind { get; set; }
}