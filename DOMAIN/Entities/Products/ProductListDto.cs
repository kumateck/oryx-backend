using SHARED;

namespace DOMAIN.Entities.Products;

public class ProductListDto
{
    public string Code { get; set; } 
    public string Name { get; set; }
    public string Description { get; set; }
    public CollectionItemDto Category { get; set; }
}