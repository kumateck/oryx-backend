using SHARED;
using APP.Utils;
using DOMAIN.Entities.Configurations;

namespace APP.IRepository
{
    public interface IConfigurationRepository
    {
        Task<Result<Guid>> CreateConfiguration(CreateConfigurationRequest request, Guid userId);
        Task<Result<ConfigurationDto>> GetConfiguration(Guid configurationId);
        Task<Result<ConfigurationDto>> GetConfiguration(string modelType);
        Task<Result<Paginateable<IEnumerable<ConfigurationDto>>>> GetConfigurations(int page, int pageSize, string searchQuery = null);
        Task<Result> UpdateConfiguration(CreateConfigurationRequest request, Guid configurationId);
        Task<Result> DeleteConfiguration(Guid configurationId);
        Task<Result<int>> GetCountForCodeConfiguration(string modelType, string prefix);
    }
}