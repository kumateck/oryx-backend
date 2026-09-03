namespace DOMAIN.Entities.Performance;

public class CreatePerformanceReviewRequest
{
    public Guid EmployeeId { get; set; }
    public Guid CycleId { get; set; }
    public Guid? ReviewerId { get; set; }
}

public class SubmitSelfAssessmentRequest
{
    public string Comments { get; set; }
    public List<GoalRatingRequest> GoalRatings { get; set; } = [];
}

public class SubmitManagerReviewRequest
{
    public string Comments { get; set; }
    public PerformanceRating OverallRating { get; set; }
    public List<GoalRatingRequest> GoalRatings { get; set; } = [];
}

public class GoalRatingRequest
{
    public Guid GoalId { get; set; }
    public int AchievementPercentage { get; set; }
    public string Comments { get; set; }
}

public class PerformanceReviewDto
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public string EmployeeName { get; set; }
    public Guid CycleId { get; set; }
    public string CycleName { get; set; }
    public Guid? ReviewerId { get; set; }
    public string ReviewerName { get; set; }
    public PerformanceReviewStatus Status { get; set; }
    public string SelfAssessmentComments { get; set; }
    public DateTime? SelfAssessmentSubmittedAt { get; set; }
    public string ManagerComments { get; set; }
    public DateTime? ManagerSubmittedAt { get; set; }
    public PerformanceRating? OverallRating { get; set; }
    public bool Approved { get; set; }
    public List<GoalRatingDto> GoalRatings { get; set; } = [];
}

public class GoalRatingDto
{
    public Guid GoalId { get; set; }
    public string GoalTitle { get; set; }
    public int AchievementPercentage { get; set; }
    public string Comments { get; set; }
}
