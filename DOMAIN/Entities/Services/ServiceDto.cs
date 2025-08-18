using DOMAIN.Entities.Attachments;

namespace DOMAIN.Entities.Services;

public class ServiceDto : WithAttachment
{  
    public string Name { get; set; }
    public string Code { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; }
    public string Description { get; set; }
}