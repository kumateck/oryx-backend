using System.ComponentModel.DataAnnotations;

namespace DOMAIN.Entities.Products.Production;

public enum ScheduleRepairKind { Bom, Packaging, Procedure }

/// <summary>One atomic product-within-schedule correction and its replay receipt.</summary>
public sealed class ProductionScheduleRepair
{
    public Guid Id { get; set; }
    public Guid ScheduleId { get; set; }
    public Guid ProductId { get; set; }
    public Guid ActorId { get; set; }
    public ScheduleRepairKind Kind { get; set; }
    public DateTime AppliedAt { get; set; }
    [MaxLength(2000)] public string Reason { get; set; } = "";
    [MaxLength(64)] public string PreviewHash { get; set; } = "";
    public string BeforeJson { get; set; } = "{}";
    public string AfterJson { get; set; } = "{}";
    public string ResultJson { get; set; } = "{}";
}

public sealed class ScheduleRepairPreview
{
    public Guid ScheduleId { get; set; }
    public Guid ProductId { get; set; }
    public ScheduleRepairKind Kind { get; set; }
    public string PreviewHash { get; set; } = "";
    public bool CanApply { get; set; }
    public List<string> Blockers { get; set; } = [];
    public List<ScheduleRepairBatchPreview> Batches { get; set; } = [];
}

public sealed class ScheduleRepairBatchPreview
{
    public Guid BatchId { get; set; }
    public Guid? ActivityId { get; set; }
    public string BatchNumber { get; set; } = "";
    public string Status { get; set; } = "";
    public string Message { get; set; } = "";
    public ProductionMaterialRepairPreview Materials { get; set; }
    public ProductionProcedureRepairResult Procedure { get; set; }
    public string EvidenceHash { get; set; } = "";
}

public sealed class ApplyScheduleRepair
{
    public Guid RequestId { get; set; }
    [Required, StringLength(64, MinimumLength = 64)] public string PreviewHash { get; set; } = "";
    [Required, StringLength(2000, MinimumLength = 5)] public string Reason { get; set; } = "";
}

public sealed class ScheduleRepairResult
{
    public Guid RepairId { get; set; }
    public bool AlreadyApplied { get; set; }
    public int Repaired { get; set; }
    public int Unchanged { get; set; }
    public int Skipped { get; set; }
    public int RequiringApproval { get; set; }
}
