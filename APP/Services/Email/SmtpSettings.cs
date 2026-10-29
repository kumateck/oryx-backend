namespace APP.Services.Email;

public record SmtpSettings(string Host, int Port, string Username, string Password, string Sender)
{
    public static SmtpSettings FromEnvironment() => Read(Environment.GetEnvironmentVariable);

    public static bool IsConfigured(Func<string, string?> env) => Missing(env).Count == 0;

    private static List<string> Missing(Func<string, string?> env) =>
        new[] { "SMTP_HOST", "SMTP_USERNAME", "SMTP_PASSWORD" }.Where(name => string.IsNullOrWhiteSpace(env(name))).ToList();

    // Blank values count as unset: docker compose passes a setting missing from .env as "".
    public static SmtpSettings Read(Func<string, string?> env)
    {
        string? Get(string name) => string.IsNullOrWhiteSpace(env(name)) ? null : env(name)!.Trim();

        var host = Get("SMTP_HOST");
        var username = Get("SMTP_USERNAME");
        var password = Get("SMTP_PASSWORD");
        var sender = Get("SMTP_FROM") ?? username;

        var missing = Missing(env);
        if (missing.Count > 0)
            throw new InvalidOperationException(
                $"Email is not configured: {string.Join(", ", missing)} must be set in the API container before sending email");

        var port = int.TryParse(Get("SMTP_PORT"), out var parsed) ? parsed : 587;
        return new SmtpSettings(host!, port, username!, password!, sender!);
    }
}
