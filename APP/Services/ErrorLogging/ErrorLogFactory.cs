using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using DOMAIN.Entities.ErrorLogs;
using Microsoft.Extensions.Logging;

namespace APP.Services.ErrorLogging;

public static partial class ErrorLogFactory
{
    private const int MaxMessageChars = 8_000;
    private const int MaxExceptionChars = 64_000;
    private static readonly string[] OwnNamespaces = ["APP.", "API.", "INFRASTRUCTURE.", "DOMAIN.", "SHARED."];

    public static ErrorLog Create(
        string category, LogLevel level, string message, Exception exception, string messageTemplate,
        string instance, string release)
    {
        var innermost = Innermost(exception);
        return new ErrorLog
        {
            Id = Guid.NewGuid(),
            OccurredAt = DateTime.UtcNow,
            Level = level.ToString(),
            Source = "Background",
            Category = Truncate(category, 256),
            Message = Truncate(string.IsNullOrWhiteSpace(message) ? exception?.Message : message, MaxMessageChars),
            ExceptionType = Truncate(innermost?.GetType().FullName, 512),
            ExceptionDetails = Truncate(exception?.ToString(), MaxExceptionChars),
            Fingerprint = Fingerprint(category, messageTemplate ?? message, innermost),
            Instance = Truncate(instance, 128),
            Release = Truncate(release, 64),
            MachineName = Truncate(Environment.MachineName, 128),
        };
    }

    /// <summary>
    /// Same failure → same fingerprint across requests and builds: the root exception type plus the
    /// first frame in our own code, or the logger category plus message template when there is no exception.
    /// </summary>
    public static string Fingerprint(string category, string messageTemplate, Exception innermost)
    {
        var key = innermost is null
            ? $"{category}|{messageTemplate}"
            : $"{innermost.GetType().FullName}|{TopFrame(innermost)}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(key));
        return Convert.ToHexString(hash, 0, 8).ToLowerInvariant();
    }

    public static Exception Innermost(Exception exception)
    {
        var current = exception;
        while (current is not null)
        {
            var next = current is AggregateException { InnerExceptions.Count: > 0 } aggregate
                ? aggregate.InnerExceptions[0]
                : current.InnerException;
            if (next is null) return current;
            current = next;
        }
        return null;
    }

    private static string TopFrame(Exception exception)
    {
        var methods = (new StackTrace(exception, false).GetFrames() ?? [])
            .Select(f => f.GetMethod())
            .Where(m => m?.DeclaringType is not null)
            .Select(m => CompilerSuffix().Replace($"{m.DeclaringType.FullName}.{m.Name}", ""))
            .ToList();
        return methods.FirstOrDefault(m => OwnNamespaces.Any(m.StartsWith)) ?? methods.FirstOrDefault() ?? "";
    }

    public static string Truncate(string value, int max) =>
        value is null || value.Length <= max ? value : value[..max];

    // Async state machine names ("<Save>d__12") shift between builds; keep only the method name.
    [GeneratedRegex(@"d__\d+")]
    private static partial Regex CompilerSuffix();
}
