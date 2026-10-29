using APP.IRepository;
using APP.Repository;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace APP.Services.ErrorLogging;

public static class ErrorLoggingServiceCollectionExtensions
{
    /// <summary>
    /// Stores Error/Critical logs in the ErrorLogs table, exposes them to IT users and, when
    /// configured, sends them to the central Oryx Monitor.
    /// </summary>
    public static IServiceCollection AddErrorLogging(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ErrorLogOptions>(configuration.GetSection(ErrorLogOptions.Section));
        services.AddHttpContextAccessor();
        services.AddSingleton<ErrorLogQueue>();
        services.AddSingleton<ErrorLogForwardSignal>();
        // Long-lived (used by singletons), so connections are recycled to pick up DNS changes.
        services.AddHttpClient<CollectorClient>(c => c.Timeout = TimeSpan.FromSeconds(30))
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler { PooledConnectionLifetime = TimeSpan.FromMinutes(5) })
            .SetHandlerLifetime(Timeout.InfiniteTimeSpan);
        services.AddSingleton<ILoggerProvider, ErrorLogLoggerProvider>();
        services.AddHostedService<ErrorLogWriter>();
        services.AddHostedService<ErrorLogForwarder>();
        services.AddHostedService<CollectorHeartbeat>();
        services.AddScoped<IErrorLogRepository, ErrorLogRepository>();
        return services;
    }
}
