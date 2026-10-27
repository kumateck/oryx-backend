using APP.Repository;
using APP.Services.NotificationService;
using DOMAIN.Entities.Alerts;
using DOMAIN.Entities.Notifications;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Notifications;
public class NotificationStateTests
{
    private sealed class Actor : ICurrentUserService
    {
        public Guid? UserId => null;
        public Guid? DepartmentId => null;
        public string DepartmentType => "";
    }
    private static ApplicationDbContext Context() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, new Actor());
    [Fact]
    public async Task List_excludes_email_and_other_users_and_preserves_sent_time()
    {
        await using var context = Context(); var user = Guid.NewGuid();
        var at = DateTime.UtcNow.AddDays(-2);
        context.Notifications.AddRange(
            new Notification { Id = Guid.NewGuid(), Message = "Mine", Recipients = [user], SentAt = at, AlertType = AlertType.InApp },
            new Notification { Id = Guid.NewGuid(), Message = "Email", Recipients = [user], AlertType = AlertType.Email },
            new Notification { Id = Guid.NewGuid(), Message = "Other user", Recipients = [Guid.NewGuid()], AlertType = AlertType.InApp });
        await context.SaveChangesAsync();
        var result = await new AlertRepository(context, null!, null!, null!).GetNotificationsForUser(user, true);
        Assert.True(result.IsSuccess); Assert.Equal(at, Assert.Single(result.Value).SentAt);
    }
    [Fact]
    public async Task Read_and_targeted_sync_are_saved_together_and_another_user_is_refused()
    {
        await using var context = Context(); var user = Guid.NewGuid();
        var item = new Notification { Id = Guid.NewGuid(), Message = "Mine", Recipients = [user], AlertType = AlertType.InApp };
        context.Notifications.Add(item); await context.SaveChangesAsync();
        var repository = new AlertRepository(context, null!, null!, null!);
        Assert.False((await repository.MarkNotificationAsRead(item.Id, Guid.NewGuid())).IsSuccess);
        Assert.Empty(context.NotificationReads); Assert.Empty(context.Set<NotificationDelivery>());
        Assert.True((await repository.MarkNotificationAsRead(item.Id, user)).IsSuccess);
        var read = Assert.Single(context.NotificationReads); var delivery = Assert.Single(context.Set<NotificationDelivery>());
        Assert.Equal(read.Id, delivery.Id); Assert.Equal(user, delivery.RecipientId);
        Assert.Equal(item.Id, delivery.NotificationId); Assert.Equal(NotificationDeliveryKind.Read, delivery.Kind);
        Assert.Empty((await repository.GetNotificationsForUser(user, true)).Value);
    }
    [Fact]
    public async Task Missing_alert_configuration_is_retained_for_retry()
    {
        await using var context = Context();
        new AlertRequestStager(context).Stage("Expiry requires review", NotificationType.ExpiredMaterial);
        var dispatcher = new AlertRequestDispatcher(context, null!, NullLogger<AlertRequestDispatcher>.Instance);
        Assert.True(await dispatcher.DispatchOne(DateTime.UtcNow, CancellationToken.None));
        var intent = Assert.Single(context.Set<AlertDispatchRequest>());
        Assert.Null(intent.CompletedAt); Assert.NotNull(intent.LastErrorCode); Assert.Equal(1, intent.Attempts);
        Assert.Empty(context.Notifications);
    }
}
