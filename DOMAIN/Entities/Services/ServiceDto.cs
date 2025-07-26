using DOMAIN.Entities.Attachments;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Users;

namespace DOMAIN.Entities.Services;

public class ServiceDto : WithAttachment
{   public Guid ServiceId { get; set; }
    public string Name { get; set; }
    public string Code { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; } = true;
    public string Description { get; set; }
    public UserDto CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
}