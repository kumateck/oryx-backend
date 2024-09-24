using System.ComponentModel.DataAnnotations;

namespace DOMAIN.Entities.Products;

public class CreateFinishedProductRequest
{
    [StringLength(255, ErrorMessage = "Max length 255 characters")] public string Name { get; set; }
    public Guid UoMId { get; set; } // Unit of Measure, e.g., bottles, tablets
    public decimal StandardCost { get; set; }
    public decimal SellingPrice { get; set; }
    [StringLength(255, ErrorMessage = "Dosage form max length is 255 characters")] public string DosageForm { get; set; }
    [StringLength(255, ErrorMessage = "Max length 255 characters")] public string Strength { get; set; }
}