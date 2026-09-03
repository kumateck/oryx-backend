namespace DOMAIN.Entities.Performance;

public class CreateGoalRequest
{
    public Guid EmployeeId { get; set; }
    public Guid CycleId { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public int Weight { get; set; }
    public DateTime TargetDate { get; set; }
}

public class UpdateGoalRequest
{
    public string Title { get; set; }
    public string Description { get; set; }
    public int Weight { get; set; }
    public DateTime TargetDate { get; set; }
    public GoalStatus Status { get; set; }
    public int ProgressPercentage { get; set; }
    public string ManagerComments { get; set; }
}

public class GoalDto
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public string EmployeeName { get; set; }
    public Guid CycleId { get; set; }
    public string CycleName { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public int Weight { get; set; }
    public DateTime TargetDate { get; set; }
    public GoalStatus Status { get; set; }
    public int ProgressPercentage { get; set; }
    public string ManagerComments { get; set; }
}
