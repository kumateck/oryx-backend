using APP.Utils;
using DOMAIN.Entities.Procurement.Suppliers;
using SHARED;

namespace APP.IRepository;

public interface ISupplierRepository
{
    Task<Result<Guid>> CreateSupplier(CreateSupplierRequest request);
    Task<Result<Paginateable<IEnumerable<SupplierDto>>>> GetSuppliers(int page, int pageSize, string searchQuery);
    Task<Result<SupplierDto>> GetSupplier(Guid id);
    Task<Result> UpdateSupplier(Guid id, CreateSupplierRequest request);
    Task<Result> DeleteSupplier(Guid id, Guid userId);
}