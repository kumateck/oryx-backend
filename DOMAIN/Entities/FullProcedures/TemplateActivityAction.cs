using DOMAIN.Entities.Roles;

namespace DOMAIN.Entities.FullProcedures;

public sealed class TemplateActivityAction
{
    public Guid Id { get; set; }
    public Guid TemplateActivityRevisionId { get; set; }
    public TemplateActivityRevision TemplateActivityRevision { get; set; } = null!;
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Order { get; set; }
    public TemplateActivityActionType ActionType { get; set; }
    public bool RequiresIndependentChecker { get; set; }
    public bool RequiresApproval { get; set; }
    public List<TemplateActivityActionRole> Roles { get; set; } = [];
}

public sealed class TemplateActivityActionRole
{
    public Guid Id { get; set; }
    public Guid TemplateActivityActionId { get; set; }
    public TemplateActivityAction TemplateActivityAction { get; set; } = null!;
    public Guid RoleId { get; set; }
    public Role Role { get; set; } = null!;
    public TemplateActivityActionRoleKind RoleKind { get; set; }
}

public enum TemplateActivityActionType
{
    Perform = 0,
    CaptureEvidence = 1,
    ApproveRelease = 2,
    PostTransaction = 3,
}

public enum TemplateActivityActionRoleKind { Performer = 0, Checker = 1, Approver = 2 }
