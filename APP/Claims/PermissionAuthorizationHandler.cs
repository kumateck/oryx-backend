using System.Security.Claims;
using INFRASTRUCTURE.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace APP.Claims;

internal class PermissionAuthorizationHandler(ApplicationDbContext context, IMemoryCache cache) : AuthorizationHandler<PermissionRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext authorizationHandlerContext,
    PermissionRequirement requirement)
    {
        var userIdClaim = authorizationHandlerContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            // Log and return if user ID is not valid
            return;
        }

        var cacheKey = $"UserId_{userId}_Permissions";
        
        if (!cache.TryGetValue(cacheKey, out Dictionary<string, List<string>> permissionsDict))
        {
            // Fetch permissions from database
            var roleIds = await context.UserRoles.IgnoreQueryFilters()
                .Where(item => item.UserId == userId)
                .Select(item => item.RoleId)
                .ToListAsync();

            var roleClaims = await context.RoleClaims.IgnoreQueryFilters()
                .Where(item => roleIds.Contains(item.RoleId))
                .ToListAsync();
            
            var allPermissions = roleClaims.Select(rc => rc.ClaimValue)
                .Distinct();
            
            permissionsDict = new Dictionary<string, List<string>>();
            
            foreach (var perm in allPermissions)
            {
                var permissionTypes = await context.PermissionTypes
                    .Where(p => p.Key == perm && roleClaims.Select(rc => rc.Id).Contains(p.RoleClaimId))
                    .Select(p => p.Type)
                    .ToListAsync();

                permissionsDict[perm] = permissionTypes;
            }

            var cacheEntryOptions = new MemoryCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromMinutes(30))
                .SetAbsoluteExpiration(TimeSpan.FromHours(12));

            cache.Set(cacheKey, permissionsDict, cacheEntryOptions);
        }

        // Check if the user has the required permission
        if (permissionsDict.TryGetValue(requirement.Permission, out var value) && value.Count != 0)
        {
            authorizationHandlerContext.Succeed(requirement);
        }
    }
}

