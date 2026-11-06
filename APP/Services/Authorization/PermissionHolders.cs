using APP.Utils;
using DOMAIN.Entities.Users;
using INFRASTRUCTURE.Context;

namespace APP.Services.Authorization;

/// <summary>
/// The users who hold any of the given keys right now, by the same rule as <see cref="CurrentPermissionSet"/>: an active
/// (not disabled, not deleted) user in a role that still exists, whose permission claim for the key has an access type.
/// Lists of people who may be given a task use this, so nobody is offered who would then be refused.
/// </summary>
public static class PermissionHolders
{
    public static IQueryable<User> Query(ApplicationDbContext context, IReadOnlyCollection<string> keys)
    {
        var roles = context.RoleClaims
            .Where(claim => claim.ClaimType == AppConstants.Permission && keys.Contains(claim.ClaimValue)
                && context.Roles.Any(role => role.Id == claim.RoleId)
                && context.PermissionTypes.Any(type => type.RoleClaimId == claim.Id && type.Key == claim.ClaimValue && type.Type != null && type.Type != ""))
            .Select(claim => claim.RoleId);
        return context.Users.Where(user => !user.IsDisabled
            && context.UserRoles.Any(link => link.UserId == user.Id && roles.Contains(link.RoleId)));
    }
}
