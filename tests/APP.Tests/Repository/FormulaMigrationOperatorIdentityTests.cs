using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FormulaMigration;
using APP.Utils;
using DOMAIN.Entities.Permissions;
using DOMAIN.Entities.Roles;
using DOMAIN.Entities.Users;
using INFRASTRUCTURE.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace APP.Tests.Repository;

public class FormulaMigrationOperatorIdentityTests
{
    private const string Key =
        "formula-migration-test-signing-key-that-is-long-enough-for-hs256";
    private static readonly Guid UserId =
        Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");

    [Fact]
    public void Authenticate_ReturnsIdentityForValidEnvironmentMatchedToken()
    {
        var token = Token("validation", DateTime.UtcNow.AddMinutes(5));

        var actual = FormulaMigrationOperatorIdentity.Authenticate(
            token, Key, "validation");

        Assert.Equal(UserId, actual);
    }

    [Theory]
    [InlineData("production", false)]
    [InlineData("validation", true)]
    public void Authenticate_RejectsWrongEnvironmentAndExpiredTokens(
        string expectedEnvironment, bool expired)
    {
        var expiry = expired
            ? DateTime.UtcNow.AddMinutes(-5)
            : DateTime.UtcNow.AddMinutes(5);
        var token = Token("validation", expiry);

        Assert.ThrowsAny<SecurityTokenException>(() =>
            FormulaMigrationOperatorIdentity.Authenticate(
                token, Key, expectedEnvironment));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task DatabaseAuthorization_RequiresDedicatedPermission(bool grantPermission)
    {
        await using var context = Context();
        var roleId = Guid.NewGuid();
        context.Users.Add(new User
        {
            Id = UserId,
            UserName = "formula-operator",
            FirstName = "Formula",
            LastName = "Operator",
            Title = string.Empty,
            Avatar = string.Empty,
            Signature = string.Empty
        });
        context.Roles.Add(new Role
        {
            Id = roleId,
            Name = "FormulaMigrationOperator",
            DisplayName = "Formula migration operator"
        });
        context.UserRoles.Add(new IdentityUserRole<Guid>
            { UserId = UserId, RoleId = roleId });
        var claim = new IdentityRoleClaim<Guid>
        {
            RoleId = roleId,
            ClaimType = "Permission",
            ClaimValue = grantPermission
                ? PermissionKeys.CanApplyFormulaMigration
                : PermissionKeys.CanViewQuestions
        };
        context.RoleClaims.Add(claim);
        await context.SaveChangesAsync();
        context.PermissionTypes.Add(new PermissionType
        {
            Id = Guid.NewGuid(),
            RoleClaimId = claim.Id,
            Key = claim.ClaimValue!,
            Type = "Allow"
        });
        await context.SaveChangesAsync();

        var action = () => FormulaMigrationDatabase.RequireAuthorizedOperatorAsync(
            context, UserId, CancellationToken.None);
        if (grantPermission)
            await action();
        else
            await Assert.ThrowsAsync<UnauthorizedAccessException>(action);
    }

    private static string Token(string environment, DateTime expires)
    {
        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, UserId.ToString()),
                new Claim("environment", environment)
            ]),
            NotBefore = expires.AddMinutes(-10),
            IssuedAt = expires.AddMinutes(-10),
            Expires = expires,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.ASCII.GetBytes(Key)),
                SecurityAlgorithms.HmacSha256)
        };
        var handler = new JwtSecurityTokenHandler();
        return handler.WriteToken(handler.CreateToken(descriptor));
    }

    private static ApplicationDbContext Context() => new(
        new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options,
        new ApplyTestUser(UserId));
}
