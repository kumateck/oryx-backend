using System.Text.RegularExpressions;
using Xunit;

namespace APP.Tests.Utils;

// Customer servers start the API with docker-compose.yml (plus the formula overlay). A setting the
// code reads but compose does not list never reaches the container, even when the server's .env has
// it. That is how forgot-password broke: SMTP_HOST was in .env but not in compose.
public class SettingsContractTests
{
    // Settings that are deliberately not passed through compose.
    private static readonly Dictionary<string, string> NotFromCompose = new()
    {
        ["Environment"] = "unset on customer servers on purpose; it selects demo/dev behaviour",
        ["ORYX_RELEASE"] = "baked into the image by deploy.Dockerfile",
        ["FORMULA_SERVICE_AUTH_SECRET"] = "the formula overlay passes FORMULA_SERVICE_AUTH_SECRET_FILE instead",
    };

    // Direct reads, plus helper lookups of upper-case names such as Get("SMTP_HOST") in SmtpSettings.
    private static readonly Regex SettingReads = new(
        "GetEnvironmentVariable\\(\"([A-Za-z0-9_]+)\"\\)|\\b(?:Get|env)\\(\"([A-Z][A-Z0-9]*_[A-Z0-9_]+)\"\\)");

    [Fact]
    public void Every_setting_the_code_reads_is_passed_by_compose()
    {
        var root = RepoRoot();
        var read = Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
            .Where(path => !IsExcluded(Path.GetRelativePath(root, path)))
            .SelectMany(path => SettingReads.Matches(File.ReadAllText(path)))
            .Select(match => match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Value)
            .ToHashSet();

        var passed = new[] { "docker-compose.yml", "docker-compose.formula.yml" }
            .SelectMany(file => File.ReadAllLines(Path.Combine(root, file)))
            .Select(line => Regex.Match(line, "^\\s+- ([A-Za-z0-9_]+)="))
            .Where(match => match.Success)
            .Select(match => match.Groups[1].Value)
            .ToHashSet();

        var missing = read.Except(passed).Except(NotFromCompose.Keys).Order().ToList();

        Assert.True(missing.Count == 0,
            "Add these to the app service in docker-compose.yml (use ${NAME:-} when optional): " + string.Join(", ", missing));
    }

    [Fact]
    public void Compose_passes_every_smtp_setting_to_the_api_container()
    {
        var lines = File.ReadAllLines(Path.Combine(RepoRoot(), "docker-compose.yml"));

        foreach (var name in new[] { "SMTP_HOST", "SMTP_PORT", "SMTP_USERNAME", "SMTP_PASSWORD", "SMTP_FROM" })
            Assert.Contains(lines, line => line.Trim().StartsWith($"- {name}=${{{name}"));
    }

    private static bool IsExcluded(string relative)
    {
        var parts = relative.Split(Path.DirectorySeparatorChar);
        return parts.Any(part => part is "bin" or "obj" or "tests" or "TOOLS");
    }

    private static string RepoRoot()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir != null; dir = dir.Parent)
            if (File.Exists(Path.Combine(dir.FullName, "docker-compose.yml")) && File.Exists(Path.Combine(dir.FullName, "Oryx.sln")))
                return dir.FullName;
        throw new DirectoryNotFoundException("repository root");
    }
}
