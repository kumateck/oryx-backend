using DOMAIN.Entities.Base;
using DOMAIN.Entities.Materials.Batch;

namespace DOMAIN.Entities.Materials;

public class MaterialDto
{
    public Guid Id { get; set; }
    public string Code { get; set; }
    public string Pharmacopoeia { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public MaterialKind Kind { get; set; }
    public MaterialCategoryDto MaterialCategory { get; set; }
    public List<MaterialBatchDto> Batches { get; set; } = [];
    public decimal TotalStock => Batches.Where(b => b.Status == BatchStatus.Available).Sum(b => b.RemainingQuantity);
}

public class MaterialCategoryDto : BaseDto
{
    public string Name { get; set; }
    public string Description { get; set; }
    public MaterialKind MaterialKind { get; set; }
}