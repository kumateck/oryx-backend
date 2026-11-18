#nullable enable
namespace APP.Services.Forms;

/// <summary>What a worksheet test is: a product analytical test request, or a raw or packaging material test (GRN or retest).</summary>
public enum TemplateRepairTestKind { Product = 0, RawMaterial = 1, PackagingMaterial = 2 }

/// <summary>A live field of the template, in section and rank order.</summary>
public sealed record TemplateRepairFieldDto(Guid FormFieldId, Guid SectionId, string Section, string Label, bool Required);

/// <summary>An analyst already on a test, with how many of its fields they hold.</summary>
public sealed record TemplateRepairAnalystDto(Guid UserId, string Name, int Fields);

/// <summary>A template field nobody on the test was given, and the analyst suggested for it (the one holding most of its section).</summary>
public sealed record TemplateRepairMissingFieldDto(Guid FormFieldId, Guid? SuggestedAssigneeId);

/// <summary>
/// One assigned test of the template. <c>RecordId</c> is the ATR, the material batch (GRN test) or the retest.
/// <c>Missing</c> lists the template's fields no analyst on this test holds, so no one can see or fill them.
/// </summary>
public sealed record TemplateRepairTestDto(Guid FormAssigneeId, TemplateRepairTestKind Kind, bool Retest, Guid RecordId, string Label,
    Guid? ResponseId, int AnsweredFields, bool Eligible, string? Blocker, List<TemplateRepairAnalystDto> Analysts,
    List<TemplateRepairMissingFieldDto> Missing);

/// <summary>
/// The repair workspace for a template: its live fields and the open tests missing some of them. <c>HiddenTests</c> counts
/// open tests needing repair that the caller cannot reassign, so they are not listed.
/// </summary>
public sealed record TemplateRepairContextDto(Guid FormId, string FormName, bool CanApply, List<TemplateRepairFieldDto> Fields,
    List<TemplateRepairTestDto> Tests, int HiddenTests);

/// <summary>Give the template field <c>FormFieldId</c> on one test to <c>AssigneeId</c>, an analyst already on that test.</summary>
public sealed record TemplateRepairAssignmentInput(Guid FormAssigneeId, Guid FormFieldId, Guid AssigneeId);
public sealed record TemplateRepairCandidate(List<TemplateRepairAssignmentInput> Assignments);
public sealed record ApplyTemplateRepairRequest(TemplateRepairCandidate Candidate, string PreviewToken, Guid RequestId, string Reason);

public sealed record TemplateRepairPreviewFieldDto(Guid FormFieldId, string Section, string Label, Guid AssigneeId, string AssigneeName);
/// <summary>One selected test in a preview: the fields it gains and how many saved answers it keeps unchanged.</summary>
public sealed record TemplateRepairPreviewTestDto(Guid FormAssigneeId, string Label, int AnswersKept, List<TemplateRepairPreviewFieldDto> Added);
public sealed record TemplateRepairPreviewDto(string PreviewToken, int FieldsAdded, List<TemplateRepairPreviewTestDto> Tests, List<string> Blockers);
public sealed record TemplateRepairResultDto(Guid RepairId, bool AlreadyApplied, List<Guid> FormAssigneeIds, int FieldsAdded);
public sealed record TemplateRepairHistoryDto(Guid Id, Guid ActorId, string ActorName, DateTime AppliedAt, string Reason, int Tests, int Fields);
