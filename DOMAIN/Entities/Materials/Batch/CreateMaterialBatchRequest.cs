using System.ComponentModel.DataAnnotations;

namespace DOMAIN.Entities.Materials.Batch;

public class CreateMaterialBatchRequest 
{
    public Guid MaterialId { get; set; }         // ID of the material being added
    public decimal TotalQuantity { get; set; }             // Quantity of the material batch
    public string BatchNumber { get; set; }      // Batch number of the material
    public DateTime? ManufacturingDate { get; set; }
    public int NumberOfContainers { get; set; }
    public Guid? ContainerPackageStyleId { get; set; }
    public decimal QuantityPerContainer { get; set; }
    public Guid? ChecklistId { get; set; }
    public Guid? UoMId { get; set; }               // ID of the unit of measure for the quantity
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

public class CreateFinishedGoodsTransferNoteRequest
{
    public string TransferNoteNumber { get; set; }
    public Guid BatchManufacturingRecordId { get; set; }
    public Guid? ProductionActivityStepId { get; set; }
    public decimal QuantityPerPack { get; set; }
    public Guid? PackageStyleId { get; set; }
    public decimal TotalQuantity { get; set; }
    public Guid? UoMId { get; set; }
    public string QarNumber { get; set; }
}