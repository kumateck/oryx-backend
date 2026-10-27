using APP.Repository;
using APP.Services.Token;
using DOMAIN.Entities.Auth;
using DOMAIN.Entities.Users;
using DOMAIN.Entities.Roles;
using INFRASTRUCTURE.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Repository;

public class RefreshTokenOwnershipTests
{
    [Fact]
    public async Task AnonymousSignInAndLaterRenewalKeepTheActualTokenOwner()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var ownerId = Guid.NewGuid();
        string issuedToken;
        await using (var issuing = new ApplicationDbContext(options, new AnonymousUser()))
        {
            issuing.Users.Add(new User
            {
                Id = ownerId, UserName = "session-test", Email = "test@example.invalid",
                FirstName = "Session", LastName = "Test", Title = "", Avatar = "", Signature = "",
                SecurityStamp = Guid.NewGuid().ToString()
            });
            await issuing.SaveChangesAsync();
            using var manager = Manager(issuing);
            var jwt = new JwtService(issuing, Configuration(), manager);
            var result = await jwt.AuthenticateById(ownerId, "web");
            Assert.True(result.IsSuccess);
            issuedToken = result.Value.RefreshToken;
            var stored = await issuing.RefreshTokens.SingleAsync(t => t.Token == issuedToken);
            Assert.Equal(ownerId, stored.UserId);
            Assert.Null(stored.CreatedById); // Audit identity is anonymous; it is not token ownership.
        }

        // A fresh context models the next app process/request: no tracked user or token can mask the defect.
        await using var renewed = new ApplicationDbContext(options, new AnonymousUser());
        using var nextManager = Manager(renewed);
        var nextJwt = new JwtService(renewed, Configuration(), nextManager);
        var repository = new AuthRepository(null!, renewed, nextManager, nextJwt, null!, null!, null!);
        var exchange = await repository.LoginWithRefreshToken(new LoginWithRefreshToken { RefreshToken = issuedToken });
        Assert.True(exchange.IsSuccess);
        Assert.Equal(ownerId, exchange.Value.UserId);
        Assert.NotEmpty(exchange.Value.AccessToken);
        var replacement = await renewed.RefreshTokens.SingleAsync(t => t.Token == exchange.Value.RefreshToken);
        Assert.Equal(ownerId, replacement.UserId);
        Assert.NotEqual(issuedToken, replacement.Token);
    }

    private static IConfiguration Configuration() => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["JwtSettings:Key"] = "test-only-signing-key-at-least-thirty-two-bytes-long"
        }).Build();

    private static UserManager<User> Manager(ApplicationDbContext context) => new(
        new UserStore<User, Role, ApplicationDbContext, Guid>(context),
        Options.Create(new IdentityOptions()), new PasswordHasher<User>(), [], [],
        new UpperInvariantLookupNormalizer(), new IdentityErrorDescriber(), null,
        NullLogger<UserManager<User>>.Instance);

    private sealed class AnonymousUser : ICurrentUserService
    {
        public Guid? UserId => null;
        public Guid? DepartmentId => null;
        public string DepartmentType => string.Empty;
    }
}
