using System.ComponentModel.DataAnnotations;

namespace DOMAIN.Entities.Products.Production;

public sealed class UnstartReservationRepair
{
    public Guid Id { get; set; }
    public Guid ActorId { get; set; }
    public Guid DepartmentId { get; set; }
    public Guid MaterialId { get; set; }
    public DateTime AppliedAt { get; set; }
    [MaxLength(1000)] public string Reason { get; set; } = "";
    [MaxLength(64)] public string PreviewToken { get; set; } = "";
    public string BeforeJson { get; set; } = "";
    public string ReservationIdsJson { get; set; } = "";
}

public sealed class UnstartReservationPreviewDto
{
    public string PreviewToken { get; set; } = "";
    public List<UnstartReservationRowDto> Reservations { get; set; } = [];
}

public sealed class UnstartReservationRowDto
{
    public Guid ReservationId { get; set; }
    public Guid UnstartRepairId { get; set; }
    public string Product { get; set; } = "";
    public string ProductBatch { get; set; } = "";
    public string MaterialBatch { get; set; } = "";
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = "";
    public DateTime UnstartedAt { get; set; }
    public List<string> Blockers { get; set; } = [];
}

public sealed class ReleaseUnstartReservationsRequest
{
    public Guid RequestId { get; set; }
    [Required, StringLength(64, MinimumLength = 64)] public string PreviewToken { get; set; } = "";
    [Required, StringLength(1000, MinimumLength = 3)] public string Reason { get; set; } = "";
    [Required, MinLength(1), MaxLength(200)] public List<Guid> ReservationIds { get; set; } = [];
}

public sealed class ReleaseUnstartReservationsResultDto
{
    public Guid RepairId { get; set; }
    public int ReleasedCount { get; set; }
    public bool AlreadyApplied { get; set; }
}
