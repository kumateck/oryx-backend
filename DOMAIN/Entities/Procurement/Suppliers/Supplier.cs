using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Countries;
using DOMAIN.Entities.Currencies;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Procurement.Manufacturers;

namespace DOMAIN.Entities.Procurement.Suppliers;

public class Supplier : BaseEntity
{
    // Vendor belongs to the parallel store-procurement subsystem. Consolidating
    // Vendor and Supplier is a deliberate future migration, not an SRM side effect.
    [StringLength(100)] public string Name { get; set; }
    [StringLength(100)] public string Email { get; set; }
    [StringLength(1000)] public string Address { get; set; }
    [StringLength(1000)] public string ContactPerson { get; set; }
    [StringLength(20)] public string ContactNumber { get; set; }
    public Guid? CountryId { get; set; }
    public Country Country { get; set; }
    public Guid? CurrencyId { get; set; }
    public Currency Currency { get; set; }
    public SupplierType Type { get; set; }
    public SupplierStatus Status { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? RequalificationDueDate { get; set; }
    public List<SupplierManufacturer> AssociatedManufacturers { get; set; } = [];
    public List<SupplierCertification> Certifications { get; set; } = [];
    public List<SupplierContact> Contacts { get; set; } = [];
    public List<SupplierBankDetail> BankDetails { get; set; } = [];
    public List<SupplierPricingAgreement> PricingAgreements { get; set; } = [];
    public List<SupplierPerformanceRecord> PerformanceRecords { get; set; } = [];
}

public enum SupplierType
{
    Foreign,
    Local
}

public enum SupplierStatus
{
    New = 0,
    Approved = 1,
    Rejected = 2
}

public class SupplierManufacturer : BaseEntity
{
    public Guid SupplierId { get; set; }
    public Supplier Supplier { get; set; }
    public Guid ManufacturerId { get; set; }
    public Manufacturer Manufacturer { get; set; }
    public Guid? MaterialId { get; set; }
    public Material Material { get; set; }
    public Guid? UoMId { get; set; }
    public UnitOfMeasure UoM { get; set; }
    public bool Default { get; set; }
}
