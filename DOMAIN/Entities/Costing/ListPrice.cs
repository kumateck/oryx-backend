using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Currencies;

namespace DOMAIN.Entities.Costing;

/// <summary>A product's current list price: the price quotations, orders and invoices fall back to
/// when the customer has no pricing agreement for it.</summary>
public class ProductListPriceDto
{
    public Guid ProductId { get; set; }
    public string ProductCode { get; set; }
    public string ProductName { get; set; }
    public Guid? DepartmentId { get; set; }
    public decimal Price { get; set; }
    public string PriceUoM { get; set; }
    /// <summary>The currency the price is in; null only when no currency could be recorded.</summary>
    public CurrencyDto Currency { get; set; }
    /// <summary>When the current price was set; null when the product was never priced.</summary>
    public DateTime? PricedAt { get; set; }
}

/// <summary>One recorded list price. A product keeps every price it has had.</summary>
public class ListPriceHistoryDto
{
    public decimal Price { get; set; }
    public string PriceUoM { get; set; }
    public CurrencyDto Currency { get; set; }
    public DateTime Date { get; set; }
}

/// <summary>Sets a product's list price from now on, in the currency chosen. Nothing is converted.</summary>
public class SetListPriceRequest
{
    [Range(typeof(decimal), "0.01", "79228162514264337593543950335", ErrorMessage = "The price must be above zero.")]
    public decimal Price { get; set; }

    [StringLength(100)] public string PriceUoM { get; set; }

    [Required] public Guid CurrencyId { get; set; }
}
