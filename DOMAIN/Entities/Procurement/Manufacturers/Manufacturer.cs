using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Materials;

namespace DOMAIN.Entities.Procurement.Manufacturers;

public class Manufacturer : BaseEntity
{
    [StringLength(100)] public string Name { get; set; }
    [StringLength(1000)] public string Address { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? ValidityDate { get; set; }
    public List<ManufacturerMaterial> Materials { get; set; } = [];
}

public class ManufacturerMaterial : BaseEntity
{
    public Guid ManufacturerId { get; set; }
    public Manufacturer Manufacturer { get; set; }
    public Guid MaterialId { get; set; }
    public Material Material { get; set; }
}