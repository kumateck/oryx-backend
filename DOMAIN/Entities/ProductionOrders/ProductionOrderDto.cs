using DOMAIN.Entities.Base;
using DOMAIN.Entities.Customers;

namespace DOMAIN.Entities.ProductionOrders;

public class ProductionOrderDto : BaseDto
{
    public string Code { get; set; }
    public CustomerDto Customer { get; set; }
    public List<ProductionOrderProductsDto> Products { get; set; } = [];
    public decimal TotalValue { get; set; }
}