using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Departments;
using DOMAIN.Entities.Materials.Batch;
using SHARED;

namespace DOMAIN.Entities.Materials;

public class Material : BaseEntity
{
    [StringLength(255)] public string Code { get; set; }
    [StringLength(255)] public string Name { get; set; }
    [StringLength(1000)] public string Description { get; set; }
    [StringLength(1000)] public string Pharmacopoeia { get; set; }
    [StringLength(10)] public string Alphabet { get; set; }
    public Guid? MaterialCategoryId { get; set; }
    public MaterialCategory MaterialCategory { get; set; }
    public List<MaterialBatch> Batches { get; set; } = [];
    public MaterialKind Kind { get; set; }
    public BatchKind Status { get; set; }
    public decimal TotalStock => Batches.Sum(b => b.RemainingQuantity);
}

public class MaterialDepartment : BaseEntity
{
    public Guid MaterialId { get; set; }
    public Material Material { get; set; }
    public Guid DepartmentId { get; set; }
    public Department Department { get; set; }
    public int ReOrderLevel { get; set; }
    public int MinimumStockLevel { get; set; }
    public int MaximumStockLevel { get; set; }
}

public class MaterialDepartmentDto
{
    public MaterialDto Material { get; set; }
    public CollectionItemDto Department { get; set; }
    public int ReOrderLevel { get; set; }
    public int MinimumStockLevel { get; set; }
    public int MaximumStockLevel { get; set; }
}

public class MaterialCategory : BaseEntity
{
    [StringLength(255)] public string Name { get; set; }
    [StringLength(1000)] public string Description { get; set; }
    public MaterialKind MaterialKind { get; set; }
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

public enum BatchKind
{
    Batch,
    NonBatch
}