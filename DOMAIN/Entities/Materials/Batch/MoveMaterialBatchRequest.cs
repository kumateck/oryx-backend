namespace DOMAIN.Entities.Materials.Batch;

public class MoveMaterialBatchRequest
{
    public Guid MaterialId { get; set; }
    public Guid FromWarehouseId { get; set; }
    public Guid ToWarehouseId { get; set; }
    public decimal Quantity { get; set; }
}

