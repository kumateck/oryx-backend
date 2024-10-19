namespace DOMAIN.Entities.Materials.Batch;

public class CreateMaterialBatchRequest
{
    public Guid MaterialId { get; set; }
    public int Quantity { get; set; }
    public Guid WarehouseId { get; set; }
    public DateTime DateReceived { get; set; }
}
