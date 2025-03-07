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
    public SupplierStatus Status { get; set; }
    public List<CreateSupplierManufacturerRequest> AssociatedManufacturers { get; set; } = [];
}

public class CreateSupplierManufacturerRequest
{
    public Guid ManufacturerId { get; set; }
    public Guid? MaterialId { get; set; }
    public decimal QuantityPerPack { get; set; }
}