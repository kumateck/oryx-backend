using APP.Utils;
using DOMAIN.Entities.Performance;
using SHARED;

namespace APP.IRepository;

public interface IPerformanceRepository
{
    // Cycles
    Task<Result<Guid>> CreateCycle(CreatePerformanceCycleRequest request, Guid userId);
    Task<Result<List<PerformanceCycleDto>>> GetCycles();
    Task<Result<PerformanceCycleDto>> GetCycle(Guid id);
    Task<Result> UpdateCycleStatus(Guid id, UpdatePerformanceCycleStatusRequest request);

    // Goals
    Task<Result<Guid>> CreateGoal(CreateGoalRequest request, Guid userId);
    Task<Result> UpdateGoal(Guid id, UpdateGoalRequest request);
    Task<Result> DeleteGoal(Guid id, Guid userId);
    Task<Result<Paginateable<IEnumerable<GoalDto>>>> GetGoals(Guid? employeeId, Guid? cycleId, int page, int pageSize);
    Task<Result<GoalDto>> GetGoal(Guid id);

    // Reviews
    Task<Result<Guid>> CreateReview(CreatePerformanceReviewRequest request, Guid userId);
    Task<Result<Paginateable<IEnumerable<PerformanceReviewDto>>>> GetReviews(Guid? employeeId, Guid? cycleId, int page, int pageSize);
    Task<Result<PerformanceReviewDto>> GetReview(Guid id);
    Task<Result> SubmitSelfAssessment(Guid id, SubmitSelfAssessmentRequest request);
    Task<Result> SubmitManagerReview(Guid id, SubmitManagerReviewRequest request, Guid userId);
}
