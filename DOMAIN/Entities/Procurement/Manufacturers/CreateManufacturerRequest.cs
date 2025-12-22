using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Procurement.Suppliers;

namespace DOMAIN.Entities.Procurement.Manufacturers;

public class CreateManufacturerRequest
{
    [StringLength(100)] public string Name { get; set; }
    [StringLength(1000)] public string Address { get; set; }
    [StringLength(100)] public string Email { get; set; }
    public DateTime? ValidityDate { get; set; }
    public Guid? CountryId { get; set; }
    public List<CreateManufacturerMaterialRequest> Materials { get; set; } = [];
}

public class CreateManufacturerMaterialRequest
{
    public QuantityType QuantityType { get; set; }
    public CreateQuantityPerPackOption QuantityPerPackOption { get; set; }
    public Guid MaterialId { get; set; }
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