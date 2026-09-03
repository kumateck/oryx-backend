namespace DOMAIN.Entities.Performance;

public class CreatePerformanceCycleRequest
{
    public string Name { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public PerformanceCycleType Type { get; set; }
}

public class UpdatePerformanceCycleStatusRequest
{
    public PerformanceCycleStatus Status { get; set; }
}

public class PerformanceCycleDto
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public PerformanceCycleType Type { get; set; }
    public PerformanceCycleStatus Status { get; set; }
    public int GoalCount { get; set; }
    public int ReviewCount { get; set; }
}
