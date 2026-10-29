using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using DOMAIN.Entities.ErrorLogs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace APP.Services.ErrorLogging;

/// <summary>
/// Collects the errors logged while one request runs, so they are written once, together with
/// the request details, when the request ends. Kept in HttpContext.Items.
/// </summary>
public sealed class ErrorLogRequestContext(string traceId)
{
    public const string ItemKey = "Oryx.ErrorLog.Request";

    private readonly object _gate = new();
    private readonly List<(ErrorLog Entry, Exception Exception)> _entries = [];
    private bool _drained;

    public string TraceId { get; } = traceId;

    /// <summary>
    /// Adds an entry unless the same exception was already recorded (EF and the exception
    /// middleware both log one failed query). When a later exception wraps an earlier one,
    /// the wrapper replaces it because its text includes the inner exception.
    /// Returns false once the request has been flushed.
    /// </summary>
    public bool TryAdd(ErrorLog entry, Exception exception)
    {
        lock (_gate)
        {
            if (_drained) return false;
            if (exception is not null)
            {
                for (var i = 0; i < _entries.Count; i++)
                {
                    var existing = _entries[i].Exception;
                    if (existing is null) continue;
                    if (Chain(existing).Contains(exception)) return true;
                    if (Chain(exception).Contains(existing))
                    {
                        _entries[i] = (entry, exception);
                        return true;
                    }
                }
            }
            _entries.Add((entry, exception));
            return true;
        }
    }

    public List<(ErrorLog Entry, Exception Exception)> Drain()
    {
        lock (_gate)
        {
            _drained = true;
            return [.. _entries];
        }
    }

    private static IEnumerable<Exception> Chain(Exception exception)
    {
        for (var e = exception; e is not null; e = e.InnerException) yield return e;
    }
}

/// <summary>Copies request details onto an error entry, redacting secrets.</summary>
public static partial class ErrorLogRequestSnapshot
{
    private static readonly string[] SensitiveFragments =
    [
        "password", "passwd", "secret", "token", "apikey", "api_key", "authorization",
        "credential", "privatekey", "private_key", "connectionstring",
    ];
    private static readonly HashSet<string> SensitiveExact = new(StringComparer.OrdinalIgnoreCase) { "pin", "otp", "cvv" };
    private static readonly string[] CapturedHeaders =
        ["User-Agent", "Referer", "Origin", "Content-Type", "Content-Length", "Module", "SubModule", "X-Forwarded-For"];

    public static void Apply(ErrorLog entry, HttpContext context, string traceId, string requestBody, long? elapsedMs)
    {
        var request = context.Request;
        entry.Source = "Request";
        entry.TraceId = traceId;
        entry.StatusCode = context.Response.StatusCode;
        entry.HttpMethod = request.Method;
        entry.Path = ErrorLogFactory.Truncate(request.Path.Value, 2048);
        entry.RouteTemplate = ErrorLogFactory.Truncate((context.GetEndpoint() as RouteEndpoint)?.RoutePattern.RawText, 512);
        entry.QueryString = RedactPairs(request.QueryString.Value?.TrimStart('?'));
        entry.RequestBody = requestBody;
        entry.RequestHeaders = JsonSerializer.Serialize(CapturedHeaders
            .Where(h => request.Headers.ContainsKey(h))
            .ToDictionary(h => h, h => request.Headers[h].ToString()));
        entry.UserId = ResolveUserId(context);
        entry.UserEmail = ErrorLogFactory.Truncate(
            context.User?.FindFirstValue(ClaimTypes.Email) ?? context.User?.FindFirstValue("email"), 256);
        entry.IpAddress = ErrorLogFactory.Truncate(
            request.Headers["X-Forwarded-For"].FirstOrDefault()?.Split(',')[0].Trim()
            ?? context.Connection.RemoteIpAddress?.ToString(), 64);
        entry.ElapsedMs = elapsedMs;
    }

    /// <summary>Reads the buffered request body (text formats only), capped and redacted.</summary>
    public static async Task<string> ReadBodyAsync(HttpRequest request, int maxChars)
    {
        try
        {
            if (!request.Body.CanSeek || request.ContentLength is 0 || !IsText(request.ContentType)) return null;
            request.Body.Position = 0;
            using var reader = new StreamReader(request.Body, Encoding.UTF8, false, 4096, leaveOpen: true);
            var buffer = new char[maxChars + 1];
            var read = await reader.ReadBlockAsync(buffer, 0, buffer.Length);
            request.Body.Position = 0;
            if (read == 0) return null;

            var truncated = read > maxChars;
            var text = new string(buffer, 0, Math.Min(read, maxChars));
            text = request.HasFormContentType ? RedactPairs(text) : RedactJson(text);
            return truncated ? text + "…[truncated]" : text;
        }
        catch (Exception)
        {
            // A body that can't be re-read must never hide the original error.
            return null;
        }
    }

    public static string RedactJson(string json) =>
        string.IsNullOrEmpty(json) ? json : JsonProperty().Replace(json, m =>
            IsSensitive(Regex.Unescape(m.Groups["key"].Value))
                ? m.Value[..(m.Groups["value"].Index - m.Index)] + "\"***\""
                : m.Value);

    public static string RedactPairs(string pairs) =>
        string.IsNullOrEmpty(pairs) ? pairs : string.Join("&", pairs.Split('&').Select(pair =>
        {
            var separator = pair.IndexOf('=');
            if (separator < 0) return pair;
            var key = Uri.UnescapeDataString(pair[..separator].Replace('+', ' '));
            return IsSensitive(key) ? pair[..separator] + "=***" : pair;
        }));

    public static bool IsSensitive(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return false;
        var normalized = key.Trim();
        return SensitiveExact.Contains(normalized)
            || SensitiveFragments.Any(f => normalized.Contains(f, StringComparison.OrdinalIgnoreCase));
    }

    private static Guid? ResolveUserId(HttpContext context)
    {
        var value = context.Items["Sub"] as string
            ?? context.User?.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? context.User?.FindFirstValue("sub");
        return Guid.TryParse(value, out var id) ? id : null;
    }

    private static bool IsText(string contentType)
    {
        if (string.IsNullOrEmpty(contentType)) return false;
        var type = contentType.Split(';')[0].Trim().ToLowerInvariant();
        return type is "application/json" or "application/xml" or "application/x-www-form-urlencoded"
            || type.EndsWith("+json") || type.StartsWith("text/");
    }

    [GeneratedRegex("\"(?<key>(?:[^\"\\\\]|\\\\.){1,200})\"\\s*:\\s*(?<value>\"(?:[^\"\\\\]|\\\\.)*\"|-?\\d[\\d.eE+-]*|true|false|null)")]
    private static partial Regex JsonProperty();
}
