using DOMAIN.Entities.Users;

namespace DOMAIN.Entities.Base;

public class BaseDto
{
    public Guid Id { get; set; }
    public UserDto CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
}