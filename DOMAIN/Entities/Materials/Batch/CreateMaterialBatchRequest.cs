namespace DOMAIN.Entities.Materials.Batch;

public class CreateMaterialBatchRequest
{
    public Guid MaterialId { get; set; }         // ID of the material being added
    public decimal Quantity { get; set; }            // Quantity of the material batch
    public Guid InitialLocationId { get; set; }  // ID of the location where the batch is first stored (e.g., warehouse)
    public DateTime DateReceived { get; set; }   // Date when the material batch was received
    public DateTime ExpiryDate { get; set; }
}
