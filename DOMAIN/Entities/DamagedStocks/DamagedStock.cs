using DOMAIN.Entities.Base;
using DOMAIN.Entities.Items;
using DOMAIN.Entities.Users;

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

public class DamagedStocksLog : BaseEntity
{
    public Guid DamagedStockId { get; set; }
    public DamagedStock DamagedStock { get; set; }
    
    public Guid UserId { get; set; }
    public User User { get; set; }
    public DateTime TimeStamp { get; set; }
}

public class DamagedStockLogDto : BaseDto
{
    public Guid DamagedStockId { get; set; }
    public DamagedStockDto DamagedStock { get; set; }
    public DateTime TimeStamp { get; set; }
    public Guid UserId { get; set; }
    public UserDto User { get; set; }
}