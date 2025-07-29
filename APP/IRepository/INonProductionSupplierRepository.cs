using APP.Utils;
using DOMAIN.Entities.NonProductionSuppliers;
using SHARED;

namespace APP.IRepository;

public interface INonProductionSupplierRepository
{
    Task<Result<Guid>> CreateNonProductionSupplier(CreateNonProductionSupplierRequest request);
    Task<Result<Paginateable<IEnumerable<NonProductionSupplierDto>>>> GetNonProductionSuppliers(int page, int pageSize,
        string searchQuery);
    Task<Result<NonProductionSupplierDto>> GetNonProductionSupplier(Guid id);
    Task<Result> UpdateNonProductionSupplier(Guid id, CreateNonProductionSupplierRequest request);
    Task<Result> DeleteNonProductionSupplier(Guid id, Guid userId);
}