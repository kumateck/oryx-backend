using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Procurement.Manufacturers;

namespace DOMAIN.Entities.Procurement.Suppliers;

public class Supplier : BaseEntity
{
    [StringLength(100)] public string Name { get; set; }
    [StringLength(1000)] public string ContactPerson { get; set; }
    [StringLength(20)] public string ContactNumber { get; set; }
    public List<SupplierManufacturer> AssociatedManufacturers { get; set; } = [];
}

public class SupplierManufacturer : BaseEntity
{
    public Guid SupplierId { get; set; }
    public Supplier Supplier { get; set; }
    public Guid ManufacturerId { get; set; } 
    public Manufacturer Manufacturer { get; set; }
}