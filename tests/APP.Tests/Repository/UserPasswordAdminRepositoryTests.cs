using APP.IRepository;
using APP.Repository;
using APP.Utils;
using DOMAIN.Entities.ActivityLogs;
using DOMAIN.Entities.Auth;
using DOMAIN.Entities.Roles;
using DOMAIN.Entities.Users;
using INFRASTRUCTURE.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SHARED;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Repository;

public class UserPasswordAdminRepositoryTests
{
    private const string StrongPassword = "NewPass#2026";

    [Theory]
    [InlineData("dkadusei@kumateck.com")]
    [InlineData("DKadusei@Kumateck.com")]
    public async Task PasswordAdminResetsPasswordEndsSessionsAndIsAudited(string adminEmail)
    {
        await using var fixture = new Fixture(adminEmail);
        var target = await fixture.AddUser("target@kumateck.com");
        fixture.Context.RefreshTokens.Add(new RefreshToken { UserId = target.Id, Token = "old-session", Expiry = DateTime.UtcNow.AddDays(1) });
        await fixture.Context.SaveChangesAsync();
        var oldHash = target.PasswordHash;

        var result = await fixture.Repository.ResetUserPassword(
            new AdminResetUserPasswordRequest { UserId = target.Id, NewPassword = StrongPassword }, fixture.Admin.Id);

        Assert.True(result.IsSuccess);
        var updated = await fixture.Manager.FindByIdAsync(target.Id.ToString());
        Assert.NotEqual(oldHash, updated!.PasswordHash);
        Assert.True(await fixture.Manager.CheckPasswordAsync(updated, StrongPassword));
        Assert.All(await fixture.Context.RefreshTokens.Where(t => t.UserId == target.Id).ToListAsync(),
            t => Assert.NotNull(t.DeletedAt));
        var renewal = await new AuthRepository(null!, fixture.Context, fixture.Manager, null!, null!, null!, null!)
            .LoginWithRefreshToken(new LoginWithRefreshToken { RefreshToken = "old-session" });
        Assert.Equal(ErrorType.NotFound, renewal.Errors.Single().Type);

        var entry = Assert.Single(fixture.Audit.Entries);
        Assert.Equal(fixture.Admin.Id, entry.UserId);
        Assert.Contains("target@kumateck.com", entry.Action);
        Assert.DoesNotContain(StrongPassword, entry.Action + entry.Payload + entry.ResponsePayload);
    }

    [Fact]
    public async Task AnyOtherCallerIsForbiddenAndNothingChanges()
    {
        await using var fixture = new Fixture("dkadusei@kumateck.com");
        var intruder = await fixture.AddUser("someone.else@kumateck.com");
        var target = await fixture.AddUser("target@kumateck.com");
        var oldHash = target.PasswordHash;

        var result = await fixture.Repository.ResetUserPassword(
            new AdminResetUserPasswordRequest { UserId = target.Id, NewPassword = StrongPassword }, intruder.Id);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Forbidden, result.Errors.Single().Type);
        Assert.Equal(oldHash, (await fixture.Manager.FindByIdAsync(target.Id.ToString()))!.PasswordHash);
        Assert.Empty(fixture.Audit.Entries);
    }

    [Fact]
    public async Task UnknownOrDisabledCallerIsForbidden()
    {
        await using var fixture = new Fixture("dkadusei@kumateck.com");
        var target = await fixture.AddUser("target@kumateck.com");
        var request = new AdminResetUserPasswordRequest { UserId = target.Id, NewPassword = StrongPassword };

        var unknown = await fixture.Repository.ResetUserPassword(request, Guid.NewGuid());
        Assert.Equal(ErrorType.Forbidden, unknown.Errors.Single().Type);

        fixture.Admin.IsDisabled = true;
        await fixture.Context.SaveChangesAsync();
        var disabled = await fixture.Repository.ResetUserPassword(request, fixture.Admin.Id);
        Assert.Equal(ErrorType.Forbidden, disabled.Errors.Single().Type);
    }

    [Fact]
    public async Task PasswordThatBreaksTheRulesIsRejectedWithEachRule()
    {
        await using var fixture = new Fixture("dkadusei@kumateck.com");
        var target = await fixture.AddUser("target@kumateck.com");
        var oldHash = target.PasswordHash;

        var result = await fixture.Repository.ResetUserPassword(
            new AdminResetUserPasswordRequest { UserId = target.Id, NewPassword = "short" }, fixture.Admin.Id);

        Assert.True(result.IsFailure);
        Assert.All(result.Errors, e => Assert.Equal(ErrorType.Validation, e.Type));
        var codes = result.Errors.Select(e => e.Code).ToList();
        Assert.Contains("UserPassword.PasswordTooShort", codes);
        Assert.Contains("UserPassword.PasswordRequiresDigit", codes);
        Assert.Contains("UserPassword.PasswordRequiresUpper", codes);
        Assert.Contains("UserPassword.PasswordRequiresNonAlphanumeric", codes);
        Assert.Equal(oldHash, (await fixture.Manager.FindByIdAsync(target.Id.ToString()))!.PasswordHash);
        Assert.Empty(fixture.Audit.Entries);
    }

    [Fact]
    public async Task MissingTargetIsNotFound()
    {
        await using var fixture = new Fixture("dkadusei@kumateck.com");

        var result = await fixture.Repository.ResetUserPassword(
            new AdminResetUserPasswordRequest { UserId = Guid.NewGuid(), NewPassword = StrongPassword }, fixture.Admin.Id);

        Assert.Equal(ErrorType.NotFound, result.Errors.Single().Type);
    }

    private sealed class Fixture : IAsyncDisposable
    {
        public ApplicationDbContext Context { get; }
        public UserManager<User> Manager { get; }
        public RecordingActivityLog Audit { get; } = new();
        public UserPasswordAdminRepository Repository { get; }
        public User Admin { get; }

        public Fixture(string adminEmail)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
            Context = new ApplicationDbContext(options, new AnonymousUser());

            // Same password rules as API/Program.cs.
            var identity = new IdentityOptions();
            identity.Password.RequireDigit = true;
            identity.Password.RequiredLength = 8;
            identity.Password.RequireNonAlphanumeric = true;
            identity.Password.RequireUppercase = true;
            identity.Password.RequireLowercase = false;

            Manager = new UserManager<User>(
                new UserStore<User, Role, ApplicationDbContext, Guid>(Context),
                Options.Create(identity), new PasswordHasher<User>(), [],
                [new PasswordValidator<User>()], new UpperInvariantLookupNormalizer(),
                new IdentityErrorDescriber(), null, NullLogger<UserManager<User>>.Instance);
            Manager.RegisterTokenProvider(TokenOptions.DefaultProvider, new StaticTokenProvider());

            Repository = new UserPasswordAdminRepository(Context, Manager, Audit,
                NullLogger<UserPasswordAdminRepository>.Instance);
            Admin = AddUser(adminEmail).GetAwaiter().GetResult();
        }

        public async Task<User> AddUser(string email)
        {
            var user = new User
            {
                Id = Guid.NewGuid(), UserName = email, Email = email, FirstName = "Test", LastName = "User",
                Title = "", Avatar = "", Signature = "", SecurityStamp = Guid.NewGuid().ToString()
            };
            user.PasswordHash = Manager.PasswordHasher.HashPassword(user, "OldPass#2025");
            Context.Users.Add(user);
            await Context.SaveChangesAsync();
            return user;
        }

        public async ValueTask DisposeAsync()
        {
            Manager.Dispose();
            await Context.DisposeAsync();
        }
    }

    private sealed class StaticTokenProvider : IUserTwoFactorTokenProvider<User>
    {
        public Task<string> GenerateAsync(string purpose, UserManager<User> manager, User user) =>
            Task.FromResult($"{purpose}:{user.Id}");

        public Task<bool> ValidateAsync(string purpose, string token, UserManager<User> manager, User user) =>
            Task.FromResult(token == $"{purpose}:{user.Id}");

        public Task<bool> CanGenerateTwoFactorTokenAsync(UserManager<User> manager, User user) => Task.FromResult(false);
    }

    private sealed class RecordingActivityLog : IActivityLogRepository
    {
        public List<CreateActivityLog> Entries { get; } = [];

        public Task RecordActivityAsync(CreateActivityLog model)
        {
            Entries.Add(model);
            return Task.CompletedTask;
        }

        public void RecordActivity(CreateActivityLog model) => Entries.Add(model);

        public Task<Paginateable<IEnumerable<ActivityLogDto>>> GetActivityLogs(ActivityLogFilter filter) =>
            throw new NotSupportedException();
    }

    private sealed class AnonymousUser : ICurrentUserService
    {
        public Guid? UserId => null;
        public Guid? DepartmentId => null;
        public string DepartmentType => string.Empty;
    }
}
