using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Notifications;
using DOMAIN.Entities.Roles;
using DOMAIN.Entities.Users;

namespace DOMAIN.Entities.Alerts;

public class CreateAlertRequest
{
    public string Title { get; set; }
    public NotificationType NotificationType { get; set; }
    public AlertType AlertType { get; set; }
    public TimeSpan TimeFrame { get; set; }
}

public class Alert : BaseEntity
{
    [StringLength(255)]
    public string Title { get; set; }
    [StringLength(255)]
    public NotificationType NotificationType { get; set; }
    public List<AlertType> AlertTypes { get; set; } = [];
    public List<AlertRole> Roles { get; set; } = [];
    public List<AlertUser> Users { get; set; } = [];
    public TimeSpan TimeFrame { get; set; }
    public bool IsConfigurable { get; set; }
    public bool IsDisabled { get; set; }
}

public class AlertRole
{
    public AlertRole()
    {
    }

    public AlertRole(Guid roleId)
    {
        RoleId = roleId;
    }

    public Guid Id { get; set; }
    public Guid RoleId { get; set; }
    public Role Role { get; set; }
}

public class AlertUser
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; }
}

public enum AlertType
{
    Email, 
    InApp,
    Combination
}


public class AlertDto : BaseDto
{
    public string Title { get; set; }
    public string ModelType { get; set; }
    public AlertType AlertType { get; set; }
    public TimeSpan TimeFrame { get; set; }
    public bool IsDisabled { get; set; }
}