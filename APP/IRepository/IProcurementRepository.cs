using SHARED;
using APP.Utils;
using DOMAIN.Entities.Procurement.Manufacturers;
using DOMAIN.Entities.Procurement.Suppliers;

namespace APP.IRepository
{
    public interface IProcurementRepository
    {
        // Manufacturer CRUD methods
        Task<Result<Guid>> CreateManufacturer(CreateManufacturerRequest request, Guid userId);
        Task<Result<ManufacturerDto>> GetManufacturer(Guid manufacturerId);
        Task<Result<Paginateable<IEnumerable<ManufacturerDto>>>> GetManufacturers(int page, int pageSize, string searchQuery);
        Task<Result<IEnumerable<ManufacturerDto>>> GetManufacturersByMaterial(Guid materialId);
        Task<Result> UpdateManufacturer(CreateManufacturerRequest request, Guid manufacturerId, Guid userId);
        Task<Result> DeleteManufacturer(Guid manufacturerId, Guid userId);

        // Supplier CRUD methods
        Task<Result<Guid>> CreateSupplier(CreateSupplierRequest request, Guid userId);
        Task<Result<SupplierDto>> GetSupplier(Guid supplierId);
        Task<Result<Paginateable<IEnumerable<SupplierDto>>>> GetSuppliers(int page, int pageSize, string searchQuery);
        Task<Result<IEnumerable<SupplierDto>>> GetSupplierByMaterial(Guid materialId);
        Task<Result> UpdateSupplier(CreateSupplierRequest request, Guid supplierId, Guid userId);
        Task<Result> DeleteSupplier(Guid supplierId, Guid userId);
    }
}