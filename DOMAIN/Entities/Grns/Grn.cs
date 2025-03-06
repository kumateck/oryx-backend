using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Materials.Batch;

namespace DOMAIN.Entities.Grns;

public class Grn:BaseEntity
{
    [StringLength(10000)]public string CarrierName { get; set; }
    [StringLength(10000)]public string VehicleNumber { get; set; }
    [StringLength(10000)]public string Remarks { get; set; }
    [StringLength(10000)]public string GrnNumber { get; set; }
    public List<MaterialBatch> MaterialBatches { get; set; }
}

public class CreateGrnRequest
{
    [StringLength(10000)]public string CarrierName { get; set; }
    [StringLength(10000)]public string VehicleNumber { get; set; }
    [StringLength(10000)]public string Remarks { get; set; }
    [StringLength(10000)]public string GrnNumber { get; set; }
    public List<Guid> MaterialBatchIds { get; set; }
}

public class GrnDto
{
    public Guid Id { get; set; }
    [StringLength(10000)]public string CarrierName { get; set; }
    [StringLength(10000)]public string VehicleNumber { get; set; }
    [StringLength(10000)]public string Remarks { get; set; }
    [StringLength(10000)]public string GrnNumber { get; set; }
    public List<MaterialBatchDto> MaterialBatches { get; set; }
}