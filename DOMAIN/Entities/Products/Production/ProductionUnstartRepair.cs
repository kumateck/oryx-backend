using System.ComponentModel.DataAnnotations;

namespace DOMAIN.Entities.Products.Production;

public sealed class ProductionUnstartRepair
{
    public Guid Id { get; set; }
    public Guid ActivityId { get; set; }
    public Guid BatchId { get; set; }
    public Guid ActorId { get; set; }
    public DateTime AppliedAt { get; set; }
    [MaxLength(1000)] public string Reason { get; set; } = "";
    [MaxLength(64)] public string PreviewToken { get; set; } = "";
    public string BeforeJson { get; set; } = "";
    public string AfterJson { get; set; } = "";
}

public sealed class ProductionUnstartRequest
{
    public Guid RequestId { get; set; }
    [Required, StringLength(64, MinimumLength = 64)] public string PreviewToken { get; set; } = "";
    [Required, StringLength(1000, MinimumLength = 3)] public string Reason { get; set; } = "";
}

public sealed class ProductionUnstartPreviewDto
{
    public Guid ActivityId { get; set; }
    public Guid BatchId { get; set; }
    public Guid ScheduleId { get; set; }
    public string ProductName { get; set; } = "";
    public string BatchNumber { get; set; } = "";
    public string PreviewToken { get; set; } = "";
    public bool CanApply { get; set; }
    public bool AlreadyUnstarted { get; set; }
    public List<string> Blockers { get; set; } = [];
    public int StepsToArchive { get; set; }
    public int DraftDocumentsToArchive { get; set; }
    public int ScheduleStatusBefore { get; set; }
    public int ScheduleStatusAfter { get; set; }
    public List<ProductionUnstartReservationDto> Reservations { get; set; } = [];
}

public sealed class ProductionUnstartReservationDto
{
    public Guid Id { get; set; }
    public string MaterialCode { get; set; } = "";
    public string MaterialName { get; set; } = "";
    public string LotNumber { get; set; } = "";
    public string Unit { get; set; } = "";
    public decimal Quantity { get; set; }
}

public sealed class ProductionUnstartResultDto
{
    public Guid RepairId { get; set; }
    public Guid BatchId { get; set; }
    public Guid ScheduleId { get; set; }
    public bool AlreadyApplied { get; set; }
}
