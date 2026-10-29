using APP.Extensions;
using APP.Repository;
using APP.Services.Email;
using DOMAIN.Entities.Auth;
using DOMAIN.Entities.Notifications;
using DOMAIN.Entities.Roles;
using DOMAIN.Entities.Users;
using INFRASTRUCTURE.Context;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SHARED;
using SHARED.Services.Identity;
using Xunit;
using ForgotPasswordRequest = DOMAIN.Entities.Auth.ForgotPasswordRequest;

namespace APP.Tests.Repository;

// Entrance Production Server, 2026-10-29: SMTP_HOST was missing from the API container, so
// POST /auth/forgot-password threw and the user got a bare HTTP 500.
public class ForgotPasswordEmailNotConfiguredTests
{
    private const string KnownEmail = "warehouse@example.invalid";

    [Fact]
    public async Task Known_account_on_a_server_without_email_gets_a_clear_503()
    {
        await using var context = Context();
        var email = new FakeEmail(configured: false);

        var result = await Repository(context, email).ForgotPassword(new ForgotPasswordRequest { Email = KnownEmail });

        AssertEmailNotConfigured(result);
        Assert.Empty(email.Sent);
        Assert.Empty(context.PasswordResets); // No reset link that was never delivered.
    }

    [Fact]
    public async Task Unknown_account_gets_the_same_503_so_the_answer_does_not_reveal_accounts()
    {
        await using var context = Context();

        var result = await Repository(context, new FakeEmail(configured: false))
            .ForgotPassword(new ForgotPasswordRequest { Email = "nobody@example.invalid" });

        AssertEmailNotConfigured(result);
    }

    [Fact]
    public async Task Configured_server_still_sends_the_reset_link()
    {
        await using var context = Context();
        var email = new FakeEmail(configured: true);

        var result = await Repository(context, email).ForgotPassword(new ForgotPasswordRequest { Email = KnownEmail });

        Assert.True(result.IsSuccess);
        Assert.Equal(KnownEmail, Assert.Single(email.Sent));
        Assert.Single(context.PasswordResets);
    }

    [Fact]
    public void Smtp_settings_report_whether_email_can_be_sent()
    {
        Assert.False(SmtpSettings.IsConfigured(_ => null));
        Assert.False(SmtpSettings.IsConfigured(name => name == "SMTP_HOST" ? " " : "set"));
        Assert.True(SmtpSettings.IsConfigured(_ => "set"));
    }

    private static void AssertEmailNotConfigured(Result result)
    {
        Assert.False(result.IsSuccess);
        Assert.Equal("Email.NotConfigured", result.Error.Code);
        Assert.Contains("contact your administrator", result.Error.Description);
        var http = Assert.IsAssignableFrom<IStatusCodeHttpResult>(result.ToProblemDetails());
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, http.StatusCode);
    }

    private static ApplicationDbContext Context()
    {
        var context = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options,
            new AnonymousUser());
        context.Users.Add(new User
        {
            Id = Guid.NewGuid(), UserName = KnownEmail, NormalizedUserName = KnownEmail.ToUpperInvariant(),
            Email = KnownEmail, NormalizedEmail = KnownEmail.ToUpperInvariant(),
            FirstName = "Warehouse", LastName = "User", Title = "", Avatar = "", Signature = "",
            SecurityStamp = Guid.NewGuid().ToString()
        });
        context.SaveChanges();
        return context;
    }

    private static AuthRepository Repository(ApplicationDbContext context, IEmailService email)
    {
        var manager = new UserManager<User>(
            new UserStore<User, Role, ApplicationDbContext, Guid>(context),
            Options.Create(new IdentityOptions()), new PasswordHasher<User>(), [], [],
            new UpperInvariantLookupNormalizer(), new IdentityErrorDescriber(), null,
            NullLogger<UserManager<User>>.Instance);
        manager.RegisterTokenProvider(TokenOptions.DefaultProvider, new FixedTokenProvider());
        return new AuthRepository(email, context, manager, null!, null!, null!, new HttpContextAccessor());
    }

    // Behaves like EmailService: on a server without SMTP settings, SendMail throws.
    private sealed class FakeEmail(bool configured) : IEmailService
    {
        public List<string> Sent { get; } = [];
        public bool IsConfigured => configured;

        public void SendMail(string name, string to, string subject, string body,
            List<(byte[] fileContent, string fileName, string fileType)> attachments)
        {
            if (!configured) SmtpSettings.Read(_ => null);
            Sent.Add(to);
        }

        public void ProcessNotificationData(NotificationDto data) { }
    }

    private sealed class FixedTokenProvider : IUserTwoFactorTokenProvider<User>
    {
        public Task<string> GenerateAsync(string purpose, UserManager<User> manager, User user) => Task.FromResult("token");
        public Task<bool> ValidateAsync(string purpose, string token, UserManager<User> manager, User user) => Task.FromResult(token == "token");
        public Task<bool> CanGenerateTwoFactorTokenAsync(UserManager<User> manager, User user) => Task.FromResult(false);
    }

    private sealed class AnonymousUser : ICurrentUserService
    {
        public Guid? UserId => null;
        public Guid? DepartmentId => null;
        public string DepartmentType => string.Empty;
    }
}
