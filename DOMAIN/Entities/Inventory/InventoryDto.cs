using DOMAIN.Entities.Attachments;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Departments;
using DOMAIN.Entities.Materials.Batch;

namespace DOMAIN.Entities.Inventory;

public class InventoryDto : WithAttachment
{
    public string MaterialName { get; set; }
    public string Code { get; set; }
    public InventoryClassification Classification { get; set; }
    public Guid InventoryTypeId { get; set; }
    public InventoryType InventoryType { get; set; }
    public Guid UnitOfMeasureId { get; set; }
    public UnitOfMeasureDto UnitOfMeasure { get; set; }
    public bool HasBatch { get; set; }
    public string Remarks { get; set; }
    public ReorderRules ReorderRule { get; set; }
    public decimal InitialStockQuantity { get; set; }
    public Guid DepartmentId { get; set; }
    public DepartmentDto Department { get; set; }
    public bool IsActive { get; set; }
    public string Description { get; set; }
}