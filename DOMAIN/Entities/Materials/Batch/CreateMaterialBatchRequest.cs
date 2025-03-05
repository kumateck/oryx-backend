using System.ComponentModel.DataAnnotations;

namespace DOMAIN.Entities.Materials.Batch;

public class CreateMaterialBatchRequest
{
    [StringLength(255)] public string Code { get; set; } 
    public Guid MaterialId { get; set; }         // ID of the material being added
    public decimal TotalQuantity { get; set; }             // Quantity of the material batch
    public string BatchNumber { get; set; }      // Batch number of the material
    public DateTime? ManufacturingDate { get; set; }
    public Guid? ChecklistId { get; set; }
    public Guid? UoMId { get; set; }               // ID of the unit of measure for the quantity
    public Guid InitialLocationId { get; set; }  // ID of the location where the batch is first stored (e.g., warehouse)
    public DateTime DateReceived { get; set; }   // Date when the material batch was received
    public DateTime ExpiryDate { get; set; }
    public List<CreateSrRequest> SampleWeights { get; set; }
}

public class CreateSrRequest
{
    [StringLength(10000)] public string SrNumber { get; set; }
    public decimal GrossWeight { get; set; }
    public Guid? UoMId { get; set; }
}
