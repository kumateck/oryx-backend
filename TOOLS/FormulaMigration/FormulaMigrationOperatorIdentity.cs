using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace FormulaMigration;

internal static class FormulaMigrationOperatorIdentity
{
    private const string TokenVariable = "ORYX_FORMULA_OPERATOR_TOKEN";
    private const string KeyVariable = "ORYX_FORMULA_JWT_KEY";
    private const string EnvironmentVariable = "ORYX_FORMULA_ENVIRONMENT";

    public static Guid Authenticate()
    {
        return Authenticate(
            RequireEnvironment(TokenVariable),
            RequireEnvironment(KeyVariable),
            RequireEnvironment(EnvironmentVariable));
    }

    internal static Guid Authenticate(string token, string key,
        string expectedEnvironment)
    {
        var parameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(key)),
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
            NameClaimType = ClaimTypes.NameIdentifier,
            RoleClaimType = ClaimTypes.Role
        };
        var principal = new JwtSecurityTokenHandler().ValidateToken(
            token, parameters, out var validatedToken);
        if (validatedToken is not JwtSecurityToken jwt ||
            !string.Equals(jwt.Header.Alg, SecurityAlgorithms.HmacSha256,
                StringComparison.Ordinal))
            throw new SecurityTokenValidationException("Operator token algorithm is invalid.");
        if (!string.Equals(principal.FindFirst("environment")?.Value,
                expectedEnvironment, StringComparison.Ordinal))
            throw new SecurityTokenValidationException(
                "Operator token environment does not match the target environment.");
        return Guid.TryParse(principal.FindFirst(ClaimTypes.NameIdentifier)?.Value,
            out var actorId)
            ? actorId
            : throw new SecurityTokenValidationException(
                "Operator token has no valid user identity.");
    }

    private static string RequireEnvironment(string name) =>
        Environment.GetEnvironmentVariable(name) is { Length: > 0 } value
            ? value
            : throw new InvalidOperationException($"Set {name} for write commands.");
}
