namespace DOMAIN.Entities.FullProcedures;

public class TemplateAreaDraftRequest
{
    public string Name { get; set; } = string.Empty;
    public Guid OwnerRoleId { get; set; }
    public List<string> PurposeIds { get; set; } = [];
    public List<string> SubjectTypeIds { get; set; } = [];
    public List<string> CapabilityIds { get; set; } = [];
    public string ReviewPolicyId { get; set; } = string.Empty;
    public List<TemplateAreaRoleGrantRequest> RoleGrants { get; set; } = [];
    public string Reason { get; set; } = string.Empty;
}

public sealed class UpdateTemplateAreaRequest : TemplateAreaDraftRequest
{
    public int ExpectedVersion { get; set; }
}

public sealed class ChangeTemplateAreaActiveRequest
{
    public bool IsActive { get; set; }
    public int ExpectedVersion { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public sealed class TemplateAreaRoleGrantRequest
{
    public Guid RoleId { get; set; }
    public TemplateAreaAccessLevel AccessLevel { get; set; }
}

public sealed record TemplateAreaDto(
    Guid Id,
    string Name,
    Guid OwnerRoleId,
    string OwnerRoleName,
    IReadOnlyList<string> PurposeIds,
    IReadOnlyList<string> SubjectTypeIds,
    IReadOnlyList<string> CapabilityIds,
    string ReviewPolicyId,
    IReadOnlyList<TemplateAreaRoleGrantDto> RoleGrants,
    bool IsActive,
    int Version,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public sealed record TemplateAreaRoleGrantDto(
    Guid RoleId,
    string RoleName,
    TemplateAreaAccessLevel AccessLevel
);

public sealed record TemplateAreaCatalogDto(
    IReadOnlyList<TemplatePurposeDto> Purposes,
    IReadOnlyList<TemplateCatalogItemDto> SubjectTypes,
    IReadOnlyList<TemplateCatalogItemDto> Capabilities,
    IReadOnlyList<TemplateCatalogItemDto> ReviewPolicies,
    IReadOnlyList<TemplateOwnerGroupDto> OwnerGroups
);

public sealed record TemplatePurposeDto(
    string Id,
    string Name,
    IReadOnlyList<int> AllowedKinds,
    IReadOnlyList<string> SubjectTypeIds,
    IReadOnlyList<string> CapabilityIds
);

public sealed record TemplateCatalogItemDto(string Id, string Name);
public sealed record TemplateOwnerGroupDto(Guid Id, string Name);
