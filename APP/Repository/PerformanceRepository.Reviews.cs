using APP.Utils;
using DOMAIN.Entities.Performance;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public partial class PerformanceRepository
{
    public async Task<Result<Guid>> CreateReview(CreatePerformanceReviewRequest request, Guid userId)
    {
        var employee = await context.Employees.FindAsync(request.EmployeeId);
        if (employee is null)
        {
            return Error.NotFound("Employee.NotFound", "Employee not found");
        }

        var cycle = await context.PerformanceCycles.FindAsync(request.CycleId);
        if (cycle is null)
        {
            return Error.NotFound("PerformanceCycle.NotFound", "Performance cycle not found");
        }

        var exists = await context.PerformanceReviews.AnyAsync(r =>
            r.EmployeeId == request.EmployeeId && r.CycleId == request.CycleId);
        if (exists)
        {
            return Error.Conflict("PerformanceReview.Exists", "A review already exists for this employee in this cycle");
        }

        var review = mapper.Map<PerformanceReview>(request);
        review.Status = PerformanceReviewStatus.SelfAssessment;
        review.CreatedById = userId;

        await context.PerformanceReviews.AddAsync(review);
        await context.SaveChangesAsync();

        return review.Id;
    }

    public async Task<Result<Paginateable<IEnumerable<PerformanceReviewDto>>>> GetReviews(
        Guid? employeeId, Guid? cycleId, int page, int pageSize)
    {
        var query = context.PerformanceReviews
            .Include(r => r.Employee)
            .Include(r => r.Cycle)
            .Include(r => r.Reviewer)
            .AsQueryable();

        if (employeeId.HasValue)
        {
            query = query.Where(r => r.EmployeeId == employeeId.Value);
        }

        if (cycleId.HasValue)
        {
            query = query.Where(r => r.CycleId == cycleId.Value);
        }

        query = query.OrderByDescending(r => r.CreatedAt);

        return await PaginationHelper.GetPaginatedResultAsync(
            query, page, pageSize, mapper.Map<PerformanceReviewDto>);
    }

    public async Task<Result<PerformanceReviewDto>> GetReview(Guid id)
    {
        var review = await context.PerformanceReviews
            .Include(r => r.Employee)
            .Include(r => r.Cycle)
            .Include(r => r.Reviewer)
            .Include(r => r.GoalRatings).ThenInclude(gr => gr.Goal)
            .FirstOrDefaultAsync(r => r.Id == id);

        return review is null
            ? Error.NotFound("PerformanceReview.NotFound", "Performance review not found")
            : mapper.Map<PerformanceReviewDto>(review);
    }

    public async Task<Result> SubmitSelfAssessment(Guid id, SubmitSelfAssessmentRequest request)
    {
        var review = await context.PerformanceReviews
            .Include(r => r.GoalRatings)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (review is null)
        {
            return Error.NotFound("PerformanceReview.NotFound", "Performance review not found");
        }

        if (review.Status != PerformanceReviewStatus.SelfAssessment)
        {
            return Error.Validation("PerformanceReview.InvalidStatus", "This review is not awaiting a self-assessment");
        }

        review.SelfAssessmentComments = request.Comments;
        review.SelfAssessmentSubmittedAt = DateTime.UtcNow;
        review.Status = PerformanceReviewStatus.ManagerReview;

        await UpsertGoalRatings(review, request.GoalRatings);

        context.PerformanceReviews.Update(review);
        await context.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result> SubmitManagerReview(Guid id, SubmitManagerReviewRequest request, Guid userId)
    {
        var review = await context.PerformanceReviews
            .Include(r => r.GoalRatings)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (review is null)
        {
            return Error.NotFound("PerformanceReview.NotFound", "Performance review not found");
        }

        if (review.Status != PerformanceReviewStatus.ManagerReview)
        {
            return Error.Validation("PerformanceReview.InvalidStatus", "This review is not awaiting a manager assessment");
        }

        review.ManagerComments = request.Comments;
        review.OverallRating = request.OverallRating;
        review.ManagerSubmittedAt = DateTime.UtcNow;
        review.Status = PerformanceReviewStatus.PendingApproval;
        review.LastUpdatedById = userId;

        await UpsertGoalRatings(review, request.GoalRatings);

        context.PerformanceReviews.Update(review);
        await context.SaveChangesAsync();

        await approvalRepository.CreateInitialApprovalsAsync(nameof(PerformanceReview), review.Id);

        return Result.Success();
    }

    private async Task UpsertGoalRatings(PerformanceReview review, List<GoalRatingRequest> ratings)
    {
        foreach (var rating in ratings)
        {
            var existing = review.GoalRatings.FirstOrDefault(gr => gr.GoalId == rating.GoalId);
            if (existing is not null)
            {
                existing.AchievementPercentage = rating.AchievementPercentage;
                existing.Comments = rating.Comments;
            }
            else
            {
                await context.GoalRatings.AddAsync(new GoalRating
                {
                    PerformanceReviewId = review.Id,
                    GoalId = rating.GoalId,
                    AchievementPercentage = rating.AchievementPercentage,
                    Comments = rating.Comments,
                });
            }
        }
    }
}
