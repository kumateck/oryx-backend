using System.ComponentModel.DataAnnotations;

namespace DOMAIN.Entities.DamagedStocks;

public class CreateDamagedStockRequest
{
    [Required] public Guid ItemId { get; set; }
    [Required] public DamageStatus DamageStatus {get; set;}
    public string Remarks {get; set;}
}