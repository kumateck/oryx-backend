using DOMAIN.Entities.Base;
using DOMAIN.Entities.Roles;
using DOMAIN.Entities.Users;

namespace DOMAIN.Entities.FullProcedures;

public sealed class TemplateArea : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string NormalizedName { get; set; } = string.Empty;
    public Guid OwnerRoleId { get; set; }
    public Role OwnerRole { get; set; } = null!;
    public string ReviewPolicyId { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public int Version { get; set; } = 1;
    public List<TemplateAreaPurpose> Purposes { get; set; } = [];
    public List<TemplateAreaSubjectType> SubjectTypes { get; set; } = [];
    public List<TemplateAreaCapability> Capabilities { get; set; } = [];
    public List<TemplateAreaRoleGrant> RoleGrants { get; set; } = [];
    public List<TemplateAreaAudit> Audits { get; set; } = [];
}

public sealed class TemplateAreaPurpose
{
    public Guid Id { get; set; }
    public Guid TemplateAreaId { get; set; }
    public TemplateArea TemplateArea { get; set; } = null!;
    public string PurposeId { get; set; } = string.Empty;
}

public sealed class TemplateAreaSubjectType
{
    public Guid Id { get; set; }
    public Guid TemplateAreaId { get; set; }
    public TemplateArea TemplateArea { get; set; } = null!;
    public string SubjectTypeId { get; set; } = string.Empty;
}

public sealed class TemplateAreaCapability
{
    public Guid Id { get; set; }
    public Guid TemplateAreaId { get; set; }
    public TemplateArea TemplateArea { get; set; } = null!;
    public string CapabilityId { get; set; } = string.Empty;
}

public sealed class TemplateAreaRoleGrant
{
    public Guid Id { get; set; }
    public Guid TemplateAreaId { get; set; }
    public TemplateArea TemplateArea { get; set; } = null!;
    public Guid RoleId { get; set; }
    public Role Role { get; set; } = null!;
    public TemplateAreaAccessLevel AccessLevel { get; set; }
}

public sealed class TemplateAreaAudit
{
    public Guid Id { get; set; }
    public Guid TemplateAreaId { get; set; }
    public TemplateArea TemplateArea { get; set; } = null!;
    public Guid ActorId { get; set; }
    public User Actor { get; set; } = null!;
    public string Action { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public int Version { get; set; }
    public string SnapshotJson { get; set; } = string.Empty;
    public string SnapshotHash { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; }
}

public enum TemplateAreaAccessLevel
{
    Viewer = 0,
    Author = 1,
    Reviewer = 2,
    Publisher = 3,
    Administrator = 4,
}
