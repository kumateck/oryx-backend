using APP.Utils;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Services.Authorization;

/// <summary>
/// Rules for changing who can do what. Nobody changes their own roles or account status, and nobody grants a
/// permission they do not hold themselves, so access can only be handed on, never escalated.
/// </summary>
public static class AccessGuard
{
    /// <summary>The permission keys a set of roles grants, by the same rule as <see cref="CurrentPermissionSet"/>.</summary>
    public static async Task<HashSet<string>> RoleKeys(ApplicationDbContext context, IReadOnlyCollection<Guid> roleIds)
    {
        if (roleIds.Count == 0) return [];
        var claims = await context.RoleClaims.Where(c => roleIds.Contains(c.RoleId) && c.ClaimType == AppConstants.Permission)
            .Select(c => new { c.Id, c.ClaimValue }).ToListAsync();
        var ids = claims.Select(c => c.Id).ToList();
        var types = await context.PermissionTypes.Where(p => ids.Contains(p.RoleClaimId) && p.Type != null && p.Type != "")
            .Select(p => new { p.RoleClaimId, p.Key }).ToListAsync();
        return claims.Where(c => types.Any(t => t.RoleClaimId == c.Id && t.Key == c.ClaimValue)).Select(c => c.ClaimValue).ToHashSet();
    }

    /// <summary>Refuses when <paramref name="granted"/> holds any key the actor does not hold.</summary>
    public static Error Escalation(IReadOnlySet<string> actor, IEnumerable<string> granted)
    {
        var missing = granted.Where(key => !actor.Contains(key)).Distinct().OrderBy(key => key, StringComparer.Ordinal).ToList();
        if (missing.Count == 0) return null;
        var shown = string.Join(", ", missing.Take(5)) + (missing.Count > 5 ? $" and {missing.Count - 5} more" : "");
        return Error.Forbidden("Access.Escalation", $"You cannot grant permissions you do not hold yourself: {shown}.");
    }

    /// <summary>
    /// The system administrator: the one account named as password administrator, active and not deleted, read from the
    /// database (never from the token). It may grant permissions it does not hold, so permissions no role holds yet (a new
    /// feature's key) can be given out at all. Everyone else can only hand on what they hold.
    /// </summary>
    public static async Task<bool> IsSystemAdministrator(ApplicationDbContext context, Guid? actorId)
    {
        if (actorId is not { } id) return false;
        var user = await context.Users.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id);
        return user is not null && APP.Repository.UserPasswordAdminRepository.IsPasswordAdmin(user);
    }

    /// <summary>Refuses assigning roles that carry permissions the actor does not hold.</summary>
    public static async Task<Error> RoleAssignment(ApplicationDbContext context, IReadOnlySet<string> actor, IReadOnlyCollection<Guid> roleIds, Guid actorId) =>
        await IsSystemAdministrator(context, actorId) ? null : Escalation(actor, await RoleKeys(context, roleIds));

    public static Error Missing(IReadOnlySet<string> actor, string key, string task) =>
        actor.Contains(key) ? null : Error.Forbidden("Access.Permission", $"You need the {key} permission to {task}.");

    public static Error Self(Guid actor, Guid target, string task) =>
        actor == target ? Error.Forbidden("Access.Self", $"You cannot {task} on your own account. Ask another administrator.") : null;
}
