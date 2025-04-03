using APP.IRepository;
using APP.Utils;
using AutoMapper;
using DOMAIN.Entities.ActivityLogs;
using INFRASTRUCTURE.Context;

namespace APP.Repository;

public class ActivityLogRepository(IMapper mapper, ApplicationDbContext context, MongoDbContext mongoDbContext) : IActivityLogRepository
{
    public async Task RecordActivity(CreateActivityLog model)
    {
        var activityLog = mapper.Map<ActivityLog>(model);
    }

    public Task<Paginateable<IEnumerable<ActivityLogDto>>> GetActivityLogs(ActivityLogFilter filter)
    {
        throw new NotImplementedException();
    }
}