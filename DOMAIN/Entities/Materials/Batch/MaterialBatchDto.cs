using DOMAIN.Entities.Base;
using DOMAIN.Entities.Warehouses;
using SHARED;

namespace DOMAIN.Entities.Materials.Batch;

public class MaterialBatchDto
{
    public Guid Id { get; set; }
    public string Code { get; set; }
    public CollectionItemDto UoM { get; set; }
    public BatchStatus Status { get; set; }  
    public DateTime DateReceived { get; set; }
    public DateTime? DateApproved { get; set; }
    public int TotalQuantity { get; set; }        
    public int ConsumedQuantity { get; set; }  
    public int RemainingQuantity { get; set; }
    public DateTime ExpiryDate { get; set; }
    public List<MaterialBatchEventDto> Events { get; set; } = [];
    public List<MaterialBatchMovementDto> Movements { get; set; } = [];
    public List<CurrentLocationDto> Locations { get; set; } = [];
}

public class MaterialBatchEventDto 
{
    public EventType Type { get; set; }
    public int Quantity { get; set; }     
    public CollectionItemDto User { get; set; }          
    public DateTime CreatedAt { get; set; }    
}

public class MaterialBatchMovementDto : BaseDto
{
    public CollectionItemDto Batch { get; set; }
    public CollectionItemDto FromLocation { get; set; }
    public CollectionItemDto ToLocation { get; set; }
    public int Quantity { get; set; }
    public DateTime MovedAt { get; set; }
    public CollectionItemDto MovedBy { get; set; }
    public MovementType MovementType { get; set; }  
}

public class CurrentLocationDto
{
    public string LocationName { get; set; }
    public int QuantityAtLocation { get; set; }
}