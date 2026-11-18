namespace DOMAIN.Entities.Forms;

/// <summary>
/// An applied template repair: fields added to a worksheet template after its tests were assigned are given, on selected
/// open tests, to analysts already on those tests. Append-only; the evidence keeps the reviewed preview.
/// </summary>
public sealed class TemplateRepair
{
    public Guid Id { get; set; }
    public Guid FormId { get; set; }
    public Guid ActorId { get; set; }
    public string Reason { get; set; } = "";
    public string PreviewToken { get; set; } = "";
    public string RequestHash { get; set; } = "";
    public string EvidenceJson { get; set; } = "{}";
    public DateTime AppliedAt { get; set; }
}

/// <summary>One field a template repair gave to an analyst on one test. Append-only.</summary>
public sealed class TemplateRepairAssignment
{
    public Guid Id { get; set; }
    public Guid RepairId { get; set; }
    public Guid FormAssigneeId { get; set; }
    public Guid FormFieldAssigneeId { get; set; }
    public Guid FormFieldId { get; set; }
    public Guid AssigneeId { get; set; }
}
