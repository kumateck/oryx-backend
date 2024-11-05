namespace DOMAIN.Entities.Procurement.Suppliers;

public class CreateSupplierRequest
{
    public string Name { get; set; }
    public string ContactPerson { get; set; }
    public string ContactNumber { get; set; }
    public List<CreateSupplierManufacturerRequest> AssociatedManufacturers { get; set; } = [];
}

public class CreateSupplierManufacturerRequest
{
    public Guid ManufacturerId { get; set; }
}