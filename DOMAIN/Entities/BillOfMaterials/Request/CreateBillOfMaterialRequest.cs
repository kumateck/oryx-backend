namespace DOMAIN.Entities.BillOfMaterials.Request;

public class CreateBillOfMaterialRequest
{
    public Guid ProductId { get; set; }
    public List<CreateBoMItemsRequest> Items { get; set; } = [];
}

public class CreateBoMItemsRequest
{
    public Guid MaterialId { get; set; }
    public Guid? UoMId { get; set; }
    public bool IsSubstitutable { get; set; }  // Allows for substitution in production
    public string Grade { get; set; }
    public string CasNumber { get; set; }
    public decimal BaseQuantity { get; set; }
    public Guid? BaseUoMId { get; set; }
    public int Order { get; set; }
    public decimal PrescribedQuantity { get; set; }
    public List<Guid> SubstituteMaterialIds { get; set; } = [];
}