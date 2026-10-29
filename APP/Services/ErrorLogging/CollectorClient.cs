using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using DOMAIN.Entities.ErrorLogs;
using Microsoft.Extensions.Options;

namespace APP.Services.ErrorLogging;

/// <summary>
/// Sends error entries and heartbeats to the central Oryx Monitor over HTTPS
/// (POST {CollectorUrl}/ingest/v1/errors and /ingest/v1/heartbeat, gzipped JSON, X-Api-Key header).
/// </summary>
public sealed class CollectorClient(HttpClient http, IOptions<ErrorLogOptions> options)
{
    public const string ApiKeyHeader = "X-Api-Key";

    /// <summary>The monitor rejects larger payloads with 413, so they are not sent at all.</summary>
    public const int MaxPayloadBytes = 5 * 1024 * 1024;

    public const int MaxEventsPerRequest = 500;

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private static readonly DateTime StartedAt = DateTime.UtcNow;

    public bool Enabled => options.Value.CollectorEnabled;

    public async Task<CollectorResponse> SendErrorsAsync(IReadOnlyList<ErrorLog> entries, CancellationToken cancellationToken)
    {
        if (entries.Count > MaxEventsPerRequest) return CollectorResponse.Rejected(HttpStatusCode.RequestEntityTooLarge);

        var settings = options.Value;
        var payload = new
        {
            instance = settings.ResolveInstance(),
            release = settings.ResolveRelease(),
            machineName = Environment.MachineName,
            events = entries.Select(e => ToEvent(e, settings.ForwardRequestBody)),
        };
        var json = JsonSerializer.SerializeToUtf8Bytes(payload, Json);
        if (json.Length > MaxPayloadBytes) return CollectorResponse.Rejected(HttpStatusCode.RequestEntityTooLarge);

        return await PostAsync("ingest/v1/errors", json, cancellationToken);
    }

    public Task<CollectorResponse> SendHeartbeatAsync(CancellationToken cancellationToken)
    {
        var settings = options.Value;
        var json = JsonSerializer.SerializeToUtf8Bytes(new
        {
            instance = settings.ResolveInstance(),
            release = settings.ResolveRelease(),
            machineName = Environment.MachineName,
            startedAt = StartedAt,
            sentAt = DateTime.UtcNow,
        }, Json);
        return PostAsync("ingest/v1/heartbeat", json, cancellationToken);
    }

    public static object ToEvent(ErrorLog e, bool includeRequestBody) => new
    {
        id = e.Id,
        occurredAt = DateTime.SpecifyKind(e.OccurredAt, DateTimeKind.Utc),
        level = e.Level,
        source = e.Source,
        category = e.Category,
        message = e.Message,
        exceptionType = e.ExceptionType,
        exceptionDetails = e.ExceptionDetails,
        fingerprint = e.Fingerprint,
        traceId = e.TraceId,
        statusCode = e.StatusCode,
        httpMethod = e.HttpMethod,
        path = e.Path,
        routeTemplate = e.RouteTemplate,
        queryString = e.QueryString,
        requestBody = includeRequestBody ? e.RequestBody : null,
        requestHeaders = e.RequestHeaders,
        userId = e.UserId,
        userEmail = e.UserEmail,
        ipAddress = e.IpAddress,
        elapsedMs = e.ElapsedMs,
        release = e.Release,
        instance = e.Instance,
        machineName = e.MachineName,
    };

    private async Task<CollectorResponse> PostAsync(string path, byte[] json, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{settings.CollectorUrl.Trim().TrimEnd('/')}/{path}");
        request.Headers.Add(ApiKeyHeader, settings.CollectorApiKey.Trim());
        request.Content = new ByteArrayContent(Gzip(json));
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        request.Content.Headers.ContentEncoding.Add("gzip");

        try
        {
            using var response = await http.SendAsync(request, cancellationToken);
            var retryAfter = response.Headers.RetryAfter?.Delta
                ?? (response.Headers.RetryAfter?.Date is { } date ? date - DateTimeOffset.UtcNow : null);
            return new CollectorResponse(response.StatusCode, retryAfter, null);
        }
        catch (Exception exception) when (exception is HttpRequestException
                                          || (exception is TaskCanceledException && !cancellationToken.IsCancellationRequested))
        {
            // Network failure or the 30-second client timeout.
            return new CollectorResponse(null, null, exception);
        }
    }

    private static byte[] Gzip(byte[] data)
    {
        using var output = new MemoryStream();
        using (var gzip = new GZipStream(output, CompressionLevel.Fastest, leaveOpen: true)) gzip.Write(data);
        return output.ToArray();
    }
}

public enum CollectorOutcome
{
    Accepted,

    /// <summary>Network error, timeout, 5xx, 429 or another unexpected status: try again later.</summary>
    Unavailable,

    /// <summary>401/403: the API key is wrong or disabled.</summary>
    Unauthorized,

    /// <summary>400/413: this payload will never be accepted as is.</summary>
    Rejected,
}

public readonly record struct CollectorResponse(HttpStatusCode? StatusCode, TimeSpan? RetryAfter, Exception Error)
{
    public static CollectorResponse Rejected(HttpStatusCode status) => new(status, null, null);

    public CollectorOutcome Outcome => StatusCode switch
    {
        null => CollectorOutcome.Unavailable,
        { } s when (int)s is >= 200 and < 300 => CollectorOutcome.Accepted,
        HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => CollectorOutcome.Unauthorized,
        HttpStatusCode.BadRequest or HttpStatusCode.RequestEntityTooLarge => CollectorOutcome.Rejected,
        // 5xx, 429, and anything unexpected such as a 404 from a wrong URL: keep the entries and retry.
        _ => CollectorOutcome.Unavailable,
    };

    public string Describe() => StatusCode is { } s ? $"HTTP {(int)s}" : Error?.Message ?? "no response";
}
