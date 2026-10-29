using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace APP.Services.ErrorLogging;

/// <summary>
/// Turns every Error/Critical log from anywhere in the app — controllers, repositories, background
/// jobs, EF Core — into a stored error entry. Errors raised during a request are held until the
/// request ends so they can carry its details; everything else is queued straight away.
/// </summary>
[ProviderAlias("ErrorLog")]
public sealed class ErrorLogLoggerProvider(
    ErrorLogQueue queue, IHttpContextAccessor httpContextAccessor, IOptions<ErrorLogOptions> options) : ILoggerProvider
{
    private readonly string _instance = options.Value.ResolveInstance();
    private readonly string _release = options.Value.ResolveRelease();

    private bool Enabled => queue.Enabled;

    public ILogger CreateLogger(string categoryName) => new ErrorLogLogger(categoryName, this);

    public void Dispose() { }

    private void Record<TState>(string category, LogLevel level, TState state, Exception exception, Func<TState, Exception, string> formatter)
    {
        // Never record our own failures: the writer logging that the database is down must not loop.
        if (ErrorLogSuppression.IsActive || category.StartsWith(typeof(ErrorLogQueue).Namespace!, StringComparison.Ordinal)) return;

        string message;
        try { message = formatter(state, exception); }
        catch { message = exception?.Message; }

        var template = state is IEnumerable<KeyValuePair<string, object>> values
            ? values.FirstOrDefault(v => v.Key == "{OriginalFormat}").Value as string
            : null;

        var entry = ErrorLogFactory.Create(category, level, message, exception, template, _instance, _release);

        var context = httpContextAccessor.HttpContext;
        if (context?.Items[ErrorLogRequestContext.ItemKey] is ErrorLogRequestContext request && request.TryAdd(entry, exception))
            return;

        if (context is not null)
        {
            // Logged outside the error middleware's window (e.g. while the response was being sent).
            var traceId = (context.Items[ErrorLogRequestContext.ItemKey] as ErrorLogRequestContext)?.TraceId ?? context.TraceIdentifier;
            ErrorLogRequestSnapshot.Apply(entry, context, traceId, requestBody: null, elapsedMs: null);
        }

        queue.Enqueue(entry);
    }

    private sealed class ErrorLogLogger(string category, ErrorLogLoggerProvider provider) : ILogger
    {
        public IDisposable BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Error && provider.Enabled;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel)) return;
            try { provider.Record(category, logLevel, state, exception, formatter); }
            catch { /* Logging must never break the caller. */ }
        }
    }
}
