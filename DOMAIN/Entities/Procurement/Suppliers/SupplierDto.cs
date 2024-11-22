using DOMAIN.Entities.Base;
using DOMAIN.Entities.Materials;
using DOMAIN.Entities.Procurement.Manufacturers;
using SHARED;

namespace DOMAIN.Entities.Procurement.Suppliers;

public class SupplierDto : BaseDto
{
    public string Name { get; set; }
    public string ContactPerson { get; set; }
    public string ContactNumber { get; set; }
    public List<SupplierManufacturerDto> AssociatedManufacturers { get; set; } = [];
}

public class SupplierManufacturerDto : BaseDto
{
    public CollectionItemDto Supplier { get; set; }
    public ManufacturerDto Manufacturer { get; set; }
    public MaterialDto Material { get; set; }
}