using APP.Utils;
using DOMAIN.Entities.ServiceProviders;
using SHARED;

namespace APP.IRepository;

public interface IServiceProviderRepository
{
    Task<Result<Guid>> CreateServiceProvider(CreateServiceProviderRequest request);
    Task<Result<Paginateable<IEnumerable<ServiceProviderReducedDto>>>> GetServiceProviders(int page, 
        int pageSize, 
        string searchQuery,
        Guid? serviceId = null);
    Task<Result<IEnumerable<ServiceProviderDto>>> GetServiceProvidersByService(Guid serviceId);
    Task<Result<ServiceProviderDto>> GetServiceProvider(Guid id);
    Task<Result> UpdateServiceProvider(Guid id, CreateServiceProviderRequest request);
    Task<Result> DeleteServiceProvider(Guid id, Guid userId);
}