namespace DOMAIN.Entities.Materials.Batch;

public class MoveMaterialBatchRequest
{
    public Guid MaterialId { get; set; }
    public Guid FromLocationId { get; set; }
    public Guid ToLocationId { get; set; }
    public decimal Quantity { get; set; }
}