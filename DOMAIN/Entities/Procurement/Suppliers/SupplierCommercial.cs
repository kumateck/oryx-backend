using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Currencies;
using DOMAIN.Entities.Materials;

namespace DOMAIN.Entities.Procurement.Suppliers;

public class SupplierBankDetail : BaseEntity
{
    public Guid SupplierId { get; set; }
    public Supplier Supplier { get; set; }
    [Required, StringLength(255)] public string BankName { get; set; }
    [Required, StringLength(255)] public string AccountNumber { get; set; }
    [Required, StringLength(255)] public string AccountName { get; set; }
    [StringLength(100)] public string SwiftCode { get; set; }
    [StringLength(100)] public string Iban { get; set; }
    [StringLength(1000)] public string BranchAddress { get; set; }
    public Guid CurrencyId { get; set; }
    public Currency Currency { get; set; }
}

public class SupplierPricingAgreement : BaseEntity
{
    public Guid SupplierId { get; set; }
    public Supplier Supplier { get; set; }
    public Guid MaterialId { get; set; }
    public Material Material { get; set; }
    public Guid UoMId { get; set; }
    public UnitOfMeasure UoM { get; set; }
    public decimal AgreedPrice { get; set; }
    [Required, StringLength(100)] public string PriceUoM { get; set; }
    public Guid CurrencyId { get; set; }
    public Currency Currency { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    [StringLength(2000)] public string Notes { get; set; }
}

public class SupplierBankDetailRequest
{
    [Required, StringLength(255)] public string BankName { get; set; }
    [Required, StringLength(255)] public string AccountNumber { get; set; }
    [Required, StringLength(255)] public string AccountName { get; set; }
    [StringLength(100)] public string SwiftCode { get; set; }
    [StringLength(100)] public string Iban { get; set; }
    [StringLength(1000)] public string BranchAddress { get; set; }
    public Guid CurrencyId { get; set; }
}

public class SupplierPricingAgreementRequest
{
    public Guid MaterialId { get; set; }
    public Guid UoMId { get; set; }
    [Range(typeof(decimal), "0.01", "79228162514264337593543950335")]
    public decimal AgreedPrice { get; set; }
    [Required, StringLength(100)] public string PriceUoM { get; set; }
    public Guid CurrencyId { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    [StringLength(2000)] public string Notes { get; set; }
}

public class SupplierBankDetailDto : BaseDto
{
    public Guid SupplierId { get; set; }
    public string BankName { get; set; }
    public string AccountNumber { get; set; }
    public string AccountName { get; set; }
    public string SwiftCode { get; set; }
    public string Iban { get; set; }
    public string BranchAddress { get; set; }
    public CurrencyDto Currency { get; set; }
}

public class SupplierPricingAgreementDto : BaseDto
{
    public Guid SupplierId { get; set; }
    public Guid MaterialId { get; set; }
    public string MaterialName { get; set; }
    public Guid UoMId { get; set; }
    public string UoMName { get; set; }
    public decimal AgreedPrice { get; set; }
    public string PriceUoM { get; set; }
    public CurrencyDto Currency { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public string Notes { get; set; }
}
