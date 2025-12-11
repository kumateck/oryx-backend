using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Items;

namespace DOMAIN.Entities.JobRequests;

/// <summary>
/// Tracks items/materials consumed during service execution
/// </summary>
public class ConsumedItem : BaseEntity
{
    public Guid? JobExecutionId { get; set; }
    public JobExecution JobExecution { get; set; }
    
    public Guid? JobOrderExecutionId { get; set; }
    public JobOrderExecution JobOrderExecution { get; set; }
    
    public Guid? QuotationItemId { get; set; }
    public QuotationItem QuotationItem { get; set; }
    
    public Guid ItemId { get; set; }
    public Item Item { get; set; }
    
    public decimal QuantityConsumed { get; set; }
    
    public Guid UnitOfMeasureId { get; set; }
    public UnitOfMeasure UnitOfMeasure { get; set; }
    
    [StringLength(1000)]
    public string Notes { get; set; }
    
    public ItemSource Source { get; set; } = ItemSource.FromStock;
}

public enum ItemSource
{
    FromStock,
    RequestedForService,
    FromQuotation
}

