using APP.Extensions;
using APP.IRepository;
using DOMAIN.Entities.ActivityLogs;
using DOMAIN.Entities.Auth;
using DOMAIN.Entities.Users;
using INFRASTRUCTURE.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SHARED;

namespace APP.Repository;

public class UserPasswordAdminRepository(
    ApplicationDbContext context,
    UserManager<User> userManager,
    IActivityLogRepository activityLog,
    ILogger<UserPasswordAdminRepository> logger) : IUserPasswordAdminRepository
{
    /// <summary>The only account allowed to set other users' passwords.</summary>
    public const string PasswordAdminEmail = "dkadusei@kumateck.com";

    public static Error NotPasswordAdmin =>
        Error.Forbidden("UserPassword.Forbidden", "You are not allowed to change other users' passwords.");

    public static bool IsPasswordAdmin(User user) =>
        user is { IsDisabled: false } && !user.IsDeleted() &&
        string.Equals(user.Email, PasswordAdminEmail, StringComparison.OrdinalIgnoreCase);

    public async Task<Result> ResetUserPassword(AdminResetUserPasswordRequest request, Guid callerId)
    {
        // Load the caller from the database: the email claim in the token is not trusted.
        var caller = await context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == callerId);
        if (caller is null || !IsPasswordAdmin(caller)) return NotPasswordAdmin;

        if (string.IsNullOrWhiteSpace(request.NewPassword))
            return Error.Validation("UserPassword.Required", "A new password is required.");

        var target = await userManager.FindByIdAsync(request.UserId.ToString());
        if (target is null || target.IsDeleted()) return UserErrors.NotFound(request.UserId);

        // A reset token plus ResetPasswordAsync runs the configured password validators
        // and rotates the security stamp.
        var token = await userManager.GeneratePasswordResetTokenAsync(target);
        var result = await userManager.ResetPasswordAsync(target, token, request.NewPassword);
        if (!result.Succeeded)
            return result.Errors
                .Select(e => Error.Validation($"UserPassword.{e.Code}", e.Description))
                .ToList();

        // End the target's existing sessions. Removal is a soft delete (DeletedAt), which
        // AuthRepository.LoginWithRefreshToken refuses. Access tokens already issued stay
        // valid until they expire.
        var sessions = await context.RefreshTokens
            .Where(t => t.UserId == target.Id && !t.DeletedAt.HasValue).ToListAsync();
        if (sessions.Count != 0)
        {
            context.RefreshTokens.RemoveRange(sessions);
            await context.SaveChangesAsync();
        }

        await RecordAudit(caller, target, sessions.Count);
        return Result.Success();
    }

    private async Task RecordAudit(User caller, User target, int endedSessions)
    {
        // Never include the password here.
        logger.LogWarning(
            "Password for user {TargetUserId} ({TargetEmail}) was reset by {CallerUserId} ({CallerEmail}); {EndedSessions} sessions ended",
            target.Id, target.Email, caller.Id, caller.Email, endedSessions);

        try
        {
            await activityLog.RecordActivityAsync(new CreateActivityLog
            {
                UserId = caller.Id,
                Action = $"Reset password for {target.Email} ({target.Id})",
                Module = "Settings",
                SubModule = "User Password",
                ActionType = ActionType.Update,
                HttpMethod = "POST",
                StatusCode = 200,
                Payload = $"{{\"userId\":\"{target.Id}\"}}",
                CreatedAt = DateTime.UtcNow
            });
        }
        catch (Exception e)
        {
            // The password has already changed; a logging outage must not report failure.
            logger.LogError(e, "Could not record the activity log entry for password reset of {TargetUserId}", target.Id);
        }
    }
}
