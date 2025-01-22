using SHARED;

namespace DOMAIN.Entities.Products;

public class ProductListDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } 
    public string Name { get; set; }
    public string Description { get; set; }
    public string GenericName { get; set; }
    public string StorageCondition { get; set; }
    public string PackageStyle { get; set; }
    public string FilledWeight { get; set; }
    public string ShelfLife { get; set; }
    public string ActionUse { get; set; }
    public CollectionItemDto Category { get; set; }
    public DateTime CreatedAt { get; set; }
}