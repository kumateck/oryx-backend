using DOMAIN.Entities.Base;
using DOMAIN.Entities.Materials;
using SHARED;

namespace DOMAIN.Entities.Procurement.Manufacturers;

public class ManufacturerDto : BaseDto
{ 
    public string Name { get; set; }
    public string Address { get; set; }
    public string Email { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? ValidityDate { get; set; }
    public List<ManufacturerMaterialDto> Materials { get; set; } = [];
}

public class ManufacturerMaterialDto : BaseDto
{
    public CollectionItemDto Manufacturer { get; set; }
    public MaterialDto Material { get; set; }
}