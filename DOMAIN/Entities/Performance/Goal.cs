using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Employees;

namespace DOMAIN.Entities.Performance;

public class Goal : BaseEntity
{
    public Guid EmployeeId { get; set; }
    public Employee Employee { get; set; }

    public Guid CycleId { get; set; }
    public PerformanceCycle Cycle { get; set; }

    [StringLength(300)] public string Title { get; set; }
    [StringLength(2000)] public string Description { get; set; }
    public int Weight { get; set; }
    public DateTime TargetDate { get; set; }
    public GoalStatus Status { get; set; } = GoalStatus.NotStarted;
    public int ProgressPercentage { get; set; }
    [StringLength(2000)] public string ManagerComments { get; set; }
}

public enum GoalStatus
{
    NotStarted,
    InProgress,
    Completed,
    Missed
}
