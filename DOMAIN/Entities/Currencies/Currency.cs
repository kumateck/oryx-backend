using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;

namespace DOMAIN.Entities.Currencies;

public class Currency : BaseEntity
{
    [StringLength(255)] public string Name { get; set; }
    [StringLength(255)] public string Symbol { get; set; }
    [StringLength(1000)] public string Description { get; set; }
}

public class CurrencyDto
{
    public string Name { get; set; }
    public string Symbol { get; set; }
    public string Description { get; set; }
}