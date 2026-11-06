using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.Requisitions;

namespace DOMAIN.Entities.Materials.WhitePlacebo;

public class WhitePlaceboRecipe : BaseEntity
{
    public Guid DepartmentId { get; set; }
    public Guid MaterialId { get; set; }
    public Material Material { get; set; }
    public Guid UoMId { get; set; }
    public UnitOfMeasure UoM { get; set; }
    public int Revision { get; set; }
    [StringLength(200)] public string Reference { get; set; }
    [StringLength(4000)] public string Instructions { get; set; }
    [StringLength(1000)] public string StorageConditions { get; set; }
    public int HoldHours { get; set; }
    public decimal ExpectedQuantity { get; set; }
    public List<WhitePlaceboRecipeIngredient> Ingredients { get; set; } = [];
}

public class WhitePlaceboRecipeIngredient : BaseEntity
{
    public Guid WhitePlaceboRecipeId { get; set; }
    public Guid MaterialId { get; set; }
    public Material Material { get; set; }
    public Guid UoMId { get; set; }
    public UnitOfMeasure UoM { get; set; }
    public decimal Quantity { get; set; }
}

public class WhitePlaceboPreparation : BaseEntity
{
    public Guid DepartmentId { get; set; }
    public Guid MaterialId { get; set; }
    public Material Material { get; set; }
    public Guid UoMId { get; set; }
    public UnitOfMeasure UoM { get; set; }
    public int RecipeRevision { get; set; }
    [StringLength(200)] public string RecipeReference { get; set; }
    [StringLength(4000)] public string Instructions { get; set; }
    [StringLength(1000)] public string StorageConditions { get; set; }
    [StringLength(100)] public string BatchNumber { get; set; }
    public int HoldHours { get; set; }
    public decimal ExpectedQuantity { get; set; }
    public decimal? ActualQuantity { get; set; }
    [StringLength(2000)] public string Notes { get; set; }
    [StringLength(2000)] public string OutputNotes { get; set; }
    public Guid RequisitionId { get; set; }
    public Requisition Requisition { get; set; }
    public Guid? IssuedById { get; set; }
    public DateTime? IssuedAt { get; set; }
    public DateTime? PreparedAt { get; set; }
    public Guid? RecordedById { get; set; }
    public DateTime? RecordedAt { get; set; }
    public Guid? OutputBatchId { get; set; }
    public MaterialBatch OutputBatch { get; set; }
    public List<WhitePlaceboPreparationInput> Inputs { get; set; } = [];
}

public class WhitePlaceboPreparationInput : BaseEntity
{
    public Guid WhitePlaceboPreparationId { get; set; }
    public Guid MaterialId { get; set; }
    public Guid MaterialBatchId { get; set; }
    public MaterialBatch MaterialBatch { get; set; }
    public Guid SourceShelfId { get; set; }
    public Guid SourceWarehouseId { get; set; }
    public Guid UoMId { get; set; }
    [StringLength(255)] public string MaterialName { get; set; }
    [StringLength(255)] public string MaterialCode { get; set; }
    [StringLength(10000)] public string BatchNumber { get; set; }
    [StringLength(20)] public string UnitSymbol { get; set; }
    public decimal RecipeQuantity { get; set; }
    public decimal Quantity { get; set; }
}
