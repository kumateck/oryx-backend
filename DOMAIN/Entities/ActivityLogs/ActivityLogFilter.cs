using SHARED;

namespace DOMAIN.Entities.ActivityLogs;

public class ActivityLogFilter : PagedQuery
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}