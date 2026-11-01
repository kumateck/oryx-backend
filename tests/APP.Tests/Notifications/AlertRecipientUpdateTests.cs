using APP.Repository;
using DOMAIN.Entities.Alerts;
using DOMAIN.Entities.Notifications;
using DOMAIN.Entities.Roles;
using DOMAIN.Entities.Users;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using SHARED.Services.Identity;
using Xunit;

namespace APP.Tests.Notifications;

public class AlertRecipientUpdateTests
{
    private sealed class Actor : ICurrentUserService
    {
        public Guid? UserId => null;
        public Guid? DepartmentId => null;
        public string DepartmentType => "";
    }

    private static ApplicationDbContext Context() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, new Actor());

    private static AlertRepository Repository(ApplicationDbContext context) => new(context, null!, null!, null!);

    private static User User() => new() { Id = Guid.NewGuid(), UserName = Guid.NewGuid().ToString(), FirstName = "Ada", LastName = "Mensah" };

    private static Role Role(string name) => new() { Id = Guid.NewGuid(), Name = name, NormalizedName = name.ToUpperInvariant() };

    private static Alert SystemAlert(Role role) => new()
    {
        Title = "Shipment has arrived", NotificationType = NotificationType.ShipmentArrived, AlertTypes = [AlertType.InApp],
        IsConfigurable = false, TimeFrame = TimeSpan.Zero, Roles = [new AlertRole { RoleId = role.Id }],
    };

    [Fact]
    public async Task System_alert_recipients_can_be_replaced_while_delivery_stays_fixed()
    {
        await using var context = Context();
        var warehouse = Role("Warehouse Manager"); var quality = Role("QA Manager"); var reviewer = User();
        context.Roles.AddRange(warehouse, quality); context.Users.Add(reviewer);
        var alert = SystemAlert(warehouse); context.Alerts.Add(alert);
        await context.SaveChangesAsync();

        var result = await Repository(context).UpdateAlertRecipients(
            new UpdateAlertRecipientsRequest { RoleIds = [quality.Id, quality.Id], UserIds = [reviewer.Id] }, alert.Id);

        Assert.True(result.IsSuccess);
        context.ChangeTracker.Clear();
        var saved = await context.Alerts.Include(item => item.Roles).Include(item => item.Users).SingleAsync();
        Assert.Equal([quality.Id], saved.Roles.Select(item => item.RoleId));
        Assert.Equal([reviewer.Id], saved.Users.Select(item => item.UserId));
        Assert.False(saved.IsConfigurable);
        Assert.Equal("Shipment has arrived", saved.Title);
        Assert.Equal([AlertType.InApp], saved.AlertTypes);
        Assert.Single(context.AlertRoles);
    }

    [Fact]
    public async Task Recipients_can_be_cleared()
    {
        await using var context = Context();
        var warehouse = Role("Warehouse Manager"); context.Roles.Add(warehouse);
        var alert = SystemAlert(warehouse); context.Alerts.Add(alert);
        await context.SaveChangesAsync();

        var result = await Repository(context).UpdateAlertRecipients(new UpdateAlertRecipientsRequest(), alert.Id);

        Assert.True(result.IsSuccess);
        Assert.Empty(context.AlertRoles);
        Assert.Empty(context.AlertUsers);
    }

    [Fact]
    public async Task Unknown_recipients_are_rejected_and_nothing_changes()
    {
        await using var context = Context();
        var warehouse = Role("Warehouse Manager"); context.Roles.Add(warehouse);
        var alert = SystemAlert(warehouse); context.Alerts.Add(alert);
        await context.SaveChangesAsync();
        var repository = Repository(context);

        var role = await repository.UpdateAlertRecipients(new UpdateAlertRecipientsRequest { RoleIds = [Guid.NewGuid()] }, alert.Id);
        var user = await repository.UpdateAlertRecipients(new UpdateAlertRecipientsRequest { UserIds = [Guid.NewGuid()] }, alert.Id);

        Assert.Equal("Alert.UnknownRole", role.Error.Code);
        Assert.Equal("Alert.UnknownUser", user.Error.Code);
        context.ChangeTracker.Clear();
        Assert.Equal([warehouse.Id], context.AlertRoles.Select(item => item.RoleId));
    }

    [Fact]
    public async Task A_missing_alert_is_not_found()
    {
        await using var context = Context();
        var result = await Repository(context).UpdateAlertRecipients(new UpdateAlertRecipientsRequest(), Guid.NewGuid());
        Assert.Equal("Alert.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task A_full_update_of_a_system_alert_is_still_refused()
    {
        await using var context = Context();
        var warehouse = Role("Warehouse Manager"); context.Roles.Add(warehouse);
        var alert = SystemAlert(warehouse); context.Alerts.Add(alert);
        await context.SaveChangesAsync();
        var result = await Repository(context).UpdateAlert(new CreateAlertRequest { Title = "Renamed", AlertTypes = [] }, Guid.NewGuid(), alert.Id);
        Assert.Equal("Alert.IsConfigurable", result.Error.Code);
    }
}
