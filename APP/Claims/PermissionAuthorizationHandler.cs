using System.Security.Claims;
using APP.Services.Authorization;
using INFRASTRUCTURE.Context;
using Microsoft.AspNetCore.Authorization;

namespace APP.Claims;

/// <summary>
/// Grants a permission policy from the same source as the permission checks inside services
/// (<see cref="CurrentPermissionSet"/>): a disabled account holds nothing, a deleted role grants nothing,
/// and a revoked permission stops working on the next request. Grants are read once per request.
/// </summary>
internal class PermissionAuthorizationHandler(ApplicationDbContext context) : AuthorizationHandler<PermissionRequirement>
{
    private Guid? _loadedFor;
    private HashSet<string> _grants = [];

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext authorizationHandlerContext,
        PermissionRequirement requirement)
    {
        var userIdClaim = authorizationHandlerContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            return;

        if (_loadedFor != userId)
        {
            _grants = await CurrentPermissionSet.Load(context, userId);
            _loadedFor = userId;
        }

        if (_grants.Contains(requirement.Permission))
            authorizationHandlerContext.Succeed(requirement);
    }
}
