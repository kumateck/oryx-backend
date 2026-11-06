using DOMAIN.Entities.Warehouses;
using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Materials.Batch;

namespace DOMAIN.Entities.Materials.WhitePlacebo;

public class SaveWhitePlaceboRecipe
{
    public Guid MaterialId { get; set; }
    public int Revision { get; set; }
    [Required, StringLength(200)] public string Reference { get; set; }
    [Required, StringLength(4000)] public string Instructions { get; set; }
    [Required, StringLength(1000)] public string StorageConditions { get; set; }
    [Range(1, 8760)] public int HoldHours { get; set; }
    public decimal ExpectedQuantity { get; set; }
    public List<WhitePlaceboIngredientRequest> Ingredients { get; set; } = [];
}

public sealed record WhitePlaceboIngredientRequest(Guid MaterialId, decimal Quantity);
public sealed record WhitePlaceboInputRequest(Guid ShelfMaterialBatchId, decimal Quantity);

public class CreateWhitePlaceboPreparation
{
    public Guid Id { get; set; }
    public int RecipeRevision { get; set; }
    [Required, StringLength(100)] public string BatchNumber { get; set; }
    public decimal ExpectedQuantity { get; set; }
    [StringLength(2000)] public string Notes { get; set; }
    public List<WhitePlaceboInputRequest> Inputs { get; set; } = [];
}

public class RecordWhitePlaceboOutput
{
    public decimal Quantity { get; set; }
    public DateTime PreparedAt { get; set; }
    [Required, StringLength(2000)] public string Notes { get; set; }
}

public sealed record WhitePlaceboMaterialDto(Guid Id, string Name, string Code,
    Guid UoMId, string UnitSymbol, bool IsWhitePlacebo);
public sealed record WhitePlaceboIngredientDto(Guid MaterialId, string Name, string Code,
    Guid UoMId, string UnitSymbol, decimal Quantity);
public sealed record WhitePlaceboRecipeDto(Guid MaterialId, string MaterialName, string UnitSymbol,
    int Revision, string Reference, string Instructions, string StorageConditions, int HoldHours,
    decimal ExpectedQuantity, List<WhitePlaceboIngredientDto> Ingredients);
public sealed record WhitePlaceboStockDto(Guid Id, Guid MaterialId, string MaterialName,
    string BatchNumber, string Shelf, string UnitSymbol, decimal Quantity, DateTime? ExpiryDate, string Rack = null);
public sealed record WhitePlaceboWorkspaceDto(WhitePlaceboRecipeDto Recipe,
    List<WhitePlaceboMaterialDto> Materials, List<WhitePlaceboStockDto> Stock);
public sealed record WhitePlaceboInputDto(Guid Id, Guid MaterialId, Guid MaterialBatchId,
    string MaterialName, string MaterialCode, string BatchNumber, string UnitSymbol,
    decimal RecipeQuantity, decimal Quantity)
{
    public MaterialWarehouseLocationShelfDto WarehouseLocationShelf { get; init; }
}
public sealed record WhitePlaceboPreparationPageDto(List<WhitePlaceboPreparationDto> Items, int TotalRows);
public sealed record WhitePlaceboPreparationDto(Guid Id, string BatchNumber, string MaterialName,
    string UnitSymbol, int RecipeRevision, string RecipeReference, string Instructions,
    string StorageConditions, int HoldHours, decimal ExpectedQuantity, decimal? ActualQuantity,
    string Notes, string OutputNotes, Guid RequisitionId, bool Approved, bool Rejected, DateTime CreatedAt,
    Guid? IssuedById, DateTime? IssuedAt, Guid? RecordedById, DateTime? RecordedAt,
    DateTime? PreparedAt, Guid? OutputBatchId, Guid? GrnId, BatchStatus? QualityStatus,
    DateTime? UseBy, List<WhitePlaceboInputDto> Inputs);
