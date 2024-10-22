using DOMAIN.Entities.Warehouses;
using SHARED;

namespace DOMAIN.Entities.Materials.Batch;

public class MaterialBatchDto
{
    public string Code { get; set; }
    public MaterialDto Material { get; set; }
    public int Quantity { get; set; }
    public CollectionItemDto UoM { get; set; }
    public BatchStatus Status { get; set; }  
    public DateTime DateReceived { get; set; }
    public DateTime? DateApproved { get; set; }
    public Guid WarehouseId { get; set; }  
    public WarehouseDto Warehouse { get; set; }
    public List<MaterialBatchEventDto> Events { get; set; } = [];
}