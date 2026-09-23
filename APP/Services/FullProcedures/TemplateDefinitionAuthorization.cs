using DOMAIN.Entities.FullProcedures;

namespace APP.Services.FullProcedures;

internal enum TemplateDefinitionOperation
{
    View = 0,
    Author = 1,
    Review = 2,
    Publish = 3,
}

internal static class TemplateDefinitionAuthorization
{
    internal static bool Allows(
        TemplateArea area, IReadOnlyCollection<Guid> roleIds,
        TemplateDefinitionOperation operation)
    {
        if (roleIds.Contains(area.OwnerRoleId)) return true;
        return area.RoleGrants.Any(grant => roleIds.Contains(grant.RoleId) &&
            (grant.AccessLevel == TemplateAreaAccessLevel.Administrator ||
             Matches(grant.AccessLevel, operation)));
    }

    private static bool Matches(
        TemplateAreaAccessLevel accessLevel, TemplateDefinitionOperation operation) =>
        operation switch
        {
            TemplateDefinitionOperation.View => true,
            TemplateDefinitionOperation.Author => accessLevel == TemplateAreaAccessLevel.Author,
            TemplateDefinitionOperation.Review => accessLevel == TemplateAreaAccessLevel.Reviewer,
            TemplateDefinitionOperation.Publish => accessLevel == TemplateAreaAccessLevel.Publisher,
            _ => false,
        };
}
