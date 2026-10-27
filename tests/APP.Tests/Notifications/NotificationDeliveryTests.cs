using APP.Services.NotificationService;
using DOMAIN.Entities.Alerts;
using DOMAIN.Entities.Notifications;
using DOMAIN.Entities.Users;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Notifications;

public class NotificationDeliveryTests
{
    private sealed class Actor : ICurrentUserService
    {
        public Guid? UserId => null;
        public Guid? DepartmentId => null;
        public string DepartmentType => "";
    }
    private sealed class Effects : INotificationDeliveryEffects
    {
        public List<NotificationDto> Published { get; } = [];
        public bool FailPublish { get; set; }
        public bool FailAudit { get; set; }
        public Task Publish(NotificationDto notification, CancellationToken cancellationToken)
        {
            if (FailPublish) throw new InvalidOperationException("broker unavailable");
            Published.Add(notification);
            return Task.CompletedTask;
        }
        public Task Read(NotificationReadEvent notification, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task Email(NotificationDto notification, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task Audit(Guid notificationId, Guid recipientId, CancellationToken cancellationToken) =>
            FailAudit ? Task.FromException(new InvalidOperationException("audit unavailable")) : Task.CompletedTask;
    }
    private static ApplicationDbContext Context(string database) => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(database).Options, new Actor());
    private static NotificationDeliveryDispatcher Dispatcher(ApplicationDbContext context, Effects effects) =>
        new(context, effects, NullLogger<NotificationDeliveryDispatcher>.Instance);
    private static async Task<Guid> Seed(ApplicationDbContext context)
    {
        var user = new User { Id = Guid.NewGuid(), FirstName = "Reviewer", LastName = "One", UserName = Guid.NewGuid().ToString() };
        context.Users.Add(user);
        await context.SaveChangesAsync();
        var dto = new NotificationDto { Message = "Review required", Type = NotificationType.BmrBprRequested, Recipients = [new UserDto { Id = user.Id }] };
        await new NotificationService(context).SendNotification(dto, [AlertType.InApp, AlertType.InApp]);
        return dto.Id;
    }
    [Fact]
    public async Task Save_precedes_publish_and_identity_survives_dispatch()
    {
        await using var context = Context(Guid.NewGuid().ToString());
        var id = await Seed(context);
        var stored = Assert.Single(context.Notifications);
        Assert.Equal(id, stored.Id);
        Assert.Single(context.Set<NotificationDelivery>());
        var effects = new Effects();
        Assert.True(await Dispatcher(context, effects).DispatchOne(DateTime.UtcNow));
        var sent = Assert.Single(effects.Published);
        Assert.Equal(id, sent.Id);
        Assert.Equal(stored.SentAt, sent.SentAt);
        Assert.Equal(stored.Recipients, sent.Recipients.Select(user => user.Id));
        Assert.NotNull(Assert.Single(context.Set<NotificationDelivery>()).CompletedAt);
        Assert.False(await Dispatcher(context, effects).DispatchOne(DateTime.UtcNow));
    }
    [Fact]
    public async Task Broker_failure_keeps_unread_and_retries_after_restart()
    {
        var db = Guid.NewGuid().ToString();
        Guid id;
        await using (var context = Context(db))
        {
            id = await Seed(context);
            await Dispatcher(context, new Effects { FailPublish = true }).DispatchOne(DateTime.UtcNow);
            Assert.Null(Assert.Single(context.Set<NotificationDelivery>()).CompletedAt);
            Assert.Equal(id, Assert.Single(context.Notifications).Id);
        }
        await using var restarted = Context(db);
        var effects = new Effects();
        await Dispatcher(restarted, effects).DispatchOne(DateTime.UtcNow.AddMinutes(1));
        Assert.Equal(id, Assert.Single(effects.Published).Id);
        Assert.Equal(2, Assert.Single(restarted.Set<NotificationDelivery>()).Attempts);
    }
    [Fact]
    public async Task Audit_failure_does_not_republish_the_notification()
    {
        await using var context = Context(Guid.NewGuid().ToString());
        await Seed(context);
        var effects = new Effects { FailAudit = true };
        await Dispatcher(context, effects).DispatchOne(DateTime.UtcNow);
        Assert.NotNull(Assert.Single(context.Set<NotificationDelivery>()).PublishedAt);
        effects.FailAudit = false;
        await Dispatcher(context, effects).DispatchOne(DateTime.UtcNow.AddMinutes(1));
        Assert.Single(effects.Published);
        Assert.NotNull(Assert.Single(context.Set<NotificationDelivery>()).CompletedAt);
    }
    [Fact]
    public async Task Disabled_recipient_is_not_published()
    {
        await using var context = Context(Guid.NewGuid().ToString());
        await Seed(context);
        Assert.Single(context.Users).IsDisabled = true;
        await context.SaveChangesAsync();
        var effects = new Effects();
        await Dispatcher(context, effects).DispatchOne(DateTime.UtcNow);
        Assert.Empty(effects.Published);
        Assert.Equal("NoActiveRecipients", Assert.Single(context.Set<NotificationDelivery>()).LastErrorCode);
    }
    [Fact]
    public async Task Active_lease_is_not_stolen_and_expired_lease_can_be_recovered()
    {
        await using var context = Context(Guid.NewGuid().ToString());
        await Seed(context);
        var row = Assert.Single(context.Set<NotificationDelivery>());
        row.LeaseUntil = DateTime.UtcNow.AddMinutes(2);
        await context.SaveChangesAsync();
        var effects = new Effects();
        Assert.False(await Dispatcher(context, effects).DispatchOne(DateTime.UtcNow));
        Assert.True(await Dispatcher(context, effects).DispatchOne(DateTime.UtcNow.AddMinutes(3)));
        Assert.Single(effects.Published);
    }
    [Fact]
    public async Task Channel_records_and_work_are_atomic_and_email_is_separate()
    {
        await using var context = Context(Guid.NewGuid().ToString());
        var userId = Guid.NewGuid();
        var dto = new NotificationDto { Message = "A real event", Recipients = [new UserDto { Id = userId }, new UserDto { Id = userId }] };
        await new NotificationService(context).SendNotification(dto, [AlertType.Email, AlertType.InApp, AlertType.Email]);
        Assert.Equal(2, context.Notifications.Count());
        Assert.Equal(2, context.Set<NotificationDelivery>().Count());
        Assert.All(context.Notifications, item => Assert.Single(item.Recipients));
        Assert.Equal(dto.Id, context.Notifications.Single(item => item.AlertType == AlertType.InApp).Id);
    }
    [Fact]
    public async Task Pending_business_changes_stage_without_an_early_commit()
    {
        var db = Guid.NewGuid().ToString();
        await using var context = Context(db);
        context.Users.Add(new User { Id = Guid.NewGuid(), UserName = "new-user", FirstName = "A", LastName = "B" });
        new AlertRequestStager(context).Stage("Review required", NotificationType.BmrBprRequested, Guid.NewGuid());
        await using (var observer = Context(db))
        {
            Assert.Empty(observer.Users);
            Assert.Empty(observer.Set<AlertDispatchRequest>());
        }
        await context.SaveChangesAsync();
        await using var committed = Context(db);
        Assert.Single(committed.Users);
        Assert.Single(committed.Set<AlertDispatchRequest>());
    }
    [Fact]
    public async Task Post_save_intent_is_persisted_with_department_and_assignees()
    {
        var db = Guid.NewGuid().ToString(); var department = Guid.NewGuid(); var user = new User { Id = Guid.NewGuid() };
        await using (var context = Context(db)) new AlertRequestStager(context).Stage("Assigned", NotificationType.ShiftAssigned, department, [user, user]);
        await using var restarted = Context(db);
        var intent = Assert.Single(restarted.Set<AlertDispatchRequest>());
        Assert.Equal(department, intent.DepartmentId);
        Assert.Equal(user.Id, Assert.Single(intent.AssignedUserIds));
    }

}
