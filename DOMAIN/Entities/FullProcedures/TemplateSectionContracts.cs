namespace DOMAIN.Entities.FullProcedures;

#nullable enable

public class TemplateSectionContentRequest
{
    public string Title { get; set; } = string.Empty;
    public List<TemplateSectionQuestionRequest> Questions { get; set; } = [];
    public List<TemplateSectionConditionalRuleRequest> ConditionalRules { get; set; } = [];
}

public sealed class CreateTemplateSectionRequest : TemplateSectionContentRequest
{
    public Guid TemplateAreaId { get; set; }
    public string PurposeId { get; set; } = string.Empty;
    public string SubjectTypeId { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}

public sealed class CreateTemplateSectionRevisionRequest : TemplateSectionContentRequest
{
    public string Reason { get; set; } = string.Empty;
}

public sealed class UpdateTemplateSectionRevisionRequest : TemplateSectionContentRequest
{
    public string ExpectedContentHash { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}

public sealed class TemplateSectionTransitionRequest
{
    public string ExpectedContentHash { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}

public sealed class TemplateSectionQuestionRequest
{
    public Guid QuestionId { get; set; }
    public Guid RevisionId { get; set; }
    public int Order { get; set; }
}

public sealed class TemplateSectionConditionalRuleRequest
{
    public Guid TargetQuestionId { get; set; }
    public Guid DependsOnQuestionId { get; set; }
    public TemplateSectionConditionOperator Operator { get; set; }
    public string? Value { get; set; }
}

public sealed record TemplateSectionDto(
    Guid Id, Guid TemplateAreaId, string AreaName, string PurposeId,
    string SubjectTypeId, TemplateSectionRevisionDto? LatestRevision);

public sealed record TemplateSectionDetailDto(
    Guid Id, Guid TemplateAreaId, string AreaName, string PurposeId,
    string SubjectTypeId, IReadOnlyList<TemplateSectionRevisionDto> Revisions);

public sealed record TemplateSectionRevisionDto(
    Guid Id, Guid TemplateSectionId, int Sequence,
    TemplateSectionRevisionStatus Status, string Title,
    IReadOnlyList<TemplateSectionQuestionDto> Questions,
    IReadOnlyList<TemplateSectionConditionalRuleDto> ConditionalRules,
    string ContentHash, Guid? CreatedById, Guid? ReviewedById,
    DateTime? ReviewedAt, Guid? PublishedById, DateTime? PublishedAt,
    DateTime? RetiredAt);

public sealed record TemplateSectionQuestionDto(
    Guid QuestionId, Guid RevisionId, int Order, string Wording,
    TemplateQuestionAnswerType AnswerType);

public sealed record TemplateSectionConditionalRuleDto(
    Guid TargetQuestionId, Guid DependsOnQuestionId,
    TemplateSectionConditionOperator Operator, string? Value);
