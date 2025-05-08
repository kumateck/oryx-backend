using APP.Utils;
using DOMAIN.Entities.ActivityLogs;

namespace APP.IRepository;

public interface IActivityLogRepository
{
    Task RecordActivityAsync(CreateActivityLog model);
    void RecordActivity(CreateActivityLog model);

    Task<Paginateable<IEnumerable<ActivityLogDto>>> GetActivityLogs(ActivityLogFilter filter);
}