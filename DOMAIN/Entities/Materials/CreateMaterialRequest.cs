namespace DOMAIN.Entities.Materials;

public class CreateMaterialRequest
{
    public string Code { get; set; }
    public string Name { get; set; }
    public string Pharmacopoeia { get; set; }
    public string Description { get; set; }
    public string Alphabet { get; set; }
    public Guid? MaterialCategoryId { get; set; }
    public MaterialKind Kind { get; set; }
    public int ReOrderLevel { get; set; }
}

public class CreateMaterialDepartment
{
    public Guid MaterialId { get; set; }
    public int ReOrderLevel { get; set; }
    public int MinimumStockLevel { get; set; }
    public int MaximumStockLevel { get; set; }
}
public class UpdateReOrderLevelRequest
{
    public int ReOrderLevel { get; set; }
}