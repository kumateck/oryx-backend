using DOMAIN.Entities.Base;
using DOMAIN.Entities.Materials.Batch;
using DOMAIN.Entities.ProductionSchedules;
using DOMAIN.Entities.Products;
using SHARED;

namespace DOMAIN.Entities.Materials;

public class MaterialReturnNote : BaseEntity
{
    public Guid MaterialBatchId { get; set; }
    public MaterialBatch MaterialBatch { get; set; }
    public DateTime ReturnDate { get; set; }
    public Guid ProductionScheduleId { get; set; }
    public ProductionSchedule ProductionSchedule { get; set; }
    public Guid ProductId { get; set; }
    public Product Product { get; set; }
    public MaterialReturnStatus Status { get; set; }
}

public class MaterialReturnNoteDto : BaseDto
{
    public CollectionItemDto MaterialBatch { get; set; }
    public DateTime ReturnDate { get; set; }
    public CollectionItemDto ProductionSchedule { get; set; }
    public CollectionItemDto Product { get; set; }
    public MaterialReturnStatus Status { get; set; }
}

public enum MaterialReturnStatus
{
    Pending, 
    Completed
}