using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Currencies;

namespace DOMAIN.Entities.Charges;

public class Charge : BaseEntity
{
    [StringLength(100)] public string Name { get; set; }
    [StringLength(100)]public string Description { get; set; }
    public Guid? CurrencyId { get; set; }
    public Currency Currency { get; set; }
    public decimal Amount { get; set; }
}

public class ChargeDto 
{
    public string Name { get; set; }
    public string Description { get; set; }
    public CurrencyDto Currency { get; set; }
    public decimal Amount { get; set; }
}

public class CreateChargeRequest
{
    public Guid? Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public Guid? CurrencyId { get; set; }
    public decimal Amount { get; set; }
}