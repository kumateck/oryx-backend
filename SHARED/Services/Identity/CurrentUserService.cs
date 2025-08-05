using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace SHARED.Services.Identity;

public class CurrentUserService : ICurrentUserService
{
    public CurrentUserService(IHttpContextAccessor httpContextAccessor, IConfiguration config)
    {
        var context = httpContextAccessor.HttpContext;
        var authHeader = context?.Request.Headers["Authorization"].ToString();
        var environment = Environment.GetEnvironmentVariable("Environment");

        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
        {
            var token = authHeader.Substring("Bearer ".Length).Trim();
            var jwtSecret = config["JwtSettings:Key"];
            var principal = ValidateToken(token, jwtSecret);
            var tokenEnv = principal.FindFirst("environment")?.Value;
            var departmentType = principal.FindFirst("departmentType")?.Value;

            if (environment != tokenEnv)
            {
                return;
            }

            var userIdString = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (Guid.TryParse(userIdString, out var userId))
                UserId = userId;

            var departmentIdString = principal.FindFirst("department")?.Value;
            if (Guid.TryParse(departmentIdString, out var departmentId))
                DepartmentId = departmentId;
            
            DepartmentType = departmentType;
        }
    }

    private ClaimsPrincipal ValidateToken(string token, string secret)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(secret);
            var parameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = false // optional for debug
            };

            return tokenHandler.ValidateToken(token, parameters, out _);
        }
        catch
        {
            return null;
        }
    }

    public Guid? UserId { get; }

    public Guid? DepartmentId { get; }
    public string DepartmentType { get; }
}
