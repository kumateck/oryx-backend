using System.ComponentModel.DataAnnotations;

namespace DOMAIN.Entities.Inventory;

public class CreateInventoryRequest
{
    [Required, MinLength(3, ErrorMessage = "Name cannot be less than 3 characters")]
    public string MaterialName { get; set; }
    [Required] public string Code { get; set; }
    [Required(ErrorMessage = "Classification is required")] public InventoryClassification Classification { get; set; }
    public Guid InventoryTypeId { get; set; }
    [Required(ErrorMessage = "Unit of measure is required")] public Guid UnitOfMeasureId { get; set; }
    public bool HasBatchNumber { get; set; }
    public string Remarks { get; set; }
    public ReorderRules ReorderRule { get; set; }
    public decimal InitialStockQuantity { get; set; }
    [Required(ErrorMessage = "Department is required")] public Guid DepartmentId { get; set; }
    [Required(ErrorMessage = "Status is required")] public bool IsActive { get; set; }
    public string Description { get; set; }
}