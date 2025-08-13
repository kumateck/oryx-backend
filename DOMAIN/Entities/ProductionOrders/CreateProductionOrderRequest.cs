using System.ComponentModel.DataAnnotations;

namespace DOMAIN.Entities.ProductionOrders;

public class CreateProductionOrderRequest
{
    public string Code { get; set; }
    
    [Required] public Guid CustomerId { get; set; }

    [MinLength(1, ErrorMessage = "At least one product must be included in the production order.")]
    public List<CreateProductionOrderProduct> Products { get; set; } = [];
}

public class CreateProductionOrderProduct
{
    public Guid ProductId {get; set;}
    public int TotalOrderQuantity { get; set; }
    public decimal VolumePerPiece { get; set; }
}