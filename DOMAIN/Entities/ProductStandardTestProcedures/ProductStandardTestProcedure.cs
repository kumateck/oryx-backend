using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Products;

namespace DOMAIN.Entities.ProductStandardTestProcedures;

public class ProductStandardTestProcedure : BaseEntity
{
    public string StpNumber { get; set; }

    public Guid ProductId { get; set; }

    public Product Product { get; set; }

    [StringLength(10000000)]
    public string Description { get; set; }
}
