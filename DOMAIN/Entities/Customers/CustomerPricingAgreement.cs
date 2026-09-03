using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Currencies;
using DOMAIN.Entities.Products;

namespace DOMAIN.Entities.Customers;

public class CustomerPricingAgreement : BaseEntity
{
    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; }
    public Guid ProductId { get; set; }
    public Product Product { get; set; }
    public Guid ProductPackingId { get; set; }
    public ProductPacking ProductPacking { get; set; }
    public decimal AgreedPrice { get; set; }
    public Guid CurrencyId { get; set; }
    public Currency Currency { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    [StringLength(2000)] public string Notes { get; set; }
}

public class CustomerPricingAgreementRequest
{
    public Guid ProductId { get; set; }
    public Guid ProductPackingId { get; set; }
    [Range(typeof(decimal), "0.01", "79228162514264337593543950335")]
    public decimal AgreedPrice { get; set; }
    public Guid CurrencyId { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    [StringLength(2000)] public string Notes { get; set; }
}

public class CustomerPricingAgreementDto : BaseDto
{
    public Guid CustomerId { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; }
    public Guid ProductPackingId { get; set; }
    public string ProductPackingName { get; set; }
    public decimal AgreedPrice { get; set; }
    public CurrencyDto Currency { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public string Notes { get; set; }
}
