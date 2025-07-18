using DOMAIN.Entities.Base;
using DOMAIN.Entities.Customers;

namespace DOMAIN.Entities.ProductionOrders;

public class ProductionOrderDto : BaseDto
{
    public string ProductionOrderCode { get; set; }
    public Guid CustomerId { get; set; }
    public CustomerDto Customer { get; set; }
    public List<ProductionOrderProducts> ProductionOrderProducts { get; set; }
    public decimal TotalValue { get; set; }
}