using APP.Services.NotificationService;
using DOMAIN.Entities.Alerts;
using DOMAIN.Entities.Notifications;
using DOMAIN.Entities.Users;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Notifications;

public sealed class AlertPostgresFactAttribute : FactAttribute
{
    public AlertPostgresFactAttribute()
    {
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ORYX_ALERT_TEST_CONNECTION")))
            Skip = "Set ORYX_ALERT_TEST_CONNECTION to the separately provisioned disposable alert database.";
    }
}
public class NotificationPostgresTests
{
    private sealed class Actor : ICurrentUserService
    {
        public Guid? UserId => null;
        public Guid? DepartmentId => null;
        public string DepartmentType => "";
    }
    private sealed class Effects : INotificationDeliveryEffects
    {
        public List<Guid> Sent { get; } = [];
        public bool Fail { get; set; }
        public async Task Publish(NotificationDto notification, CancellationToken cancellationToken)
        {
            if (Fail) throw new InvalidOperationException("Injected broker outage");
            lock (Sent) Sent.Add(notification.Id);
            await Task.Delay(100, cancellationToken);
        }
        public Task Email(NotificationDto notification, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task Read(NotificationReadEvent notification, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task Audit(Guid notificationId, Guid recipientId, CancellationToken cancellationToken) => Task.CompletedTask;
    }
    private static ApplicationDbContext Context()
    {
        var connection = Environment.GetEnvironmentVariable("ORYX_ALERT_TEST_CONNECTION")!;
        var options = new NpgsqlConnectionStringBuilder(connection);
        if (options.Host != "127.0.0.1" || options.Port != 55439 || options.Database != "oryx_alert_delivery_test")
            throw new InvalidOperationException("Refusing notification tests outside the disposable local alert database.");
        return new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(connection).Options, new Actor());
    }
    private static NotificationDeliveryDispatcher Dispatcher(ApplicationDbContext context, Effects effects) =>
        new(context, effects, NullLogger<NotificationDeliveryDispatcher>.Instance);
    [AlertPostgresFact]
    public async Task PostgreSql_claims_retry_after_context_restart_and_fence_concurrent_workers()
    {
        Guid notificationId; var user = Guid.NewGuid();
        await using (var setup = Context())
        {
            await setup.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO users (\"Id\", \"FirstName\", \"LastName\", \"Email\") VALUES ({user}, 'QA', 'Reviewer', 'qa@example.invalid')");
            var item = new NotificationDto { Message = "Disposable delivery test", Type = NotificationType.BmrBprRequested, Recipients = [new UserDto { Id = user }] };
            await new NotificationService(setup).SendNotification(item, [AlertType.InApp]);
            notificationId = item.Id;
        }
        await using (var failed = Context())
        {
            await Dispatcher(failed, new Effects { Fail = true }).DispatchOne(DateTime.UtcNow);
            var row = await failed.Set<NotificationDelivery>().SingleAsync(item => item.Id == notificationId);
            Assert.Null(row.CompletedAt); Assert.Equal(1, row.Attempts);
        }
        var effects = new Effects();
        await using var first = Context(); await using var second = Context();
        await Task.WhenAll(Dispatcher(first, effects).DispatchOne(DateTime.UtcNow.AddMinutes(1)), Dispatcher(second, effects).DispatchOne(DateTime.UtcNow.AddMinutes(1)));
        Assert.Equal(notificationId, Assert.Single(effects.Sent));
        await using var verify = Context();
        var completed = await verify.Set<NotificationDelivery>().SingleAsync(item => item.Id == notificationId);
        Assert.NotNull(completed.CompletedAt); Assert.Equal(2, completed.Attempts);
    }
}
