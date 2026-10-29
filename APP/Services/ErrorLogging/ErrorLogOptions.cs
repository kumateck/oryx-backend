namespace APP.Services.ErrorLogging;

/// <summary>Bound from the "ErrorLogging" configuration section (env: ErrorLogging__Name).</summary>
public class ErrorLogOptions
{
    public const string Section = "ErrorLogging";

    /// <summary>Store errors in the database. On by default.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Customer or server name stamped on every entry. Falls back to the "Environment" variable, then the machine name.</summary>
    public string Instance { get; set; }

    /// <summary>Build identifier stamped on every entry. Falls back to the ORYX_RELEASE variable set at image build time.</summary>
    public string Release { get; set; }

    /// <summary>
    /// Keep a copy in this instance's own Postgres database (ErrorLogs table), which the
    /// /api/v1/error-log endpoints read. On by default.
    /// </summary>
    public bool StoreInDatabase { get; set; } = true;

    /// <summary>
    /// Base URL of the central Oryx Monitor (e.g. https://monitor.example.com). With
    /// <see cref="CollectorApiKey"/> set, every error is also sent there. Empty = off.
    /// </summary>
    public string CollectorUrl { get; set; }

    /// <summary>This customer's API key for the monitor, sent in the X-Api-Key header.</summary>
    public string CollectorApiKey { get; set; }

    /// <summary>Send captured request bodies to the monitor. When false they stay on this server only.</summary>
    public bool ForwardRequestBody { get; set; } = true;

    /// <summary>How often this server tells the monitor it is alive. 0 or less = no heartbeat.</summary>
    public int HeartbeatIntervalMinutes { get; set; } = 5;

    public bool CollectorEnabled =>
        !string.IsNullOrWhiteSpace(CollectorUrl) && !string.IsNullOrWhiteSpace(CollectorApiKey);

    /// <summary>Entries older than this are deleted automatically from this instance's database.</summary>
    public int RetentionDays { get; set; } = 90;

    /// <summary>Request bodies longer than this are truncated.</summary>
    public int MaxRequestBodyChars { get; set; } = 16_000;

    /// <summary>Entries waiting to be written; the oldest are dropped beyond this so a burst can't exhaust memory.</summary>
    public int QueueCapacity { get; set; } = 5_000;

    public string ResolveInstance() =>
        FirstNonBlank(Instance, Environment.GetEnvironmentVariable("Environment"), Environment.MachineName);

    public string ResolveRelease() =>
        FirstNonBlank(Release, Environment.GetEnvironmentVariable("ORYX_RELEASE"), "unversioned");

    private static string FirstNonBlank(params string[] values) =>
        values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v))?.Trim();
}
