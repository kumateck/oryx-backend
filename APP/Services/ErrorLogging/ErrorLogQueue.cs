using System.Threading.Channels;
using DOMAIN.Entities.ErrorLogs;
using Microsoft.Extensions.Options;

namespace APP.Services.ErrorLogging;

/// <summary>Hands captured errors to <see cref="ErrorLogWriter"/> without blocking the request.</summary>
public class ErrorLogQueue(IOptions<ErrorLogOptions> options)
{
    private readonly Channel<ErrorLog> _channel = Channel.CreateBounded<ErrorLog>(
        new BoundedChannelOptions(Math.Max(1, options.Value.QueueCapacity))
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
        });

    public bool Enabled => options.Value.Enabled;

    public void Enqueue(ErrorLog entry)
    {
        if (Enabled) _channel.Writer.TryWrite(entry);
    }

    public ChannelReader<ErrorLog> Reader => _channel.Reader;
}

/// <summary>
/// Stops the error log from recording its own failures (for example EF logging a failed insert
/// while the writer saves), which would otherwise loop forever when the database is down.
/// </summary>
public static class ErrorLogSuppression
{
    private static readonly AsyncLocal<bool> Active = new();

    public static bool IsActive => Active.Value;

    public static IDisposable Begin()
    {
        var previous = Active.Value;
        Active.Value = true;
        return new Restore(previous);
    }

    private sealed class Restore(bool previous) : IDisposable
    {
        public void Dispose() => Active.Value = previous;
    }
}
