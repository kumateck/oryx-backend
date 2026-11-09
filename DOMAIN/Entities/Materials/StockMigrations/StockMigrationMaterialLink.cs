using System.ComponentModel.DataAnnotations;

namespace DOMAIN.Entities.Materials.StockMigrations;

/// <summary>A missing department link and its proposed defaults, in the smallest storage unit.</summary>
public class StockMigrationMaterialLinkDto
{
    public Guid MaterialId { get; set; }
    public string Code { get; set; }
    public string Name { get; set; }
    public MaterialKind Kind { get; set; }
    public Guid? UoMId { get; set; }
    public string UnitName { get; set; }
    public string UnitSymbol { get; set; }
    public decimal MinimumStockLevel { get; set; }
    public decimal ReOrderLevel { get; set; }
    public decimal MaximumStockLevel { get; set; }
    public string Issue { get; set; }
}

public class CreateStockMigrationMaterialLinksRequest
{
    [Required, MinLength(1), MaxLength(1000)]
    public List<StockMigrationMaterialLinkDto> Materials { get; set; } = [];
}

public class StockMigrationMaterialLinksResult
{
    public int Created { get; set; }
    public int AlreadyLinked { get; set; }
}
