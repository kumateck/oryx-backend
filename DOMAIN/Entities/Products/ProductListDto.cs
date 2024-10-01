using SHARED;

namespace DOMAIN.Entities.Products;

public class ProductListDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } 
    public string Name { get; set; }
    public string Description { get; set; }
    public CollectionItemDto Category { get; set; }
    public CollectionItemDto CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
}