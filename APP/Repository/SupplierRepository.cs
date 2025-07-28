using APP.IRepository;
using APP.Utils;
using DOMAIN.Entities.Procurement.Suppliers;
using SHARED;

namespace APP.Repository;

public class SupplierRepository : ISupplierRepository
{
    public async Task<Result<Guid>> CreateSupplier(CreateSupplierRequest request)
    {
        throw new NotImplementedException();
    }

    public async Task<Result<Paginateable<IEnumerable<SupplierDto>>>> GetSuppliers(int page, int pageSize, string searchQuery)
    {
        throw new NotImplementedException();
    }

    public async Task<Result<SupplierDto>> GetSupplier(Guid id)
    {
        throw new NotImplementedException();
    }

    public async Task<Result> UpdateSupplier(Guid id, CreateSupplierRequest request)
    {
        throw new NotImplementedException();
    }

    public async Task<Result> DeleteSupplier(Guid id, Guid userId)
    {
        throw new NotImplementedException();
    }
}