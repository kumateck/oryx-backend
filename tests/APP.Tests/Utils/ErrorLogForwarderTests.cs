using System.IO.Compression;
using System.Net;
using System.Text.Json;
using APP.Services.ErrorLogging;
using DOMAIN.Entities.ErrorLogs;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Utils;

public class ErrorLogForwarderTests : IAsyncLifetime
{
    private const string ApiKey = "test-key-123";
    private readonly ErrorLogOptions _settings = new()
    {
        Instance = "acme", Release = "abc123", CollectorUrl = "https://monitor.test/", CollectorApiKey = ApiKey,
    };
    private readonly FakeCollector _collector = new();
    private ServiceProvider _provider;

    public Task InitializeAsync()
    {
        var services = new ServiceCollection();
        var database = Guid.NewGuid().ToString();
        services.AddDbContext<ApplicationDbContext>(o => o.UseInMemoryDatabase(database));
        services.AddScoped<ICurrentUserService, TestCurrentUser>();
        _provider = services.BuildServiceProvider();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync() => await _provider.DisposeAsync();

    [Fact]
    public async Task ServerErrors_KeepRowsUnsent_AndBackOffExponentially_UntilAccepted()
    {
        var ids = await Seed(3);
        var forwarder = Forwarder();

        _collector.Respond(HttpStatusCode.InternalServerError);
        Assert.Equal(TimeSpan.FromSeconds(5), await forwarder.ForwardPendingAsync(CancellationToken.None));
        Assert.Equal(3, await CountUnsent());

        _collector.Respond(HttpStatusCode.ServiceUnavailable);
        Assert.Equal(TimeSpan.FromSeconds(10), await forwarder.ForwardPendingAsync(CancellationToken.None));
        Assert.Equal(3, await CountUnsent());

        _collector.Respond(HttpStatusCode.OK);
        Assert.Null(await forwarder.ForwardPendingAsync(CancellationToken.None));
        Assert.Equal(0, await CountUnsent());

        // The last request carried every entry, oldest first.
        Assert.Equal(ids, _collector.Requests[^1].EventIds);

        // Backoff starts over after a success.
        await Seed(1);
        _collector.Respond(HttpStatusCode.BadGateway);
        Assert.Equal(TimeSpan.FromSeconds(5), await forwarder.ForwardPendingAsync(CancellationToken.None));
    }

    [Fact]
    public async Task NetworkFailure_AndRetryAfter_AreHonoured_AndBackoffIsCapped()
    {
        await Seed(1);
        var forwarder = Forwarder();

        _collector.Fail();
        Assert.Equal(TimeSpan.FromSeconds(5), await forwarder.ForwardPendingAsync(CancellationToken.None));

        _collector.Respond(HttpStatusCode.TooManyRequests, retryAfterSeconds: 120);
        Assert.Equal(TimeSpan.FromSeconds(120), await forwarder.ForwardPendingAsync(CancellationToken.None));

        _collector.Respond(HttpStatusCode.InternalServerError);
        for (var i = 0; i < 10; i++) await forwarder.ForwardPendingAsync(CancellationToken.None);
        Assert.Equal(TimeSpan.FromMinutes(5), await forwarder.ForwardPendingAsync(CancellationToken.None));
        Assert.Equal(1, await CountUnsent());
    }

    [Fact]
    public async Task BadApiKey_WaitsFiveMinutes_WithoutMarkingRows()
    {
        await Seed(2);
        _collector.Respond(HttpStatusCode.Unauthorized);

        Assert.Equal(TimeSpan.FromMinutes(5), await Forwarder().ForwardPendingAsync(CancellationToken.None));
        Assert.Equal(2, await CountUnsent());
    }

    [Fact]
    public async Task RejectedBatch_IsSplit_AndASingleBadEntryIsSkipped()
    {
        var ids = await Seed(5);
        var poison = ids[3];
        _collector.Handler = request => request.EventIds.Contains(poison)
            ? HttpStatusCode.BadRequest
            : HttpStatusCode.OK;

        Assert.Null(await Forwarder().ForwardPendingAsync(CancellationToken.None));

        Assert.Equal(0, await CountUnsent());
        Assert.Contains(_collector.Requests, r => r.EventIds.SequenceEqual([poison]));
        var accepted = _collector.Requests.Where(r => !r.EventIds.Contains(poison)).SelectMany(r => r.EventIds);
        Assert.Equal(ids.Where(id => id != poison).OrderBy(id => id), accepted.OrderBy(id => id));
    }

    [Fact]
    public async Task Request_IsGzippedJson_WithApiKey_AndMatchesTheIngestContract()
    {
        await Seed(1, requestBody: """{"name":"Batch 7"}""");
        _collector.Respond(HttpStatusCode.OK);

        await Forwarder().ForwardPendingAsync(CancellationToken.None);

        var request = Assert.Single(_collector.Requests);
        Assert.Equal("https://monitor.test/ingest/v1/errors", request.Url);
        Assert.Equal(ApiKey, request.ApiKey);
        Assert.Equal("gzip", request.ContentEncoding);
        Assert.Equal("application/json", request.ContentType);

        var root = request.Json.RootElement;
        Assert.Equal("acme", root.GetProperty("instance").GetString());
        Assert.Equal("abc123", root.GetProperty("release").GetString());
        Assert.False(string.IsNullOrEmpty(root.GetProperty("machineName").GetString()));
        var e = Assert.Single(root.GetProperty("events").EnumerateArray().ToList());
        Assert.EndsWith("Z", e.GetProperty("occurredAt").GetString());
        Assert.Equal("""{"name":"Batch 7"}""", e.GetProperty("requestBody").GetString());
        Assert.Equal(500, e.GetProperty("statusCode").GetInt32());
        Assert.Equal(JsonValueKind.Null, e.GetProperty("userId").ValueKind);
        Assert.False(e.TryGetProperty("forwardedAt", out _));
    }

    [Fact]
    public async Task RequestBody_IsLeftOut_WhenForwardRequestBodyIsOff_ButKeptLocally()
    {
        _settings.ForwardRequestBody = false;
        await Seed(1, requestBody: """{"name":"Batch 7"}""");
        _collector.Respond(HttpStatusCode.OK);

        await Forwarder().ForwardPendingAsync(CancellationToken.None);

        var e = _collector.Requests.Single().Json.RootElement.GetProperty("events")[0];
        Assert.Equal(JsonValueKind.Null, e.GetProperty("requestBody").ValueKind);
        using var scope = _provider.CreateScope();
        var stored = await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().ErrorLogs.SingleAsync();
        Assert.Equal("""{"name":"Batch 7"}""", stored.RequestBody);
        Assert.NotNull(stored.ForwardedAt);
    }

    [Fact]
    public async Task Heartbeat_PostsInstanceAndTimes()
    {
        _collector.Respond(HttpStatusCode.NoContent);

        var response = await Client().SendHeartbeatAsync(CancellationToken.None);

        Assert.Equal(CollectorOutcome.Accepted, response.Outcome);
        var request = Assert.Single(_collector.Requests);
        Assert.Equal("https://monitor.test/ingest/v1/heartbeat", request.Url);
        Assert.Equal(ApiKey, request.ApiKey);
        var root = request.Json.RootElement;
        Assert.Equal("acme", root.GetProperty("instance").GetString());
        Assert.True(root.TryGetProperty("startedAt", out _));
        Assert.True(root.TryGetProperty("sentAt", out _));
    }

    [Fact]
    public async Task Writer_SendsStraightToTheMonitor_WhenNotStoringLocally()
    {
        _settings.StoreInDatabase = false;
        var options = Options.Create(_settings);
        var queue = new ErrorLogQueue(options);
        queue.Enqueue(Entry(DateTime.UtcNow, null));
        _collector.Respond(HttpStatusCode.OK);

        var writer = new ErrorLogWriter(queue, _provider.GetRequiredService<IServiceScopeFactory>(), options,
            NullLogger<ErrorLogWriter>.Instance, Client(), new ErrorLogForwardSignal());
        await writer.FlushAsync(CancellationToken.None);

        Assert.Single(_collector.Requests.Single().EventIds);
        Assert.False(Forwarder().Enabled);
    }

    private CollectorClient Client() => new(new HttpClient(_collector), Options.Create(_settings));

    private ErrorLogForwarder Forwarder() => new(
        _provider.GetRequiredService<IServiceScopeFactory>(), Client(), new ErrorLogForwardSignal(),
        Options.Create(_settings), NullLogger<ErrorLogForwarder>.Instance);

    private async Task<List<Guid>> Seed(int count, string requestBody = null)
    {
        using var scope = _provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var start = DateTime.UtcNow.AddMinutes(-count);
        // Inserted newest first so the test proves the forwarder orders by OccurredAt.
        var entries = Enumerable.Range(0, count).Select(i => Entry(start.AddMinutes(i), requestBody)).Reverse().ToList();
        db.ErrorLogs.AddRange(entries);
        await db.SaveChangesAsync();
        return entries.OrderBy(e => e.OccurredAt).Select(e => e.Id).ToList();
    }

    private async Task<int> CountUnsent()
    {
        using var scope = _provider.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().ErrorLogs.CountAsync(e => e.ForwardedAt == null);
    }

    private static ErrorLog Entry(DateTime at, string requestBody) => new()
    {
        Id = Guid.NewGuid(), OccurredAt = at, Level = "Error", Source = "Request", Category = "test",
        Message = "boom", Fingerprint = "aaaa", StatusCode = 500, RequestBody = requestBody,
    };

    private sealed record CapturedRequest(string Url, string ApiKey, string ContentEncoding, string ContentType, JsonDocument Json)
    {
        public List<Guid> EventIds => Json.RootElement.TryGetProperty("events", out var events)
            ? events.EnumerateArray().Select(e => e.GetProperty("id").GetGuid()).ToList()
            : [];
    }

    private sealed class FakeCollector : HttpMessageHandler
    {
        public List<CapturedRequest> Requests { get; } = [];
        public Func<CapturedRequest, HttpStatusCode> Handler { get; set; } = _ => HttpStatusCode.OK;
        private int? _retryAfter;
        private bool _fail;

        public void Respond(HttpStatusCode status, int? retryAfterSeconds = null)
        {
            _fail = false;
            _retryAfter = retryAfterSeconds;
            Handler = _ => status;
        }

        public void Fail() => _fail = true;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (_fail) throw new HttpRequestException("Connection refused");

            await using var gzip = new GZipStream(await request.Content!.ReadAsStreamAsync(cancellationToken), CompressionMode.Decompress);
            var captured = new CapturedRequest(
                request.RequestUri!.ToString(),
                request.Headers.GetValues(CollectorClient.ApiKeyHeader).Single(),
                string.Join(",", request.Content.Headers.ContentEncoding),
                request.Content.Headers.ContentType?.MediaType,
                await JsonDocument.ParseAsync(gzip, cancellationToken: cancellationToken));
            Requests.Add(captured);

            var response = new HttpResponseMessage(Handler(captured));
            if (_retryAfter is { } seconds) response.Headers.RetryAfter = new(TimeSpan.FromSeconds(seconds));
            return response;
        }
    }

    private sealed class TestCurrentUser : ICurrentUserService
    {
        public Guid? UserId => null;
        public Guid? DepartmentId => null;
        public string DepartmentType => string.Empty;
    }
}
