using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Countries;
using DOMAIN.Entities.Materials;
using Microsoft.EntityFrameworkCore;

namespace DOMAIN.Entities.Procurement.Manufacturers;

public class Manufacturer : BaseEntity
{
    [StringLength(100)] public string Name { get; set; }
    [StringLength(1000)] public string Address { get; set; }
    [StringLength(100)] public string Email { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? ValidityDate { get; set; }
    public Guid? CountryId { get; set; }
    public Country Country { get; set; }
    public List<ManufacturerMaterial> Materials { get; set; } = [];
}

public class ManufacturerMaterial : BaseEntity
{
    public Guid ManufacturerId { get; set; }
    public Manufacturer Manufacturer { get; set; }
    public Guid MaterialId { get; set; }
    public Material Material { get; set; }
    public QuantityType QuantityType { get; set; }
    public QuantityPerPackOption QuantityPerPackOption { get; set; }
}

[Owned]
public class QuantityPerPackOption
{
    public QuantityType Type { get; set; }
    public decimal? Min { get; set; }
    public decimal? Max { get; set; }
    public List<decimal> Values { get; set; } = [];
}