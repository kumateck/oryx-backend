using SHARED;

namespace DOMAIN.Entities.ErrorLogs;

/// <summary>
/// One server-side error, stored in the instance's own database so production issues can be
/// diagnosed on customer servers without shell access or an external service.
/// </summary>
public class ErrorLog
{
    public Guid Id { get; set; }
    public DateTime OccurredAt { get; set; }

    /// <summary>Error or Critical.</summary>
    public string Level { get; set; }

    /// <summary>"Request" when raised while serving an HTTP request, otherwise "Background".</summary>
    public string Source { get; set; }

    /// <summary>Logger category, usually the class that logged the error.</summary>
    public string Category { get; set; }

    public string Message { get; set; }
    public string ExceptionType { get; set; }

    /// <summary>Full exception text: message, stack trace and inner exceptions.</summary>
    public string ExceptionDetails { get; set; }

    /// <summary>Stable hash that groups repeats of the same failure.</summary>
    public string Fingerprint { get; set; }

    /// <summary>Request reference returned to clients in the X-Trace-Id header and error responses.</summary>
    public string TraceId { get; set; }

    public int? StatusCode { get; set; }
    public string HttpMethod { get; set; }
    public string Path { get; set; }
    public string RouteTemplate { get; set; }
    public string QueryString { get; set; }

    /// <summary>Request body with secrets redacted and size capped.</summary>
    public string RequestBody { get; set; }

    /// <summary>Selected non-sensitive request headers as JSON.</summary>
    public string RequestHeaders { get; set; }

    public Guid? UserId { get; set; }
    public string UserEmail { get; set; }
    public string IpAddress { get; set; }
    public long? ElapsedMs { get; set; }

    public string Release { get; set; }
    public string Instance { get; set; }
    public string MachineName { get; set; }

    /// <summary>When the central monitor accepted this entry; null while it still has to be sent.</summary>
    public DateTime? ForwardedAt { get; set; }
}

public class ErrorLogFilter : PagedQuery
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    /// <summary>Matches message, exception type, path or category.</summary>
    public string SearchQuery { get; set; }

    /// <summary>Reference shown to the user. Accepts the bare id or the full W3C traceparent.</summary>
    public string TraceId { get; set; }

    public string Fingerprint { get; set; }
    public Guid? UserId { get; set; }
    public int? StatusCode { get; set; }
    public string Source { get; set; }
}

public class ErrorLogListDto
{
    public Guid Id { get; set; }
    public DateTime OccurredAt { get; set; }
    public string Level { get; set; }
    public string Source { get; set; }
    public string Category { get; set; }
    public string Message { get; set; }
    public string ExceptionType { get; set; }
    public string Fingerprint { get; set; }
    public string TraceId { get; set; }
    public int? StatusCode { get; set; }
    public string HttpMethod { get; set; }
    public string Path { get; set; }
    public Guid? UserId { get; set; }
    public string UserEmail { get; set; }
    public string UserName { get; set; }
}

public class ErrorLogDto : ErrorLogListDto
{
    public string ExceptionDetails { get; set; }
    public string RouteTemplate { get; set; }
    public string QueryString { get; set; }
    public string RequestBody { get; set; }
    public string RequestHeaders { get; set; }
    public string IpAddress { get; set; }
    public long? ElapsedMs { get; set; }
    public string Release { get; set; }
    public string Instance { get; set; }
    public string MachineName { get; set; }
}

/// <summary>Repeats of one failure, most frequent first, for triage.</summary>
public class ErrorLogSummaryDto
{
    public string Fingerprint { get; set; }
    public int Count { get; set; }
    public DateTime FirstSeen { get; set; }
    public DateTime LastSeen { get; set; }
    public Guid LatestId { get; set; }
    public string ExceptionType { get; set; }
    public string Message { get; set; }
    public string Category { get; set; }
    public string Path { get; set; }
}
