using SHARED;

namespace DOMAIN.Entities.Products;

public class FinishedProductDto
{ 
    public string Name { get; set; }
    public CollectionItemDto Product { get; set; }
    public CollectionItemDto UoM { get; set; }
    public decimal StandardCost { get; set; }
    public decimal SellingPrice { get; set; }
    public string DosageForm { get; set; }
    public string Strength { get; set; }
}