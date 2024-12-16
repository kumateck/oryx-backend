using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Countries;
using DOMAIN.Entities.Currencies;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Procurement.Manufacturers;

namespace DOMAIN.Entities.Procurement.Suppliers;

public class Supplier : BaseEntity
{
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
    public List<SupplierManufacturer> AssociatedManufacturers { get; set; } = [];
}

public enum SupplierType
{
    Foreign, 
    Local
}

public class SupplierManufacturer : BaseEntity
{
    public Guid SupplierId { get; set; }
    public Supplier Supplier { get; set; }
    public Guid ManufacturerId { get; set; } 
    public Manufacturer Manufacturer { get; set; }
    public Guid? MaterialId { get; set; }
    public Material Material { get; set; }
}