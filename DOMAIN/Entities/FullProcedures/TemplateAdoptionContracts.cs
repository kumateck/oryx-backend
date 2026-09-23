namespace DOMAIN.Entities.FullProcedures;

public sealed class AdoptTemplateRevisionRequest
{
    public int ExpectedGrantVersion { get; set; }
    public string Reason { get; set; } = string.Empty;
    public List<TemplateAdoptionDependencyMappingRequest> DependencyMappings { get; set; } = [];
    public List<TemplateAdoptionRoleMappingRequest> RoleMappings { get; set; } = [];
}

public sealed class TemplateAdoptionDependencyMappingRequest
{
    public Guid SourceDefinitionId { get; set; }
    public Guid SourceRevisionId { get; set; }
    public Guid TargetDefinitionId { get; set; }
    public Guid TargetRevisionId { get; set; }
}

public sealed class TemplateAdoptionRoleMappingRequest
{
    public Guid SourceRoleId { get; set; }
    public Guid TargetRoleId { get; set; }
}

public sealed record TemplateAdoptionDto(Guid Id, Guid TemplateSharingGrantId,
    TemplateRevisionKind TemplateKind, Guid SourceDefinitionId, Guid SourceRevisionId,
    string SourceContentHash, int GrantVersion, Guid TargetAreaId,
    Guid TargetDefinitionId, Guid TargetRevisionId, string TargetContentHash,
    IReadOnlyList<TemplateAdoptionDependencyMappingDto> DependencyMappings,
    IReadOnlyList<TemplateAdoptionRoleMappingDto> RoleMappings, string Reason,
    string SnapshotHash, Guid AdoptedById, DateTime AdoptedAt, Guid CorrelationId);

public sealed record TemplateAdoptionDependencyMappingDto(Guid SourceDefinitionId,
    Guid SourceRevisionId, Guid TargetDefinitionId, Guid TargetRevisionId);

public sealed record TemplateAdoptionRoleMappingDto(Guid SourceRoleId, Guid TargetRoleId);
