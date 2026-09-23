namespace DOMAIN.Entities.FullProcedures;

#nullable enable

public class ProcedureContentRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid TemplateWorkflowId { get; set; }
    public Guid TemplateWorkflowRevisionId { get; set; }
    public string ParameterSchemaJson { get; set; } = "{}";
    public List<ProcedureApplicabilityRequest> Applicabilities { get; set; } = [];
    public List<ProcedureStageScopeRequest> StageScopes { get; set; } = [];
}

public sealed class CreateProcedureRequest : ProcedureContentRequest
{
    public string Reason { get; set; } = string.Empty;
}

public sealed class CreateProcedureRevisionRequest : ProcedureContentRequest
{
    public string Reason { get; set; } = string.Empty;
}

public sealed class UpdateProcedureRevisionRequest : ProcedureContentRequest
{
    public string ExpectedContentHash { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}

public sealed class ProcedureTransitionRequest
{
    public string ExpectedContentHash { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}

public sealed class ProcedureApplicabilityRequest
{
    public Guid ProductId { get; set; }
    public Guid SiteId { get; set; }
    public ProcedureBatchType BatchType { get; set; }
}

public sealed class ProcedureStageScopeRequest
{
    public string WorkflowNodeKey { get; set; } = string.Empty;
    public ProcedureRecordScope RecordScope { get; set; }
}

public sealed record ProcedureDefinitionDto(Guid Id, Guid TemplateAreaId, string AreaName,
    string PurposeId, string SubjectTypeId, ProcedureRevisionDto? LatestRevision);

public sealed record ProcedureDefinitionDetailDto(Guid Id, Guid TemplateAreaId, string AreaName,
    string PurposeId, string SubjectTypeId, IReadOnlyList<ProcedureRevisionDto> Revisions);

public sealed record ProcedureRevisionDto(Guid Id, Guid ProcedureDefinitionId, int Sequence,
    ProcedureRevisionStatus Status, string Name, string Description,
    Guid TemplateWorkflowId, Guid TemplateWorkflowRevisionId, string WorkflowName,
    string WorkflowContentHash, string ParameterSchemaJson,
    IReadOnlyList<ProcedureApplicabilityDto> Applicabilities,
    IReadOnlyList<ProcedureStageScopeDto> StageScopes, string ContentHash,
    Guid? CreatedById, Guid? ReviewedById, DateTime? ReviewedAt,
    Guid? ApprovedById, DateTime? ApprovedAt, DateTime? RetiredAt);

public sealed record ProcedureApplicabilityDto(Guid ProductId, string ProductName,
    Guid SiteId, string SiteName, ProcedureBatchType BatchType);

public sealed record ProcedureStageScopeDto(string WorkflowNodeKey, string WorkflowNodeName,
    ProcedureRecordScope RecordScope);

public sealed record ProcedureValidationReportDto(bool IsValid,
    IReadOnlyList<string> BlockedReasons, string ContentHash);
