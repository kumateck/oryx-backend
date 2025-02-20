using DOMAIN.Entities.Warehouses;

namespace DOMAIN.Entities.Materials.Batch;

public class SupplyMaterialBatchRequest
{
    public Guid MaterialBatchId { get; set; }
    public List<CreateShelfMaterialBatch> ShelfMaterialBatches { get; set; }
}

public class CreateShelfMaterialBatch
{
    public Guid WarehouseLocationShelfId { get; set; }
    public Guid MaterialBatchId { get; set; }
    public decimal Quantity { get; set; }
    public Guid? UomId { get; set; }
    public string Note { get; set; }
}

public class MoveShelfMaterialBatchRequest
{
    public Guid ShelfMaterialBatchId { get; set; }
    public List<MovedShelfBatchMaterial> MovedShelfBatchMaterials { get; set; }
}

public class MovedShelfBatchMaterial
{
    public Guid WarehouseLocationShelfId { get; set; }
    public decimal Quantity { get; set; }
    public Guid? UomId { get; set; }
    public string Note { get; set; }
}
