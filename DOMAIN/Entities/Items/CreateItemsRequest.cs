using System.ComponentModel.DataAnnotations;

namespace DOMAIN.Entities.Items;

// related to vendors
public class CreateItemsRequest
{
    [Required, MinLength(3, ErrorMessage = "Name cannot be less than 3 characters")]
    public string Name { get; set; }
    
    [Required] public string Code { get; set; }
    [Required(ErrorMessage = "Classification is required")] 
    
    public InventoryClassification Classification { get; set; }
    
    [Required(ErrorMessage = "Unit of measure is required")] 
    public Guid UnitOfMeasureId { get; set; }
    
    public bool HasBatchNumber { get; set; }
    public int MinimumLevel { get; set; }
    public int MaximumLevel { get; set; }
    public int ReorderLevel { get; set; }
    [Required] public Store Store { get; set; }
    
    [Required(ErrorMessage = "Status is required")] 
    public bool IsActive { get; set; }
    
    public string Description { get; set; }
}