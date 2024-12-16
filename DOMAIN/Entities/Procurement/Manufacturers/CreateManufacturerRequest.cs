using System.ComponentModel.DataAnnotations;

namespace DOMAIN.Entities.Procurement.Manufacturers;

public class CreateManufacturerRequest
{
    [StringLength(100)] public string Name { get; set; }
    [StringLength(1000)] public string Address { get; set; }
    [StringLength(100)]  public string Email { get; set; }
    public DateTime? ValidityDate { get; set; }
    public Guid? CountryId { get; set; }
    public List<CreateManufacturerMaterialRequest> Materials { get; set; } = [];
}

public class CreateManufacturerMaterialRequest
{
    public Guid MaterialId { get; set; }
}