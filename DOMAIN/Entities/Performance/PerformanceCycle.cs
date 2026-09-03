using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;

namespace DOMAIN.Entities.Performance;

public class PerformanceCycle : BaseEntity
{
    [StringLength(200)] public string Name { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public PerformanceCycleType Type { get; set; }
    public PerformanceCycleStatus Status { get; set; } = PerformanceCycleStatus.Draft;

    public List<Goal> Goals { get; set; } = [];
    public List<PerformanceReview> Reviews { get; set; } = [];
}

public enum PerformanceCycleType
{
    Annual,
    SemiAnnual,
    Quarterly
}

public enum PerformanceCycleStatus
{
    Draft,
    Active,
    Closed
}
