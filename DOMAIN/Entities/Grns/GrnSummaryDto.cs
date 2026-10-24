namespace DOMAIN.Entities.Grns;

/// <summary>List-only contract. Retains the batch array used for counts by existing clients.</summary>
public sealed class GrnSummaryDto
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public string GrnNumber { get; set; }
    public string CarrierName { get; set; }
    public string VehicleNumber { get; set; }
    public string Remarks { get; set; }
    public string DeclarationNumber { get; set; }
    public Status Status { get; set; }
    public List<GrnBatchSummaryDto> MaterialBatches { get; set; } = [];
}

public sealed record GrnBatchSummaryDto(Guid Id, string SupplierName);
