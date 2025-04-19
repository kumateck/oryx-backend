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
        
        if (!cache.TryGetValue(cacheKey, out List<string> permissions))
        {
            // Fetch permissions from database
            var roleIds = await context.UserRoles.IgnoreQueryFilters()
                .Where(item => item.UserId == userId)
                .Select(item => item.RoleId)
                .ToListAsync();

            var roleClaims = await context.RoleClaims.IgnoreQueryFilters()
                .Where(item => roleIds.Contains(item.RoleId))
                .ToListAsync();
            
            permissions = roleClaims.Select(rc => rc.ClaimValue)
                .Distinct().ToList();

            var cacheEntryOptions = new MemoryCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromMinutes(30))
                .SetAbsoluteExpiration(TimeSpan.FromHours(12));

            cache.Set(cacheKey, permissions, cacheEntryOptions);
        }

        // Check if the user has the required permission
        if (permissions.Contains(requirement.Permission))
        {
            authorizationHandlerContext.Succeed(requirement);
        }
    }
}

