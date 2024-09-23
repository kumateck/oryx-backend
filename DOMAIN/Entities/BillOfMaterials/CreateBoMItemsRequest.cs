namespace DOMAIN.Entities.BillOfMaterials;

public class CreateBoMItemsRequest
{
    public Guid? ComponentMaterialId { get; set; }
    public Guid? ComponentProductId { get; set; }
    public Guid UoMId { get; set; }  // Unit of Measure, e.g., grams, liters, pieces
    public bool IsSubstitutable { get; set; }  // Allows for substitution in production
    public Guid? MaterialTypeId { get; set; }
    public string Grade { get; set; }
    public string CasNumber { get; set; }
    public string Function { get; set; }
    public int Order { get; set; }
}