using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;

namespace DOMAIN.Entities.Products;

public class FinishedProduct : BaseEntity
{
    [StringLength(255)] public string Name { get; set; }
    public Guid ProductId { get; set; }
    public Product Product { get; set; }
    public Guid UoMId { get; set; } // Unit of Measure, e.g., bottles, tablets
    public UnitOfMeasure UoM { get; set; }
    public decimal StandardCost { get; set; }
    public decimal SellingPrice { get; set; }
    [StringLength(255)] public string DosageForm { get; set; }
    [StringLength(255)] public string Strength { get; set; }
}