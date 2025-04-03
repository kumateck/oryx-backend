using APP.IRepository;
using APP.Utils;
using DOMAIN.Entities.ActivityLogs;

namespace APP.Repository;

public class ActivityLogRepository : IActivityLogRepository
{
    public Task RecordActivity(CreateActivityLog model)
    {
        throw new NotImplementedException();
    }

    public Task<Paginateable<IEnumerable<ActivityLogDto>>> GetActivityLogs(ActivityLogFilter filter)
    {
        throw new NotImplementedException();
    }
}