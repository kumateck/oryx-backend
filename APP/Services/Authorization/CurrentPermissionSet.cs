using APP.Utils;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;

namespace APP.Services.Authorization;

public static class CurrentPermissionSet
{
    public static async Task<HashSet<string>> Load(ApplicationDbContext context, Guid? actor)
    {
        if (!actor.HasValue || !await context.Users.AnyAsync(u => u.Id == actor && !u.IsDisabled)) return [];
        var roles = await context.UserRoles.Where(r => r.UserId == actor && context.Roles.Any(role => role.Id == r.RoleId))
            .Select(r => r.RoleId).ToListAsync();
        var claims = await context.RoleClaims.Where(c => roles.Contains(c.RoleId) && c.ClaimType == AppConstants.Permission)
            .Select(c => new { c.Id, c.ClaimValue }).ToListAsync();
        var ids = claims.Select(c => c.Id).ToList();
        var types = await context.PermissionTypes.Where(p => ids.Contains(p.RoleClaimId) && p.Type != null && p.Type != "")
            .Select(p => new { p.RoleClaimId, p.Key }).ToListAsync();
        return claims.Where(c => types.Any(t => t.RoleClaimId == c.Id && t.Key == c.ClaimValue))
            .Select(c => c.ClaimValue).ToHashSet();
    }
}
