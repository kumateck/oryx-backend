using System.ComponentModel.DataAnnotations;

namespace DOMAIN.Entities.ProductionOrders;

public class CreateProductionOrderRequest
{
    public string ProductionOrderCode { get; set; }
    
    [Required] public Guid CustomerId { get; set; }
    
    [MinLength(1, ErrorMessage = "At least one product must be included in the production order.")]
    public List<ProductionOrderProducts> ProductionOrderProducts { get; set; }
    
    public decimal TotalValue => ProductionOrderProducts.Sum(prod => prod.TotalValue);
}