#nullable enable

namespace APP.Services.Formulas;

public sealed record FormulaCalculationSettings(
    bool Enabled,
    Uri BaseUri,
    string WorkloadToken,
    TimeSpan Timeout)
{
    public static FormulaCalculationSettings Load()
    {
        var enabled = string.Equals(
            Environment.GetEnvironmentVariable("FORMULA_RUNTIME_ENABLED"),
            "true",
            StringComparison.OrdinalIgnoreCase);
        var rawBaseUrl = Environment.GetEnvironmentVariable("FORMULA_SERVICE_BASE_URL")
            ?? "http://formula-calculation:3101/";
        if (!Uri.TryCreate(rawBaseUrl, UriKind.Absolute, out var baseUri) ||
            (baseUri.Scheme != Uri.UriSchemeHttp && baseUri.Scheme != Uri.UriSchemeHttps) ||
            !string.IsNullOrEmpty(baseUri.UserInfo) || !string.IsNullOrEmpty(baseUri.Query) ||
            !string.IsNullOrEmpty(baseUri.Fragment))
            throw new InvalidOperationException("FORMULA_SERVICE_BASE_URL is invalid.");

        var timeoutMs = ParseBoundedInteger(
            Environment.GetEnvironmentVariable("FORMULA_SERVICE_CLIENT_TIMEOUT_MS"),
            2500, 100, 10000, "FORMULA_SERVICE_CLIENT_TIMEOUT_MS");
        var token = enabled ? LoadToken() : "";
        return new FormulaCalculationSettings(
            enabled, baseUri, token, TimeSpan.FromMilliseconds(timeoutMs));
    }

    private static string LoadToken()
    {
        var secretFile = Environment.GetEnvironmentVariable("FORMULA_SERVICE_AUTH_SECRET_FILE");
        var token = string.IsNullOrWhiteSpace(secretFile)
            ? Environment.GetEnvironmentVariable("FORMULA_SERVICE_AUTH_SECRET") ?? ""
            : File.ReadAllText(secretFile).Trim();
        if (token.Length is < 32 or > 512)
            throw new InvalidOperationException("The formula workload secret is invalid.");
        return token;
    }

    private static int ParseBoundedInteger(
        string? raw, int fallback, int minimum, int maximum, string name)
    {
        var value = raw is null ? fallback : int.TryParse(raw, out var parsed) ? parsed : -1;
        if (value < minimum || value > maximum)
            throw new InvalidOperationException($"{name} is outside its permitted range.");
        return value;
    }
}
