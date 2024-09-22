namespace DOMAIN.Entities.Products;

public class CreateProductRequest
{ 
    public string Code { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public Guid CategoryId { get; set; } 
    public Guid UoMId { get; set; }
    public decimal StandardCost { get; set; }
    public decimal SellingPrice { get; set; }
    public string DosageForm { get; set; }
    public string Strength { get; set; }
    public bool IsActive { get; set; }
}