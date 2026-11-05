using System.ComponentModel.DataAnnotations;

namespace DOMAIN.Entities.Products.Production;

public sealed class ReservationRepair
{
    public Guid Id { get; set; }
    public Guid BatchId { get; set; }
    public Guid MigrationId { get; set; }
    public Guid ActorId { get; set; }
    public DateTime AppliedAt { get; set; }
    [Required, MaxLength(1000)] public string Reason { get; set; } = "";
    [Required, MaxLength(64)] public string PreviewToken { get; set; } = "";
    [Required] public string BeforeJson { get; set; } = "";
    [Required] public string AfterJson { get; set; } = "";
    [Required] public string RowsJson { get; set; } = "";
}

public sealed class ReservationRepairRequest
{
    public Guid RequestId { get; set; }
    [Required, RegularExpression("^[a-f0-9]{64}$")] public string PreviewToken { get; set; } = "";
    [Required, StringLength(1000, MinimumLength = 10)] public string Reason { get; set; } = "";
}

public sealed class ReservationRepairPreviewDto
{
    public Guid BatchId { get; set; }
    public string BatchNumber { get; set; } = "";
    public Guid MigrationId { get; set; }
    public string MigrationFile { get; set; } = "";
    public string PreviewToken { get; set; } = "";
    public bool HasChanges { get; set; }
    public bool CanApply { get; set; }
    public List<string> Blockers { get; set; } = [];
    public List<ReservationRepairRowDto> Rows { get; set; } = [];
    public List<ReservationRepairHistoryDto> History { get; set; } = [];
}

public sealed class ReservationRepairRowDto
{
    public Guid ReservationId { get; set; }
    public string RequisitionCode { get; set; } = "";
    public string MaterialCode { get; set; } = "";
    public string MaterialName { get; set; } = "";
    public string Unit { get; set; } = "";
    public decimal Quantity { get; set; }
    public Guid OldBatchId { get; set; }
    public string OldBatchNumber { get; set; } = "";
    public string OldShelf { get; set; } = "";
    public List<ReservationRepairCandidateDto> Candidates { get; set; } = [];
}

public sealed class ReservationRepairCandidateDto
{
    public Guid BatchId { get; set; }
    public string BatchNumber { get; set; } = "";
    public Guid ShelfId { get; set; }
    public string Shelf { get; set; } = "";
    public decimal AvailableQuantity { get; set; }
}

public sealed class ReservationRepairHistoryDto
{
    public Guid Id { get; set; }
    public Guid ActorId { get; set; }
    public string ActorName { get; set; } = "";
    public DateTime AppliedAt { get; set; }
    public string Reason { get; set; } = "";
    public List<ReservationRepairRowDto> Rows { get; set; } = [];
}

public sealed class ReservationRepairResultDto
{
    public Guid RepairId { get; set; }
    public bool AlreadyApplied { get; set; }
}
