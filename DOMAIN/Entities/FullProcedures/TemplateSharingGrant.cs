using DOMAIN.Entities.Base;
using DOMAIN.Entities.Users;

#nullable enable

namespace DOMAIN.Entities.FullProcedures;

public sealed class TemplateSharingGrant : BaseEntity
{
    public Guid SourceAreaId { get; set; }
    public TemplateArea SourceArea { get; set; } = null!;
    public Guid TargetAreaId { get; set; }
    public TemplateArea TargetArea { get; set; } = null!;
    public Guid RequestedByAreaId { get; set; }
    public TemplateRevisionKind TemplateKind { get; set; }
    public Guid DefinitionId { get; set; }
    public Guid RevisionId { get; set; }
    public string RevisionContentHash { get; set; } = string.Empty;
    public string PurposeId { get; set; } = string.Empty;
    public string SubjectTypeId { get; set; } = string.Empty;
    public TemplateSharingGrantStatus Status { get; set; }
    public int Version { get; set; } = 1;
    public Guid RequestedById { get; set; }
    public User RequestedBy { get; set; } = null!;
    public DateTime RequestedAt { get; set; }
    public Guid? DecidedById { get; set; }
    public User? DecidedBy { get; set; }
    public DateTime? DecidedAt { get; set; }
    public Guid? RevokedById { get; set; }
    public User? RevokedBy { get; set; }
    public DateTime? RevokedAt { get; set; }
    public List<TemplateSharingGrantAudit> Audits { get; set; } = [];
}

public sealed class TemplateSharingGrantAudit
{
    public Guid Id { get; set; }
    public Guid TemplateSharingGrantId { get; set; }
    public TemplateSharingGrant TemplateSharingGrant { get; set; } = null!;
    public TemplateSharingGrantStatus? PriorStatus { get; set; }
    public TemplateSharingGrantStatus NewStatus { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public int Version { get; set; }
    public string SnapshotJson { get; set; } = string.Empty;
    public string SnapshotHash { get; set; } = string.Empty;
    public Guid ActorId { get; set; }
    public User Actor { get; set; } = null!;
    public DateTime OccurredAt { get; set; }
    public Guid CorrelationId { get; set; }
}

public enum TemplateRevisionKind { Question = 0, Section = 1, Form = 2, Activity = 3, Workflow = 4 }
public enum TemplateSharingGrantStatus { Pending = 0, Active = 1, Rejected = 2, Revoked = 3 }
