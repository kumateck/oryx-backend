using System.Security.Claims;
using System.Text;
using APP.Middlewares;
using APP.Repository;
using APP.Services.ErrorLogging;
using DOMAIN.Entities.ErrorLogs;
using INFRASTRUCTURE.Context;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Utils;

public class ErrorLoggingTests
{
    private readonly IOptions<ErrorLogOptions> _options = Options.Create(new ErrorLogOptions { Instance = "acme", Release = "abc123" });
    private readonly HttpContextAccessor _accessor = new();
    private readonly ErrorLogQueue _queue;
    private readonly ILoggerFactory _loggers;

    public ErrorLoggingTests()
    {
        _queue = new ErrorLogQueue(_options);
        _loggers = LoggerFactory.Create(b => b.AddProvider(new ErrorLogLoggerProvider(_queue, _accessor, _options)));
    }

    [Fact]
    public async Task UnhandledException_Returns500WithTraceId_AndStoresRequestDetailsWithSecretsRedacted()
    {
        var userId = Guid.NewGuid();
        var context = CreateContext("""{"name":"Batch 7","password":"hunter2","nested":{"accessToken":"abc"}}""", "?page=1&token=xyz");
        context.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, userId.ToString())], "test"));

        await Middleware(_ => throw new InvalidOperationException("Stock went negative")).InvokeAsync(context);

        Assert.Equal(500, context.Response.StatusCode);
        var entry = Assert.Single(Drain());
        Assert.Contains($"\"traceId\":\"{entry.TraceId}\"", ReadResponse(context));
        Assert.Equal("Request", entry.Source);
        Assert.Equal("System.InvalidOperationException", entry.ExceptionType);
        Assert.Contains("Stock went negative", entry.ExceptionDetails);
        Assert.Equal("POST", entry.HttpMethod);
        Assert.Equal("/api/v1/production", entry.Path);
        Assert.Equal(500, entry.StatusCode);
        Assert.Equal(userId, entry.UserId);
        Assert.Equal("acme", entry.Instance);
        Assert.Equal("abc123", entry.Release);
        Assert.Contains("Batch 7", entry.RequestBody);
        Assert.DoesNotContain("hunter2", entry.RequestBody);
        Assert.DoesNotContain("\"abc\"", entry.RequestBody);
        Assert.Equal("page=1&token=***", entry.QueryString);
    }

    [Fact]
    public async Task SameExceptionLoggedTwice_IsStoredOnce()
    {
        var context = CreateContext();
        var efLogger = _loggers.CreateLogger("Microsoft.EntityFrameworkCore.Update");

        await Middleware(_ =>
        {
            var failure = new DbUpdateException("duplicate key");
            efLogger.LogError(failure, "An error occurred while saving the entity changes.");
            throw failure;
        }).InvokeAsync(context);

        var entry = Assert.Single(Drain());
        Assert.Equal("Microsoft.EntityFrameworkCore.Update", entry.Category);
    }

    [Fact]
    public async Task WrappingException_ReplacesTheInnerOne()
    {
        var context = CreateContext();
        var repoLogger = _loggers.CreateLogger("APP.Repository.StockRepository");

        await Middleware(_ =>
        {
            var inner = new TimeoutException("db timeout");
            repoLogger.LogError(inner, "Query failed");
            throw new InvalidOperationException("Could not post stock", inner);
        }).InvokeAsync(context);

        var entry = Assert.Single(Drain());
        Assert.Contains("Could not post stock", entry.ExceptionDetails);
        Assert.Contains("db timeout", entry.ExceptionDetails);
    }

    [Fact]
    public async Task ClientDisconnect_IsNotRecordedAsAnError()
    {
        var context = CreateContext();
        using var aborted = new CancellationTokenSource();
        aborted.Cancel();
        context.RequestAborted = aborted.Token;

        await Middleware(_ => throw new OperationCanceledException()).InvokeAsync(context);

        Assert.Equal(499, context.Response.StatusCode);
        Assert.Empty(Drain());
    }

    [Fact]
    public async Task ServerErrorWithoutException_IsRecorded()
    {
        var context = CreateContext();

        await Middleware(c => { c.Response.StatusCode = 500; return Task.CompletedTask; }).InvokeAsync(context);

        var entry = Assert.Single(Drain());
        Assert.Equal(500, entry.StatusCode);
        Assert.Null(entry.ExceptionType);
    }

    [Fact]
    public async Task SuccessfulRequest_RecordsNothing()
    {
        await Middleware(c => { c.Response.StatusCode = 200; return Task.CompletedTask; }).InvokeAsync(CreateContext());
        Assert.Empty(Drain());
    }

    [Fact]
    public void ErrorOutsideARequest_IsQueuedAsBackground()
    {
        _loggers.CreateLogger("APP.Services.Background.LeaveExpiryService").LogError(new Exception("boom"), "Leave expiry failed");

        var entry = Assert.Single(Drain());
        Assert.Equal("Background", entry.Source);
        Assert.Equal("Leave expiry failed", entry.Message);
        Assert.Null(entry.TraceId);
    }

    [Fact]
    public void WarningsAndSuppressedErrors_AreIgnored()
    {
        var logger = _loggers.CreateLogger("APP.Anything");
        logger.LogWarning(new Exception("warn"), "Just a warning");
        using (ErrorLogSuppression.Begin()) logger.LogError(new Exception("loop"), "Writer failed");
        _loggers.CreateLogger(typeof(ErrorLogWriter).FullName!).LogError("Own failure");

        Assert.Empty(Drain());
    }

    [Fact]
    public void Fingerprint_GroupsTheSameThrowSite_AndSeparatesDifferentOnes()
    {
        static Exception ThrowA(string message) { try { throw new InvalidOperationException(message); } catch (Exception e) { return e; } }
        static Exception ThrowB() { try { throw new InvalidOperationException("x"); } catch (Exception e) { return e; } }

        var first = ErrorLogFactory.Fingerprint("c", null, ThrowA("order 1"));
        var second = ErrorLogFactory.Fingerprint("c", null, ThrowA("order 2"));
        var other = ErrorLogFactory.Fingerprint("c", null, ThrowB());

        Assert.Equal(first, second);
        Assert.NotEqual(first, other);
    }

    [Theory]
    [InlineData("""{"Password":"a","userName":"bob"}""", """{"Password":"***","userName":"bob"}""")]
    [InlineData("""{"refreshToken":"t","pin":1234,"shipping":5}""", """{"refreshToken":"***","pin":"***","shipping":5}""")]
    [InlineData("""{"code":"MAT-01","otp":"991"}""", """{"code":"MAT-01","otp":"***"}""")]
    public void RedactJson_HidesSecretValuesOnly(string input, string expected) =>
        Assert.Equal(expected, ErrorLogRequestSnapshot.RedactJson(input));

    [Theory]
    [InlineData("00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01", "4bf92f3577b34da6a3ce929d0e0e4736")]
    [InlineData(" 4bf92f3577b34da6a3ce929d0e0e4736 ", "4bf92f3577b34da6a3ce929d0e0e4736")]
    [InlineData("", null)]
    public void NormalizeTraceId_AcceptsBareIdOrTraceparent(string input, string expected) =>
        Assert.Equal(expected, ErrorLogRepository.NormalizeTraceId(input));

    [Fact]
    public async Task Writer_SavesQueuedEntries_AndRepositoryFindsAndGroupsThem()
    {
        var services = new ServiceCollection();
        var database = Guid.NewGuid().ToString();
        services.AddDbContext<ApplicationDbContext>(o => o.UseInMemoryDatabase(database));
        services.AddScoped<ICurrentUserService, TestCurrentUser>();
        await using var provider = services.BuildServiceProvider();

        var now = DateTime.UtcNow;
        _queue.Enqueue(Entry("aaaa", "trace-1", now.AddMinutes(-3), "first"));
        _queue.Enqueue(Entry("aaaa", "trace-2", now.AddMinutes(-1), "latest"));
        _queue.Enqueue(Entry("bbbb", "trace-3", now.AddMinutes(-2), "other"));
        _queue.Enqueue(Entry("cccc", "trace-old", now.AddDays(-30), "old"));

        var writer = new ErrorLogWriter(_queue, provider.GetRequiredService<IServiceScopeFactory>(), _options, NullLogger<ErrorLogWriter>.Instance,
            new CollectorClient(new HttpClient(), _options), new ErrorLogForwardSignal());
        await writer.FlushAsync(CancellationToken.None);

        using var scope = provider.CreateScope();
        var repository = new ErrorLogRepository(scope.ServiceProvider.GetRequiredService<ApplicationDbContext>());

        var recent = await repository.GetErrorLogs(new ErrorLogFilter { PageSize = 50 });
        Assert.Equal(3, recent.TotalRecordCount);
        Assert.Equal("latest", recent.Data.First().Message);

        // A trace lookup ignores the default 7-day window.
        var byTrace = await repository.GetErrorLogs(new ErrorLogFilter { TraceId = "trace-old", PageSize = 10 });
        Assert.Equal("old", Assert.Single(byTrace.Data).Message);

        var summary = await repository.GetErrorSummary(new ErrorLogFilter());
        Assert.Equal(["aaaa", "bbbb"], summary.Select(s => s.Fingerprint));
        Assert.Equal(2, summary[0].Count);
        Assert.Equal("latest", summary[0].Message);

        var missing = await repository.GetErrorLog(Guid.NewGuid());
        Assert.True(missing.IsFailure);
    }

    [Fact]
    public void Forwarding_IsOffWithoutCollectorSettings() =>
        Assert.False(new CollectorClient(new HttpClient(), _options).Enabled);

    private ExceptionHandlingMiddleware Middleware(RequestDelegate next) =>
        new(next, _loggers.CreateLogger<ExceptionHandlingMiddleware>(), _queue, _options);

    private DefaultHttpContext CreateContext(string jsonBody = null, string query = "")
    {
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Path = "/api/v1/production";
        context.Request.QueryString = new QueryString(query);
        if (jsonBody is not null)
        {
            var bytes = Encoding.UTF8.GetBytes(jsonBody);
            context.Request.Body = new MemoryStream(bytes);
            context.Request.ContentType = "application/json";
            context.Request.ContentLength = bytes.Length;
        }
        context.Response.Body = new MemoryStream();
        _accessor.HttpContext = context;
        return context;
    }

    private static string ReadResponse(HttpContext context)
    {
        context.Response.Body.Position = 0;
        return new StreamReader(context.Response.Body).ReadToEnd();
    }

    private List<ErrorLog> Drain()
    {
        var entries = new List<ErrorLog>();
        while (_queue.Reader.TryRead(out var entry)) entries.Add(entry);
        return entries;
    }

    private static ErrorLog Entry(string fingerprint, string traceId, DateTime at, string message) => new()
    {
        Id = Guid.NewGuid(), Fingerprint = fingerprint, TraceId = traceId, OccurredAt = at, Message = message,
        Level = "Error", Source = "Request", Category = "test",
    };

    private sealed class TestCurrentUser : ICurrentUserService
    {
        public Guid? UserId => null;
        public Guid? DepartmentId => null;
        public string DepartmentType => string.Empty;
    }
}
