using INFRASTRUCTURE.Reporting;
using Microsoft.EntityFrameworkCore;
namespace INFRASTRUCTURE.Context;
public partial class ApplicationDbContext
{
    public DateTime? ReportAsOfUtc { get; internal set; }
    internal HashSet<string>? ReportHistoryTables { get; set; }
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    { base.OnConfiguring(optionsBuilder); optionsBuilder.AddInterceptors(ReportHistoryReadGuard.Instance); }
}
