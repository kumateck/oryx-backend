using DOMAIN.Entities.Base;

namespace DOMAIN.Entities.Attachments;

public class WithAttachment : BaseDto
{
    public IEnumerable<AttachmentDto> Attachments { get; set; }
}