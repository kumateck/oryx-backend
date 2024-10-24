using DOMAIN.Entities.Base;
using DOMAIN.Entities.Users;
using SHARED;

namespace DOMAIN.Entities.Materials.Batch;

public class MaterialBatchEvent : BaseEntity
{
    public Guid BatchId { get; set; }            
    public MaterialBatch Batch { get; set; }     
    public int Quantity { get; set; }     
    public Guid UserId { get; set; }       
    public User User { get; set; } 
    public EventType Type { get; set; }
}

public enum EventType
{
    Supplied,
    Added
}

public class MaterialBatchEventDto 
{
    public EventType Type { get; set; }
    public int Quantity { get; set; }     
    public CollectionItemDto User { get; set; }          
    public DateTime CreatedAt { get; set; }    
}


