using DOMAIN.Entities.Products;

namespace DOMAIN.Entities.BillOfMaterials.Request;

public class CreateBillOfMaterialRequest
{
    public Guid ProductId { get; set; } 
    public List<CreateBoMItemsRequest> Items { get; set; } = [];
}

public class CreateBoMItemsRequest
{
    public Guid? ComponentMaterialId { get; set; }
    public Guid? ComponentProductId { get; set; }
    public Guid? UoMId { get; set; }  
    public bool IsSubstitutable { get; set; }  // Allows for substitution in production
    public Guid? MaterialTypeId { get; set; }
    public string Grade { get; set; }
    public string CasNumber { get; set; }
    public string Function { get; set; }
    public decimal BaseQuantity { get; set; } 
    public Guid? BaseUoMId { get; set; } 
    public int Order { get; set; }
}