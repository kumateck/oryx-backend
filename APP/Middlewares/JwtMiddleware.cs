using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using DOMAIN.Context;

namespace APP.Middlewares;

public class JwtMiddleware(RequestDelegate next)
{
    public async Task Invoke(HttpContext context,OryxContext db)
    {
        var token = context.Request.Headers.Authorization.FirstOrDefault()?.Split(" ").Last();

        if (!string.IsNullOrWhiteSpace(token))
            await AttachUserToContext(context, token, db);

        await next(context);
    }

    private static async Task AttachUserToContext(HttpContext context, string token, OryxContext db)
    {
        try
        {
            if (token != null)
            {
                var jwtToken = new JwtSecurityToken(token);
                var user = await db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(item => item.Id == Guid.Parse(jwtToken.Subject));

                var roles = jwtToken.Claims
                    .Where(claim => claim.Type == "role")
                    .Select(claim => claim.Value)
                    .ToList();

                if (user != null)
                {
                    context.Items["Sub"] = jwtToken.Subject;
                    context.Items["Roles"] = roles;
                }
            }
        }
        catch (Exception)
        {
            //do nothing
        }
    }
}