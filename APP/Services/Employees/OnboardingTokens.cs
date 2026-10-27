using System.Security.Cryptography;
using System.Text;

namespace APP.Services.Employees;

/// <summary>Random single-use onboarding tokens; only their SHA-256 hash is stored.</summary>
public static class OnboardingTokens
{
    public static string Create() => Base64Url(RandomNumberGenerator.GetBytes(32));

    public static string Hash(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token ?? string.Empty))).ToLowerInvariant();

    /// <summary>A strong random password for an account whose owner sets their own from the emailed link.</summary>
    public static string TemporaryPassword() => $"{Base64Url(RandomNumberGenerator.GetBytes(24))}aA1!";

    private static string Base64Url(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
