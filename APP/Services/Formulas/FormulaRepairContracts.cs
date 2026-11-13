#nullable enable
namespace APP.Services.Formulas;

public sealed record FormulaRepairTarget(Guid AtrId, Guid FormFieldId);
public sealed record FormulaRepairCandidate(string Definition, bool UpdateQuestion, List<FormulaRepairTarget> Targets);
public sealed record ApplyFormulaRepairRequest(FormulaRepairCandidate Candidate, string PreviewToken, Guid RequestId, string Reason);
public sealed record FormulaRepairTestDto(Guid AtrId, Guid? ResponseId, Guid FormFieldId, string Label, bool Eligible, string? Blocker);
public sealed record FormulaRepairContextDto(Guid QuestionId, string Label, string Definition, string DefinitionHash,
    bool CanRepairQuestion, bool CanRepairTests, int FormsUsingQuestion, int ResponsesAffected,
    List<FormulaRepairTestDto> Tests, List<string> Blockers);
/// <summary>One selected test field in a preview: whether readings exist, the formula changes for it, and inputs are missing.</summary>
public sealed record FormulaRepairPreviewTestDto(Guid AtrId, Guid FormFieldId, string Label, bool HasReadings, bool ConfigurationChanges, bool MissingInputs);
public sealed record FormulaRepairPreviewDto(string PreviewToken, string Before, string After, int Captures,
    int SelectedTests, List<string> MissingInputs, List<string> Blockers, List<FormulaRepairPreviewTestDto> Tests);
public sealed record FormulaRepairResultDto(Guid RepairId, bool AlreadyApplied, List<Guid> FormIds, List<Guid> ResponseIds);
public sealed record FormulaRepairHistoryDto(Guid Id, Guid ActorId, string ActorName, DateTime AppliedAt, string Reason,
    string DefinitionBefore, string DefinitionAfter, bool UpdatedQuestion, int RepairedTests, int CapturedTests);
