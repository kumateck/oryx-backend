using SHARED;

namespace DOMAIN.Entities.Base;

public class BaseDto
{
    public Guid Id { get; set; }
    public CollectionItemDto CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
}