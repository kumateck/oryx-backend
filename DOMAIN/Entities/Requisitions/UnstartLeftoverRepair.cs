using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Materials;

namespace DOMAIN.Entities.Requisitions;

public sealed class UnstartLeftoverRepair
{
    public Guid Id { get; set; }
    public Guid ActorId { get; set; }
    public Guid DepartmentId { get; set; }
    public MaterialKind Kind { get; set; }
    public DateTime AppliedAt { get; set; }
    [MaxLength(1000)] public string Reason { get; set; } = "";
    [MaxLength(64)] public string PreviewToken { get; set; } = "";
    public string BeforeJson { get; set; } = "";
    public string RequisitionIdsJson { get; set; } = "";
}

public sealed class UnstartLeftoverPreviewDto
{
    public string PreviewToken { get; set; } = "";
    public List<UnstartLeftoverRowDto> Requisitions { get; set; } = [];
}

public sealed class UnstartLeftoverRowDto
{
    public Guid RequisitionId { get; set; }
    public string Code { get; set; } = "";
    public string BatchNumber { get; set; } = "";
    public Guid UnstartRepairId { get; set; }
    public DateTime UnstartedAt { get; set; }
    public int MaterialCount { get; set; }
    public List<string> Blockers { get; set; } = [];
}

public sealed class RepairUnstartLeftoversRequest
{
    public Guid RequestId { get; set; }
    [Required, StringLength(64, MinimumLength = 64)] public string PreviewToken { get; set; } = "";
    [Required, StringLength(1000, MinimumLength = 3)] public string Reason { get; set; } = "";
    [Required, MinLength(1), MaxLength(200)] public List<Guid> RequisitionIds { get; set; } = [];
}

public sealed class RepairUnstartLeftoversResultDto
{
    public Guid RepairId { get; set; }
    public int CancelledCount { get; set; }
    public bool AlreadyApplied { get; set; }
}
