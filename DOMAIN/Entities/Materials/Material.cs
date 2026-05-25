using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Departments;
using DOMAIN.Entities.Materials.Batch;
using SHARED;

namespace DOMAIN.Entities.Materials;

public class Material : BaseEntity
{
    [StringLength(255)]
    public string Code { get; set; }

    [StringLength(255)]
    public string Name { get; set; }

    [StringLength(1000)]
    public string Description { get; set; }

    [StringLength(1000)]
    public string Pharmacopoeia { get; set; }

    [StringLength(10)]
    public string Alphabet { get; set; }
    public Guid? MaterialCategoryId { get; set; }
    public MaterialCategory MaterialCategory { get; set; }
    public List<MaterialBatch> Batches { get; set; } = [];
    public MaterialKind Kind { get; set; }
    public BatchKind Status { get; set; }
    public bool IsUnlimited { get; set; }
    public decimal TotalStock =>
        IsUnlimited ? 999_999_999_999_999_999_999_999.99m : Batches.Sum(b => b.RemainingQuantity);
    public List<MaterialDepartment> Departments { get; set; } = [];
}

public class MaterialDepartment : BaseEntity
{
    public Guid MaterialId { get; set; }
    public Material Material { get; set; }
    public Guid UoMId { get; set; }
    public UnitOfMeasure UoM { get; set; }
    public Guid DepartmentId { get; set; }
    public Department Department { get; set; }
    public decimal Density { get; set; }
    public Guid? DensityUoMId { get; set; }
    public UnitOfMeasure DensityUoM { get; set; }
    public decimal ReOrderLevel { get; set; }
    public decimal MinimumStockLevel { get; set; }
    public decimal MaximumStockLevel { get; set; }
}

public class MaterialDepartmentDto
{
    public MaterialDto Material { get; set; }
    public CollectionItemDto Department { get; set; }
    public UnitOfMeasureDto UoM { get; set; }
    public decimal Density { get; set; }
    public UnitOfMeasureDto DensityUoM { get; set; }
    public decimal ReOrderLevel { get; set; }
    public decimal MinimumStockLevel { get; set; }
    public decimal MaximumStockLevel { get; set; }
}

public class MaterialCategory : BaseEntity
{
    [StringLength(255)]
    public string Name { get; set; }

    [StringLength(1000)]
    public string Description { get; set; }
    public MaterialKind MaterialKind { get; set; }
}

public class MaterialType : BaseEntity
{
    [StringLength(255)]
    public string Name { get; set; }

    [StringLength(1000)]
    public string Description { get; set; }
}

public enum MaterialKind
{
    Raw,
    Package,
}

public enum BatchKind
{
    Batch,
    NonBatch,
}
