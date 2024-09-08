namespace DOMAIN.Entities.BillOfMaterials;

public class CreateBoMItemsRequest
{
    public Guid? ComponentMaterialId { get; set; }
    public Guid? ComponentProductId { get; set; }
    public int Quantity { get; set; }  // Quantity of the component required
    public Guid UoMId { get; set; }  // Unit of Measure, e.g., grams, liters, pieces
    public bool IsSubstitutable { get; set; }  // Allows for substitution in production
    public BomItemType Type { get; set; }
    public string Grade { get; set; }
    public string CasNumber { get; set; }
    public string Function { get; set; }
    public int Order { get; set; }
}