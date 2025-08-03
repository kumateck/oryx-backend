using APP.Utils;
using DOMAIN.Entities.Vendors;
using SHARED;

namespace APP.IRepository;

public interface IVendorRepository
{
    Task<Result<Guid>> CreateVendor(CreateVendorRequest request);
    Task<Result<Paginateable<IEnumerable<VendorDto>>>> GetVendors(int page, int pageSize,
        string searchQuery);
    Task<Result<VendorDto>> GetVendor(Guid id);
    Task<Result> UpdateVendor(Guid id, CreateVendorRequest request);
    Task<Result> DeleteVendor(Guid id, Guid userId);
}