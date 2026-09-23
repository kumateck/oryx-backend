namespace DOMAIN.Entities.FullProcedures;

#nullable enable

public class TemplateQuestionContentRequest
{
    public string Wording { get; set; } = string.Empty;
    public TemplateQuestionAnswerType AnswerType { get; set; }
    public string InputType { get; set; } = string.Empty;
    public Guid? UnitOfMeasureId { get; set; }
    public List<TemplateQuestionOptionRequest> Options { get; set; } = [];
    public bool Required { get; set; }
    public string? HelpText { get; set; }
    public decimal? Minimum { get; set; }
    public decimal? Maximum { get; set; }
    public List<Guid> CalculationQuestionIds { get; set; } = [];
    public TemplateQuestionSensitivity Sensitivity { get; set; }
}

public sealed class CreateTemplateQuestionRequest : TemplateQuestionContentRequest
{
    public Guid TemplateAreaId { get; set; }
    public string PurposeId { get; set; } = string.Empty;
    public string SubjectTypeId { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}

public sealed class CreateTemplateQuestionRevisionRequest : TemplateQuestionContentRequest
{
    public string Reason { get; set; } = string.Empty;
}

public sealed class UpdateTemplateQuestionRevisionRequest : TemplateQuestionContentRequest
{
    public string ExpectedContentHash { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}

public sealed class TemplateQuestionTransitionRequest
{
    public string ExpectedContentHash { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}

public sealed class TemplateQuestionOptionRequest
{
    public string Value { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
}

public sealed record TemplateQuestionDto(
    Guid Id,
    Guid TemplateAreaId,
    string AreaName,
    string PurposeId,
    string SubjectTypeId,
    TemplateQuestionRevisionDto? LatestRevision
);

public sealed record TemplateQuestionDetailDto(
    Guid Id,
    Guid TemplateAreaId,
    string AreaName,
    string PurposeId,
    string SubjectTypeId,
    IReadOnlyList<TemplateQuestionRevisionDto> Revisions
);

public sealed record TemplateQuestionRevisionDto(
    Guid Id,
    Guid TemplateQuestionId,
    int Sequence,
    TemplateQuestionRevisionStatus Status,
    string Wording,
    TemplateQuestionAnswerType AnswerType,
    string InputType,
    Guid? UnitOfMeasureId,
    string? UnitOfMeasureName,
    IReadOnlyList<TemplateQuestionOptionDto> Options,
    bool Required,
    string? HelpText,
    decimal? Minimum,
    decimal? Maximum,
    IReadOnlyList<TemplateQuestionReferenceDto> CalculationReferences,
    TemplateQuestionSensitivity Sensitivity,
    string ContentHash,
    Guid? CreatedById,
    Guid? ReviewedById,
    DateTime? ReviewedAt,
    Guid? PublishedById,
    DateTime? PublishedAt,
    DateTime? RetiredAt
);

public sealed record TemplateQuestionOptionDto(string Value, string Label, int Rank);
public sealed record TemplateQuestionReferenceDto(Guid QuestionId, Guid RevisionId);
