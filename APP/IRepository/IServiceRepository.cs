using APP.Utils;
using DOMAIN.Entities.Services;
using SHARED;

namespace APP.IRepository;

public interface IServiceRepository
{
    Task<Result<Guid>> CreateService(CreateServiceRequest request);
    Task<Result<Paginateable<IEnumerable<ServiceDto>>>> GetServices(int page, int pageSize, string searchQuery, bool? isActive = null,
        DateTime? startDate = null, DateTime? endDate = null);
    Task<Result<ServiceDto>> GetService(Guid id);
    Task<Result> UpdateService(Guid id, CreateServiceRequest request);
    Task<Result> DeleteService(Guid id, Guid userId);
}