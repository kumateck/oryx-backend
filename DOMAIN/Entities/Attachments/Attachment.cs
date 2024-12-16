using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;

namespace DOMAIN.Entities.Attachments;

public class Attachment : BaseEntity
{
    public Guid ModelId { get; set; }
    [StringLength(255)] public string ModelType { get; set; }
    [StringLength(255)] public string Reference { get; set; }
    [StringLength(255)] public string Name { get; set; }
}