using DOMAIN.Entities.Base;
using DOMAIN.Entities.Users;
using SHARED;

namespace DOMAIN.Entities.Materials.Batch;

public class MaterialBatchConsumption : BaseEntity
{
    public Guid BatchId { get; set; }            
    public MaterialBatch Batch { get; set; }     
    public int QuantityConsumed { get; set; }     
    public Guid ConsumedById { get; set; }       
    public User ConsumedBy { get; set; }          
    public DateTime DateConsumed { get; set; }    
}

public class MaterialBatchConsumptionDto 
{
    public Guid Id { get; set; }
    public MaterialBatchDto Batch { get; set; }     
    public int QuantityConsumed { get; set; }     
    public CollectionItemDto ConsumedBy { get; set; }          
    public DateTime DateConsumed { get; set; }    
}


