using DOMAIN.Entities.Users;

namespace DOMAIN.Entities.Notifications;

public class NotificationDto
{
    public string Message { get; set; }
    public List<UserDto> Recipients { get; set; } = [];
}