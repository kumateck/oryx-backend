using System.ComponentModel.DataAnnotations;
using DOMAIN.Entities.Approvals;
using DOMAIN.Entities.Base;
using DOMAIN.Entities.Employees;

namespace DOMAIN.Entities.Performance;

public class PerformanceReview : BaseEntity, IRequireApproval
{
    public Guid EmployeeId { get; set; }
    public Employee Employee { get; set; }

    public Guid CycleId { get; set; }
    public PerformanceCycle Cycle { get; set; }

    public Guid? ReviewerId { get; set; }
    public Employee Reviewer { get; set; }

    public PerformanceReviewStatus Status { get; set; } = PerformanceReviewStatus.Draft;

    [StringLength(4000)] public string SelfAssessmentComments { get; set; }
    public DateTime? SelfAssessmentSubmittedAt { get; set; }

    [StringLength(4000)] public string ManagerComments { get; set; }
    public DateTime? ManagerSubmittedAt { get; set; }

    public PerformanceRating? OverallRating { get; set; }
    public bool Approved { get; set; }

    public List<GoalRating> GoalRatings { get; set; } = [];
    public List<PerformanceReviewApproval> Approvals { get; set; } = [];
}

public class GoalRating : BaseEntity
{
    public Guid PerformanceReviewId { get; set; }
    public PerformanceReview PerformanceReview { get; set; }

    public Guid GoalId { get; set; }
    public Goal Goal { get; set; }

    public int AchievementPercentage { get; set; }
    [StringLength(2000)] public string Comments { get; set; }
}

public class PerformanceReviewApproval : ResponsibleApprovalStage
{
    public Guid Id { get; set; }
    public Guid PerformanceReviewId { get; set; }
    public PerformanceReview PerformanceReview { get; set; }
    public Guid ApprovalId { get; set; }
    public Approval Approval { get; set; }
}

public enum PerformanceReviewStatus
{
    Draft,
    SelfAssessment,
    ManagerReview,
    PendingApproval,
    Completed
}

public enum PerformanceRating
{
    Unsatisfactory,
    NeedsImprovement,
    MeetsExpectations,
    ExceedsExpectations,
    Outstanding
}
