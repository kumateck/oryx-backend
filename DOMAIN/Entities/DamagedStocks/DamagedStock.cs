using DOMAIN.Entities.Base;
using DOMAIN.Entities.Items;

namespace DOMAIN.Entities.DamagedStocks;

public class DamagedStock : BaseEntity
{
    public Guid ItemId { get; set; }
    public Item Item { get; set; }
    public DamageStatus DamageStatus {get; set;}
    public int QuantityDamaged { get; set; }
    public string Remarks {get; set;}
    public List<DamagedStockBatch> Batches {get; set;}
    
}

public enum DamageStatus
{
    Damage,
    Missing
}

public class DamagedStockBatch : BaseEntity
{
    public string BatchNumber { get; set; }
    public int Quantity { get; set; }
}