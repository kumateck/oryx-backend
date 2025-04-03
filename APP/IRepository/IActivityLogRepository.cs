using APP.Utils;
using DOMAIN.Entities.ActivityLogs;

namespace APP.IRepository;

public interface IActivityLogRepository
{
    Task RecordActivity(CreateActivityLog model);
    Task<Paginateable<IEnumerable<ActivityLogDto>>> GetActivityLogs(ActivityLogFilter filter);
}