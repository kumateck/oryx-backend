namespace DOMAIN.Entities.Materials.Batch;

public class CreateMaterialBatchRequest
{
    public Guid MaterialId { get; set; }
    public int Quantity { get; set; }
    public Guid CurrentLocationId { get; set; }
    public DateTime DateReceived { get; set; }
}
