using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Checklists;
using DOMAIN.Entities.Materials.Batch;
using SHARED;

namespace DOMAIN.Entities.Grns;

public class Grn : BaseEntity
{
    [StringLength(10000)]
    public string CarrierName { get; set; }

    [StringLength(10000)]
    public string VehicleNumber { get; set; }

    [StringLength(10000)]
    public string Remarks { get; set; }

    [StringLength(10000)]
    public string GrnNumber { get; set; }

    [StringLength(10000)]
    public string DeclarationNumber { get; set; }
    public Status Status { get; set; }
    public List<MaterialBatch> MaterialBatches { get; set; }
}

public enum Status
{
    Pending,
    Partial,
    Completed,
}

public class CreateGrnRequest
{
    [StringLength(10000)]
    public string CarrierName { get; set; }

    [StringLength(10000)]
    public string VehicleNumber { get; set; }

    [StringLength(10000)]
    public string Remarks { get; set; }

    [StringLength(10000)]
    public string GrnNumber { get; set; }

    [StringLength(10000)]
    public string DeclarationNumber { get; set; }
    public Guid? DepartmentId { get; set; }
    public List<Guid> MaterialBatchIds { get; set; }

    [StringLength(100, ErrorMessage = "Issue number cannot be longer than 100 characters.")]
    public string IssueNumber { get; set; }
}

public interface IGrnEnrichedDto
{
    string SupplierName { get; set; }
    string ManufacturerName { get; set; }
    string ArNumber { get; set; }
    string SampledBy { get; set; }
    DateTime? SampledOn { get; set; }
    decimal SampleQuantity { get; set; }
    string AnalysedBy { get; set; }
}

public class GrnListDto
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }

    [StringLength(10000)]
    public string CarrierName { get; set; }

    [StringLength(10000)]
    public string VehicleNumber { get; set; }

    [StringLength(10000)]
    public string Remarks { get; set; }

    [StringLength(10000)]
    public string GrnNumber { get; set; }
    public List<CollectionItemDto> MaterialBatches { get; set; } = [];
    public Status Status { get; set; }

    [StringLength(10000)]
    public string DeclarationNumber { get; set; }
}

public class GrnDto : IGrnEnrichedDto
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }

    [StringLength(10000)]
    public string CarrierName { get; set; }

    [StringLength(10000)]
    public string VehicleNumber { get; set; }

    [StringLength(10000)]
    public string Remarks { get; set; }

    [StringLength(10000)]
    public string GrnNumber { get; set; }
    public List<CheckListDto> CheckLists { get; set; } = [];
    public List<MaterialBatchListDto> MaterialBatches { get; set; } = [];

    [StringLength(10000)]
    public string DeclarationNumber { get; set; }
    public string SupplierName { get; set; }
    public string ManufacturerName { get; set; }
    public string ArNumber { get; set; }
    public string SampledBy { get; set; }
    public DateTime? SampledOn { get; set; }
    public decimal SampleQuantity { get; set; }
    public string AnalysedBy { get; set; }
}
