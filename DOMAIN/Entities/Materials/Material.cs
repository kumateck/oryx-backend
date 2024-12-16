using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Materials.Batch;

namespace DOMAIN.Entities.Materials;

public class Material : BaseEntity
{
    [StringLength(255)] public string Code { get; set; }
    [StringLength(255)] public string Name { get; set; }
    [StringLength(1000)] public string Description { get; set; }
    [StringLength(1000)] public string Pharmacopoeia { get; set; }
    public Guid? MaterialCategoryId { get; set; }
    public MaterialCategory MaterialCategory { get; set; }
    public int MinimumStockLevel { get; set; }
    public int MaximumStockLevel { get; set; }
    public List<MaterialBatch> Batches { get; set; } = [];
    public MaterialKind Kind { get; set; }
}

public class MaterialCategory : BaseEntity
{
    [StringLength(255)] public string Name { get; set; }
    [StringLength(1000)] public string Description { get; set; }
    public int MinimumStockLevel { get; set; }
    public int MaximumStockLevel { get; set; }
}

public class MaterialType : BaseEntity
{
    [StringLength(255)] public string Name { get; set; }
    [StringLength(1000)] public string Description { get; set; }
}

public enum MaterialKind
{
    Raw,
    Package
}