using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Materials.Batch;
using SHARED;

namespace DOMAIN.Entities.Grns;

public class Grn:BaseEntity
{
    [StringLength(10000)]public string CarrierName { get; set; }
    [StringLength(10000)]public string VehicleNumber { get; set; }
    [StringLength(10000)]public string Remarks { get; set; }
    [StringLength(10000)]public string GrnNumber { get; set; }
    
    public Status Status { get; set; }
    public List<MaterialBatch> MaterialBatches { get; set; }
}

public enum Status
{
    Pending,
    Partial,
    Completed
}
public class CreateGrnRequest
{
    [StringLength(10000)]public string CarrierName { get; set; }
    [StringLength(10000)]public string VehicleNumber { get; set; }
    [StringLength(10000)]public string Remarks { get; set; }
    [StringLength(10000)]public string GrnNumber { get; set; }
    public List<Guid> MaterialBatchIds { get; set; }
}


public class GrnListDto
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }
    [StringLength(10000)]public string CarrierName { get; set; }
    [StringLength(10000)]public string VehicleNumber { get; set; }
    [StringLength(10000)]public string Remarks { get; set; }
    [StringLength(10000)]public string GrnNumber { get; set; }
    public List<CollectionItemDto> MaterialBatches { get; set; } = [];
    public Status Status { get; set; }
}
public class GrnDto
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }
    [StringLength(10000)]public string CarrierName { get; set; }
    [StringLength(10000)]public string VehicleNumber { get; set; }
    [StringLength(10000)]public string Remarks { get; set; }
    [StringLength(10000)]public string GrnNumber { get; set; }
    public List<MaterialBatchListDto> MaterialBatches { get; set; } = [];
}