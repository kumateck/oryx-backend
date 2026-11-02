using SHARED;

namespace DOMAIN.Entities.Auth;

public static class AuthErrors
{
    public static Error NotFound =>
        Error.NotFound("Token.NotFound", $"The reset token was not found");

    public static Error EmailNotConfigured =>
        Error.Unavailable("Email.NotConfigured",
            "Password reset email isn't set up on this server; contact your administrator.");

    public static Error EmailUnavailable =>
        Error.Unavailable("Email.Unavailable",
            "Password reset email could not be sent. Please try again later or contact your administrator.");

    public static Error TokenExpired =>
        Error.NotFound("Token.Expired", $"The reset token has expired. Kindly request a new one");
}
