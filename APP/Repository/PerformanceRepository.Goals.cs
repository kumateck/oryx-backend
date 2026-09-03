using APP.Utils;
using DOMAIN.Entities.Performance;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public partial class PerformanceRepository
{
    public async Task<Result<Guid>> CreateGoal(CreateGoalRequest request, Guid userId)
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

        var goal = mapper.Map<Goal>(request);
        goal.CreatedById = userId;

        await context.Goals.AddAsync(goal);
        await context.SaveChangesAsync();

        return goal.Id;
    }

    public async Task<Result> UpdateGoal(Guid id, UpdateGoalRequest request)
    {
        var goal = await context.Goals.FirstOrDefaultAsync(g => g.Id == id);
        if (goal is null)
        {
            return Error.NotFound("Goal.NotFound", "Goal not found");
        }

        mapper.Map(request, goal);
        context.Goals.Update(goal);
        await context.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result> DeleteGoal(Guid id, Guid userId)
    {
        var goal = await context.Goals.FirstOrDefaultAsync(g => g.Id == id);
        if (goal is null)
        {
            return Error.NotFound("Goal.NotFound", "Goal not found");
        }

        goal.DeletedAt = DateTime.UtcNow;
        goal.LastDeletedById = userId;
        context.Goals.Update(goal);
        await context.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result<Paginateable<IEnumerable<GoalDto>>>> GetGoals(
        Guid? employeeId, Guid? cycleId, int page, int pageSize)
    {
        var query = context.Goals
            .Include(g => g.Employee)
            .Include(g => g.Cycle)
            .AsQueryable();

        if (employeeId.HasValue)
        {
            query = query.Where(g => g.EmployeeId == employeeId.Value);
        }

        if (cycleId.HasValue)
        {
            query = query.Where(g => g.CycleId == cycleId.Value);
        }

        query = query.OrderByDescending(g => g.TargetDate);

        return await PaginationHelper.GetPaginatedResultAsync(
            query, page, pageSize, mapper.Map<GoalDto>);
    }

    public async Task<Result<GoalDto>> GetGoal(Guid id)
    {
        var goal = await context.Goals
            .Include(g => g.Employee)
            .Include(g => g.Cycle)
            .FirstOrDefaultAsync(g => g.Id == id);

        return goal is null
            ? Error.NotFound("Goal.NotFound", "Goal not found")
            : mapper.Map<GoalDto>(goal);
    }
}
