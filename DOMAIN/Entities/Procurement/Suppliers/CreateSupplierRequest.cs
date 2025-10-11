using Microsoft.EntityFrameworkCore;

namespace DOMAIN.Entities.Procurement.Suppliers;

public class CreateSupplierRequest
{
    public string Name { get; set; }
    public string Email { get; set; }
    public string Address { get; set; }
    public string ContactPerson { get; set; }
    public string ContactNumber { get; set; }
    public Guid? CountryId { get; set; }
    public Guid? CurrencyId { get; set; }
    public SupplierType Type { get; set; }
    public List<CreateSupplierManufacturerRequest> AssociatedManufacturers { get; set; } = [];
}

public class CreateSupplierManufacturerRequest
{
    public Guid ManufacturerId { get; set; }
    public Guid? MaterialId { get; set; }
    public QuantityType QuantityType { get; set; }
    public CreateQuantityPerPackOption QuantityPerPackOption { get; set; }
    public bool Default { get; set; }
}

public class CreateQuantityPerPackOption
{
    public QuantityType Type { get; set; }
    public decimal? Min { get; set; }
    public decimal? Max { get; set; }
    public List<decimal> Values { get; set; } = [];
}

public enum QuantityType
{
    Single = 0,
    Range = 1,
    List = 2
}