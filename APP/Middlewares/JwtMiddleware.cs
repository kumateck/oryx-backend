using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using INFRASTRUCTURE.Context;
using Microsoft.Extensions.Caching.Memory;

namespace APP.Middlewares;

public class JwtMiddleware(RequestDelegate next, IMemoryCache cache)
{
    public async Task Invoke(HttpContext context, ApplicationDbContext db)
    {
        var token = context.Request.Headers.Authorization.FirstOrDefault()?.Split(" ").Last();

        if (!string.IsNullOrWhiteSpace(token))
            await AttachUserToContext(context, token, db);

        await next(context);
    }

    private async Task AttachUserToContext(HttpContext context, string token, ApplicationDbContext db)
    {
        try
        {
            if (!cache.TryGetValue(token, out (string UserId, List<Guid> RoleIds) cachedData))
            {
                // Parse the token to extract user and roles information
                var jwtToken = new JwtSecurityToken(token);
                var userId = jwtToken.Subject ?? context.User.FindFirstValue(ClaimTypes.NameIdentifier);
                var user = await db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(item => item.Id == Guid.Parse(userId));

                var roles = jwtToken.Claims
                    .Where(claim => claim.Type == "role")
                    .Select(claim => claim.Value)
                    .ToList();

                var roleIds = await db.Roles
                    .Where(role => roles.Contains(role.Name))
                    .Select(r => r.Id)
                    .ToListAsync();

                // Cache the user and role information if the user exists
                if (user != null)
                {
                    cachedData = (UserId: userId, RoleIds: roleIds);
                    cache.Set(token, cachedData, new MemoryCacheEntryOptions
                    {
                        SlidingExpiration = TimeSpan.FromMinutes(5)
                    });
                }
            }

            // Attach cached user data to context
            if (cachedData.UserId != null)
            {
                context.Items["Sub"] = cachedData.UserId;
                context.Items["Roles"] = cachedData.RoleIds;
            }
        }
        catch (Exception)
        {
            // Handle or log the exception as needed
        }
    }
}
