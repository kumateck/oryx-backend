using APP.Services.NotificationService;
using DOMAIN.Entities.Alerts;
using DOMAIN.Entities.Notifications;
using DOMAIN.Entities.Users;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Notifications;
public class AlertRecipientTests
{
    private sealed class Actor : ICurrentUserService
    {
        public Guid? UserId => null;
        public Guid? DepartmentId => null;
        public string DepartmentType => "";
    }
    private sealed class Capture : INotificationService
    {
        public List<(NotificationDto Event, List<AlertType> Channels)> Sent { get; } = [];
        public Task SendNotification(NotificationDto notification, List<AlertType> alertTypes) { Sent.Add((notification, alertTypes)); return Task.CompletedTask; }
    }
    private static ApplicationDbContext Context() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, new Actor());
    private static User User(Guid? department = null, bool disabled = false) => new() {
        Id = Guid.NewGuid(), UserName = Guid.NewGuid().ToString(), FirstName = "Reviewer", LastName = "One", DepartmentId = department, IsDisabled = disabled,
    };
    private static Alert Rule(params User[] users) => new() {
        Title = "Review", NotificationType = NotificationType.BmrBprRequested, AlertTypes = [AlertType.InApp],
        Users = users.Select(user => new AlertUser { UserId = user.Id, User = user }).ToList(),
    };
    [Fact]
    public async Task Every_active_configuration_contributes_without_duplicate_recipients()
    {
        await using var context = Context();
        var first = User(); var second = User(); var disabled = User(disabled: true);
        context.Users.AddRange(first, second, disabled); context.Alerts.AddRange(Rule(first), Rule(first, second, disabled));
        await context.SaveChangesAsync(); var capture = new Capture();
        await new AlertProcessor(context, null!, capture).Process("Actual event", NotificationType.BmrBprRequested, null, []);
        var sent = Assert.Single(capture.Sent);
        Assert.Equal(2, sent.Event.Recipients.Count);
        Assert.Contains(sent.Event.Recipients, user => user.Id == first.Id);
        Assert.Contains(sent.Event.Recipients, user => user.Id == second.Id);
    }
    [Fact]
    public async Task Department_scope_is_retained_and_explicit_assignees_are_not_lost()
    {
        await using var context = Context(); var department = Guid.NewGuid();
        var local = User(department); var elsewhere = User(Guid.NewGuid()); var assigned = User(Guid.NewGuid());
        context.Users.AddRange(local, elsewhere, assigned); context.Alerts.Add(Rule(local, elsewhere));
        await context.SaveChangesAsync(); var capture = new Capture();
        await new AlertProcessor(context, null!, capture).Process("Assigned work", NotificationType.BmrBprRequested, department, [assigned]);
        var recipients = Assert.Single(capture.Sent).Event.Recipients;
        Assert.Contains(recipients, user => user.Id == local.Id);
        Assert.Contains(recipients, user => user.Id == assigned.Id);
        Assert.DoesNotContain(recipients, user => user.Id == elsewhere.Id);
    }
}
