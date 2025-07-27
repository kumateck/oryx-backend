using DOMAIN.Entities.Attachments;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Departments;
using DOMAIN.Entities.Materials.Batch;

namespace DOMAIN.Entities.Inventory;

public class InventoryDto : WithAttachment
{
    public string MaterialName { get; set; }
    public string Code { get; set; }
    public InventoryType Classification { get; set; }
    public string Type { get; set; }
    public Guid UnitOfMeasureId { get; set; }
    public UnitOfMeasureDto UnitOfMeasure { get; set; }
    public bool HasBatchNumber { get; set; }
    public Guid MaterialBatchId { get; set; }
    public MaterialBatchDto MaterialBatch { get; set; }
    public string Remarks { get; set; }
    public ReorderRules ReorderRule { get; set; }
    public decimal InitialStockQuantity { get; set; }
    public Guid DepartmentId { get; set; }
    public DepartmentDto Department { get; set; }
    public bool IsActive { get; set; }
    public string Description { get; set; }
}