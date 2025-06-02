using APP.Utils;
using DOMAIN.Entities.Alerts;

namespace APP.IRepository;

public interface IAlertRepository
{ 
    Task CreateAlert(CreateAlertRequest request);
    Task<AlertDto> GetAlert(Guid alertId);
    Task<Paginateable<IEnumerable<AlertDto>>> GetAlerts(int page, int pageSize, string searchQuery,
        bool withDisabled = false);
    Task UpdateAlert(CreateAlertRequest request, Guid userId, Guid approvalId);
    Task ToggleDisable(Guid id);
}