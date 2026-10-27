using APP.Utils;
using DOMAIN.Entities.Performance;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

/// <summary>
/// Who may act on a goal or review, by the employee record linked to the signed-in account (Employee.UserId), and the rules
/// a goal and a set of goal ratings must satisfy. Employee and user identities are never compared directly.
/// </summary>
public partial class PerformanceRepository
{
    public Task<Guid?> EmployeeOf(Guid userId) =>
        context.Employees.Where(e => e.UserId == userId).Select(e => (Guid?)e.Id).FirstOrDefaultAsync();

    public Task<Guid?> GoalOwner(Guid goalId) =>
        context.Goals.Where(g => g.Id == goalId && g.DeletedAt == null).Select(g => (Guid?)g.EmployeeId).FirstOrDefaultAsync();

    public async Task<(Guid EmployeeId, Guid? ReviewerId)?> ReviewParticipants(Guid reviewId)
    {
        var review = await context.PerformanceReviews.Where(r => r.Id == reviewId).Select(r => new { r.EmployeeId, r.ReviewerId }).FirstOrDefaultAsync();
        return review == null ? null : (review.EmployeeId, review.ReviewerId);
    }

    /// <summary>True when the reviewer is assigned a review of the employee in the cycle.</summary>
    public Task<bool> Reviews(Guid reviewerId, Guid employeeId, Guid cycleId) =>
        context.PerformanceReviews.AnyAsync(r => r.ReviewerId == reviewerId && r.EmployeeId == employeeId && r.CycleId == cycleId);

    public async Task<Result<Paginateable<IEnumerable<PerformanceReviewDto>>>> GetReviewsFor(Guid employeeId, int page, int pageSize)
    {
        var query = context.PerformanceReviews.Include(r => r.Employee).Include(r => r.Cycle).Include(r => r.Reviewer)
            .Where(r => r.EmployeeId == employeeId || r.ReviewerId == employeeId).OrderByDescending(r => r.CreatedAt);
        return await APP.Utils.PaginationHelper.GetPaginatedResultAsync(query, page, pageSize, mapper.Map<PerformanceReviewDto>);
    }

    private async Task<Result> ValidGoal(Guid employeeId, Guid cycleId, string title, int weight, DateTime targetDate, Guid? goalId)
    {
        if (string.IsNullOrWhiteSpace(title)) return Error.Validation("Goal.Title", "A goal needs a title.");
        if (weight is < 1 or > 100) return Error.Validation("Goal.Weight", "A goal's weight must be between 1 and 100.");
        var cycle = await context.PerformanceCycles.FirstOrDefaultAsync(c => c.Id == cycleId);
        if (cycle is null) return Error.NotFound("PerformanceCycle.NotFound", "Performance cycle not found");
        if (cycle.Status == PerformanceCycleStatus.Closed) return Error.Validation("Goal.CycleClosed", "The performance cycle is closed.");
        if (targetDate.Date < cycle.PeriodStart.Date || targetDate.Date > cycle.PeriodEnd.Date)
            return Error.Validation("Goal.TargetDate", "The target date must fall within the performance cycle.");
        var others = await context.Goals.Where(g => g.EmployeeId == employeeId && g.CycleId == cycleId && g.DeletedAt == null && g.Id != goalId)
            .Select(g => new { g.Title, g.Weight }).ToListAsync();
        if (others.Any(g => string.Equals(g.Title?.Trim(), title.Trim(), StringComparison.OrdinalIgnoreCase)))
            return Error.Validation("Goal.Duplicate", "This employee already has a goal with that title in the cycle.");
        if (others.Sum(g => g.Weight) + weight > 100) return Error.Validation("Goal.Weight", "The employee's goal weights in a cycle cannot add up to more than 100%.");
        return Result.Success();
    }

    /// <summary>Ratings must cover distinct goals of the review's employee and cycle, each between 0 and 100%.</summary>
    private async Task<Result> ValidRatings(PerformanceReview review, List<GoalRatingRequest> ratings)
    {
        ratings ??= [];
        if (ratings.GroupBy(r => r.GoalId).Any(g => g.Count() > 1)) return Error.Validation("Review.Ratings", "Rate each goal once.");
        if (ratings.Any(r => r.AchievementPercentage is < 0 or > 100)) return Error.Validation("Review.Ratings", "Achievement must be between 0 and 100%.");
        var ids = ratings.Select(r => r.GoalId).ToList();
        var valid = await context.Goals.CountAsync(g => ids.Contains(g.Id) && g.EmployeeId == review.EmployeeId && g.CycleId == review.CycleId && g.DeletedAt == null);
        return valid == ids.Count ? Result.Success() : Error.Validation("Review.Ratings", "Only this employee's goals for this cycle can be rated.");
    }
}
