using System.ComponentModel.DataAnnotations;

namespace DOMAIN.Entities.Materials.Batch;

/// <summary>Append-only evidence for a migration correction, independent of QC retest cycles.</summary>
public class MigrationRetestDateChange
{
    public Guid Id { get; set; }
    public Guid MaterialBatchId { get; set; }
    public MaterialBatch MaterialBatch { get; set; }
    public DateTime? PreviousDate { get; set; }
    public DateTime? NewDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string PreviousArNumber { get; set; }
    /// <summary>Null preserves the AR; an empty string records an explicit clear.</summary>
    [StringLength(500)] public string NewArNumber { get; set; }
    public Guid ActorId { get; set; }
    public DateTime OccurredAt { get; set; }
    [Required, StringLength(1000)] public string Reason { get; set; }
}

public class UpdateMigrationRetestDate
{
    public DateTime? RetestDate { get; set; }
    public DateTime? ExpectedRetestDate { get; set; }
    public DateTime? ExpectedExpiryDate { get; set; }
    /// <summary>Optional correction. Omit to preserve the AR; send empty to clear it.</summary>
    [StringLength(500)] public string ArNumber { get; set; }
    public string ExpectedArNumber { get; set; }
    [Required, StringLength(1000, MinimumLength = 3)] public string Reason { get; set; }
}
