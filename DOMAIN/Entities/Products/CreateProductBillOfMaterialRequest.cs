namespace DOMAIN.Entities.Products;

public class CreateProductBillOfMaterialRequest
{
    public Guid? ComponentMaterialId { get; set; }
    public Guid? ComponentProductId { get; set; }
    public decimal Quantity { get; set; }  // Quantity of the component required
    public Guid UoMId { get; set; }  // Unit of Measure, e.g., grams, liters, pieces
    public bool IsSubstitutable { get; set; }  // Allows for substitution in production
}