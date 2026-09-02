using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;

namespace DOMAIN.Entities.Currencies;

public class ExchangeRate : BaseEntity
{
    public Guid CurrencyId { get; set; }
    public Currency Currency { get; set; }
    public decimal RateToBase { get; set; }
    public DateTime EffectiveDate { get; set; }
}

public class CreateExchangeRateRequest
{
    public Guid CurrencyId { get; set; }
    [Range(typeof(decimal), "0.0000000001", "79228162514264337593543950335")]
    public decimal RateToBase { get; set; }
    public DateTime EffectiveDate { get; set; }
}

public class ExchangeRateDto : BaseDto
{
    public CurrencyDto Currency { get; set; }
    public decimal RateToBase { get; set; }
    public DateTime EffectiveDate { get; set; }
}
