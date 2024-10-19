using DOMAIN.Entities.Products;

namespace DOMAIN.Entities.BillOfMaterials;

public class CreateBillOfMaterialRequest
{
    public Guid ProductId { get; set; } 
    public List<CreateBoMItemsRequest> Items { get; set; } = [];
}