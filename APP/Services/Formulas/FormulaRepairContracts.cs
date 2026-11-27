#nullable enable
namespace APP.Services.Formulas;

public sealed record FormulaRepairTarget(Guid AtrId, Guid FormFieldId);
public sealed record FormulaRepairCandidate(string Definition, bool UpdateQuestion, List<FormulaRepairTarget> Targets);
public sealed record ApplyFormulaRepairRequest(FormulaRepairCandidate Candidate, string PreviewToken, Guid RequestId, string Reason);
/// <summary>One test field the formula is on. <c>CurrentDefinition</c> is the formula that test calculates with now.</summary>
public sealed record FormulaRepairTestDto(Guid AtrId, Guid? ResponseId, Guid FormFieldId, string Label, bool Eligible, string? Blocker,
    string? CurrentDefinition = null);
/// <summary>
/// <c>ApprovedDefinition</c> is the formula of the question's approved, effective revision. When there is one, a repair applies
/// exactly that formula: corrections are made through a new revision, not typed into the repair.
/// </summary>
public sealed record FormulaRepairContextDto(Guid QuestionId, string Label, string Definition, string DefinitionHash,
    bool CanRepairQuestion, bool CanRepairTests, int FormsUsingQuestion, int ResponsesAffected,
    List<FormulaRepairTestDto> Tests, List<string> Blockers, string? ApprovedDefinition = null, int? ApprovedRevision = null);
/// <summary>One selected test field in a preview: whether readings exist, the formula changes for it, and inputs are missing.</summary>
public sealed record FormulaRepairPreviewTestDto(Guid AtrId, Guid FormFieldId, string Label, bool HasReadings, bool ConfigurationChanges, bool MissingInputs);
public sealed record FormulaRepairPreviewDto(string PreviewToken, string Before, string After, int Captures,
    int SelectedTests, List<string> MissingInputs, List<string> Blockers, List<FormulaRepairPreviewTestDto> Tests);
public sealed record FormulaRepairResultDto(Guid RepairId, bool AlreadyApplied, List<Guid> FormIds, List<Guid> ResponseIds);
public sealed record FormulaRepairHistoryDto(Guid Id, Guid ActorId, string ActorName, DateTime AppliedAt, string Reason,
    string DefinitionBefore, string DefinitionAfter, bool UpdatedQuestion, int RepairedTests, int CapturedTests);
/// <summary>A repaired Percentage result stored as a fraction. <c>Blocker</c> says why it cannot be recalculated now.</summary>
public sealed record PercentageRecalculationItemDto(Guid ResponseId, Guid FormFieldId, Guid QuestionId, string Test, string? Product,
    string? Batch, string? ArNumber, string? TestStatus, string CurrentResults, bool Finalized, string? Blocker);
public sealed record PercentageRecalculationTarget(Guid ResponseId, Guid FormFieldId);
public sealed record ApplyPercentageRecalculationRequest(string Reason, List<PercentageRecalculationTarget> Targets);
/// <summary>The outcome for one result: the earlier and the recalculated values, or why it was not recalculated.</summary>
public sealed record PercentageRecalculationResultDto(Guid ResponseId, Guid FormFieldId, bool Recalculated, string? PreviousResults,
    string? Results, string? Problem);
