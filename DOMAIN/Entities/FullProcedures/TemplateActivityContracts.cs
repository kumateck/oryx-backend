namespace DOMAIN.Entities.FullProcedures;

#nullable enable

public class TemplateActivityContentRequest
{
    public string Name { get; set; } = string.Empty;
    public string Instructions { get; set; } = string.Empty;
    public List<TemplateActivityFormBindingRequest> Forms { get; set; } = [];
    public List<TemplateActivityActionRequest> Actions { get; set; } = [];
    public List<TemplateActivityResourceRequest> Resources { get; set; } = [];
    public List<TemplateActivityDataBindingRequest> DataBindings { get; set; } = [];
    public List<TemplateActivityCompletionRuleRequest> CompletionRules { get; set; } = [];
}

public sealed class CreateTemplateActivityRequest : TemplateActivityContentRequest
{
    public Guid TemplateAreaId { get; set; }
    public string PurposeId { get; set; } = string.Empty;
    public string SubjectTypeId { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}

public sealed class CreateTemplateActivityRevisionRequest : TemplateActivityContentRequest
{
    public string Reason { get; set; } = string.Empty;
}

public sealed class UpdateTemplateActivityRevisionRequest : TemplateActivityContentRequest
{
    public string ExpectedContentHash { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}

public sealed class TemplateActivityTransitionRequest
{
    public string ExpectedContentHash { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}

public sealed class TemplateActivityFormBindingRequest
{
    public Guid FormId { get; set; }
    public Guid RevisionId { get; set; }
    public string Key { get; set; } = string.Empty;
    public int Order { get; set; }
    public TemplateActivityFormUsage Usage { get; set; }
    public bool IsRequired { get; set; }
}

public sealed class TemplateActivityActionRequest
{
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Order { get; set; }
    public TemplateActivityActionType ActionType { get; set; }
    public bool RequiresIndependentChecker { get; set; }
    public bool RequiresApproval { get; set; }
    public List<Guid> PerformerRoleIds { get; set; } = [];
    public List<Guid> CheckerRoleIds { get; set; } = [];
    public List<Guid> ApproverRoleIds { get; set; } = [];
}

public sealed class TemplateActivityResourceRequest
{
    public string CapabilityId { get; set; } = string.Empty;
    public int Order { get; set; }
    public bool IsRequired { get; set; }
}

public sealed class TemplateActivityDataBindingRequest
{
    public string Key { get; set; } = string.Empty;
    public int Order { get; set; }
    public TemplateActivityDataDirection Direction { get; set; }
    public TemplateActivityDataType DataType { get; set; }
    public bool IsRequired { get; set; }
}

public sealed class TemplateActivityCompletionRuleRequest
{
    public int Order { get; set; }
    public TemplateActivityCompletionRuleType RuleType { get; set; }
    public string? TargetKey { get; set; }
}

public sealed record TemplateActivityDto(Guid Id, Guid TemplateAreaId, string AreaName,
    string PurposeId, string SubjectTypeId, TemplateActivityRevisionDto? LatestRevision);
public sealed record TemplateActivityDetailDto(Guid Id, Guid TemplateAreaId, string AreaName,
    string PurposeId, string SubjectTypeId, IReadOnlyList<TemplateActivityRevisionDto> Revisions);
public sealed record TemplateActivityRevisionDto(Guid Id, Guid TemplateActivityId, int Sequence,
    TemplateActivityRevisionStatus Status, string Name, string Instructions,
    IReadOnlyList<TemplateActivityFormBindingDto> Forms,
    IReadOnlyList<TemplateActivityActionDto> Actions,
    IReadOnlyList<TemplateActivityResourceDto> Resources,
    IReadOnlyList<TemplateActivityDataBindingDto> DataBindings,
    IReadOnlyList<TemplateActivityCompletionRuleDto> CompletionRules,
    string ContentHash, Guid? CreatedById, Guid? ReviewedById, DateTime? ReviewedAt,
    Guid? PublishedById, DateTime? PublishedAt, DateTime? RetiredAt);
public sealed record TemplateActivityFormBindingDto(Guid FormId, Guid RevisionId,
    string Key, int Order, TemplateActivityFormUsage Usage, bool IsRequired, string Name);
public sealed record TemplateActivityActionDto(string Key, string Name, int Order,
    TemplateActivityActionType ActionType, bool RequiresIndependentChecker,
    bool RequiresApproval, IReadOnlyList<Guid> PerformerRoleIds,
    IReadOnlyList<Guid> CheckerRoleIds, IReadOnlyList<Guid> ApproverRoleIds);
public sealed record TemplateActivityResourceDto(string CapabilityId, int Order, bool IsRequired);
public sealed record TemplateActivityDataBindingDto(string Key, int Order,
    TemplateActivityDataDirection Direction, TemplateActivityDataType DataType, bool IsRequired);
public sealed record TemplateActivityCompletionRuleDto(int Order,
    TemplateActivityCompletionRuleType RuleType, string? TargetKey);
