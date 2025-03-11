using DOMAIN.Entities.Base;
using SHARED;

namespace DOMAIN.Entities.Products.Production;

public class ProductionFilter : PagedQuery
{
    public List<Guid> UserIds { get; set; } = [];
    public ProductionStatus? Status { get; set; } 
}