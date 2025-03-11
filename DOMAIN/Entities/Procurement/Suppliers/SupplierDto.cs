using DOMAIN.Entities.Base;
using DOMAIN.Entities.Countries;
using DOMAIN.Entities.Currencies;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Procurement.Manufacturers;

namespace DOMAIN.Entities.Procurement.Suppliers;

public class SupplierDto : BaseDto
{
    public string Name { get; set; }
    public string Email { get; set; }
    public string Address { get; set; }
    public string ContactPerson { get; set; }
    public string ContactNumber { get; set; }
    public CountryDto Country { get; set; }
    public CurrencyDto Currency { get; set; }
    public SupplierType Type { get; set; }
    public SupplierStatus Status { get; set; }
    public List<SupplierManufacturerDto> AssociatedManufacturers { get; set; } = [];
}

public class SupplierManufacturerDto : BaseDto
{
    public ManufacturerDto Manufacturer { get; set; }
    public MaterialDto Material { get; set; }
    public decimal QuantityPerPack { get; set; }
}