using DOMAIN.Entities.Base;
using DOMAIN.Entities.Users;

#nullable enable

namespace DOMAIN.Entities.FullProcedures;

public sealed class TemplateAdoption : BaseEntity
{
    public Guid TemplateSharingGrantId { get; set; }
    public TemplateSharingGrant TemplateSharingGrant { get; set; } = null!;
    public TemplateRevisionKind TemplateKind { get; set; }
    public Guid SourceDefinitionId { get; set; }
    public Guid SourceRevisionId { get; set; }
    public string SourceContentHash { get; set; } = string.Empty;
    public int GrantVersion { get; set; }
    public Guid TargetAreaId { get; set; }
    public TemplateArea TargetArea { get; set; } = null!;
    public Guid TargetDefinitionId { get; set; }
    public Guid TargetRevisionId { get; set; }
    public string TargetContentHash { get; set; } = string.Empty;
    public string DependencyMappingsJson { get; set; } = "[]";
    public string RoleMappingsJson { get; set; } = "[]";
    public string Reason { get; set; } = string.Empty;
    public string SnapshotHash { get; set; } = string.Empty;
    public Guid AdoptedById { get; set; }
    public User AdoptedBy { get; set; } = null!;
    public DateTime AdoptedAt { get; set; }
    public Guid CorrelationId { get; set; }
}
