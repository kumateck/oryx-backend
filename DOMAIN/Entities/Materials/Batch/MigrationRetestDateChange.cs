using System.Text.Json.Serialization;
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
    [StringLength(500)] public string PreviousBatchNumber { get; set; }
    /// <summary>Null when the batch number was not corrected in this change.</summary>
    [StringLength(500)] public string NewBatchNumber { get; set; }
    public DateTime? PreviousManufacturingDate { get; set; }
    public DateTime? NewManufacturingDate { get; set; }
    public DateTime? NewExpiryDate { get; set; }
    [StringLength(500)] public string PreviousGrnNumber { get; set; }
    [StringLength(500)] public string NewGrnNumber { get; set; }
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
    /// <summary>Optional correction of the migrated batch number. Omit to preserve it.</summary>
    [StringLength(500)] public string BatchNumber { get; set; }
    /// <summary>The batch number the editor loaded; a different stored value refuses the change.</summary>
    public string ExpectedBatchNumber { get; set; }
    [Required, StringLength(1000, MinimumLength = 3)] public string Reason { get; set; }
}

public class UpdateMigrationBatchDetails
{
    [JsonRequired]
    public DateTime? ManufacturingDate { get; set; }
    [JsonRequired]
    public DateTime? ExpiryDate { get; set; }
    [JsonRequired]
    [StringLength(500)] public string GrnNumber { get; set; }
    [JsonRequired]
    public DateTime? ExpectedManufacturingDate { get; set; }
    [JsonRequired]
    public DateTime? ExpectedExpiryDate { get; set; }
    [JsonRequired]
    public string ExpectedGrnNumber { get; set; }
    [Required, StringLength(1000, MinimumLength = 3)] public string Reason { get; set; }
}
