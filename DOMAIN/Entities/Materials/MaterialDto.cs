using DOMAIN.Entities.Materials.Batch;
using SHARED;

namespace DOMAIN.Entities.Materials;

public class MaterialDto
{
    public Guid Id { get; set; }
    public string Code { get; set; }
    public string Pharmacopoeia { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public MaterialKind Kind { get; set; }
    public CollectionItemDto MaterialCategory { get; set; }
    public List<MaterialBatchDto> Batches { get; set; } = [];
    public int AmountInStock => Batches.Where(b => b.Status == BatchStatus.Available).Sum(b => b.RemainingQuantity);
}