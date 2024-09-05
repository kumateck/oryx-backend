namespace DOMAIN.Entities.Products;

public class CreateProductRequest
{ 
    public string ProductId { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public Guid CategoryId { get; set; } 
    public Guid UoMId { get; set; }
    public decimal StandardCost { get; set; }
    public decimal SellingPrice { get; set; }
    public bool IsActive { get; set; }
}