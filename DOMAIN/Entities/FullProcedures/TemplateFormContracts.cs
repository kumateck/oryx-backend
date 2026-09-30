namespace DOMAIN.Entities.FullProcedures;

#nullable enable

public class TemplateFormContentRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool RequiresEvidence { get; set; }
    public bool RequiresSignature { get; set; }
    public List<TemplateFormSectionRequest> Sections { get; set; } = [];
    public List<TemplateFormConditionalRuleRequest> ConditionalRules { get; set; } = [];
}

public sealed class CreateTemplateFormRequest : TemplateFormContentRequest
{
    public Guid TemplateAreaId { get; set; }
    public string PurposeId { get; set; } = string.Empty;
    public string SubjectTypeId { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}

public sealed class CreateTemplateFormRevisionRequest : TemplateFormContentRequest
{
    public string Reason { get; set; } = string.Empty;
}

public sealed class UpdateTemplateFormRevisionRequest : TemplateFormContentRequest
{
    public string ExpectedContentHash { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}

public sealed class TemplateFormTransitionRequest
{
    public string ExpectedContentHash { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}

public sealed class TemplateFormSectionRequest
{
    public Guid SectionId { get; set; }
    public Guid RevisionId { get; set; }
    public int Order { get; set; }
    public bool IsRequired { get; set; }
}

public sealed class TemplateFormConditionalRuleRequest
{
    public Guid TargetSectionId { get; set; }
    public Guid SourceSectionId { get; set; }
    public Guid SourceQuestionId { get; set; }
    public Guid SourceQuestionRevisionId { get; set; }
    public TemplateFormConditionOperator Operator { get; set; }
    public string? Value { get; set; }
}

public sealed record TemplateFormDto(
    Guid Id, Guid TemplateAreaId, string AreaName, string PurposeId,
    string SubjectTypeId, TemplateFormRevisionDto? LatestRevision);

public sealed record TemplateFormDetailDto(
    Guid Id, Guid TemplateAreaId, string AreaName, string PurposeId,
    string SubjectTypeId, IReadOnlyList<TemplateFormRevisionDto> Revisions);

public sealed record TemplateFormRevisionDto(
    Guid Id, Guid TemplateFormId, int Sequence, TemplateFormRevisionStatus Status,
    string Name, string Description, bool RequiresEvidence, bool RequiresSignature,
    IReadOnlyList<TemplateFormSectionDto> Sections,
    IReadOnlyList<TemplateFormConditionalRuleDto> ConditionalRules,
    string ContentHash, Guid? CreatedById, Guid? ReviewedById, DateTime? ReviewedAt,
    Guid? PublishedById, DateTime? PublishedAt, DateTime? RetiredAt);

public sealed record TemplateFormSectionDto(
    Guid SectionId, Guid RevisionId, int Order, bool IsRequired, string Title);

public sealed record TemplateFormConditionalRuleDto(
    Guid TargetSectionId, Guid SourceSectionId, Guid SourceQuestionId,
    Guid SourceQuestionRevisionId, TemplateFormConditionOperator Operator, string? Value);
