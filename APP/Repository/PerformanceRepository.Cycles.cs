using DOMAIN.Entities.Performance;
using Microsoft.EntityFrameworkCore;
using SHARED;

namespace APP.Repository;

public partial class PerformanceRepository
{
    public async Task<Result<Guid>> CreateCycle(CreatePerformanceCycleRequest request, Guid userId)
    {
        if (request.PeriodEnd <= request.PeriodStart)
        {
            return Error.Validation("PerformanceCycle.InvalidPeriod", "Period end must be after period start");
        }

        var cycle = mapper.Map<PerformanceCycle>(request);
        cycle.CreatedById = userId;

        await context.PerformanceCycles.AddAsync(cycle);
        await context.SaveChangesAsync();

        return cycle.Id;
    }

    public async Task<Result<List<PerformanceCycleDto>>> GetCycles()
    {
        var cycles = await context.PerformanceCycles
            .Include(c => c.Goals)
            .Include(c => c.Reviews)
            .OrderByDescending(c => c.PeriodStart)
            .ToListAsync();

        return mapper.Map<List<PerformanceCycleDto>>(cycles);
    }

    public async Task<Result<PerformanceCycleDto>> GetCycle(Guid id)
    {
        var cycle = await context.PerformanceCycles
            .Include(c => c.Goals)
            .Include(c => c.Reviews)
            .FirstOrDefaultAsync(c => c.Id == id);

        return cycle is null
            ? Error.NotFound("PerformanceCycle.NotFound", "Performance cycle not found")
            : mapper.Map<PerformanceCycleDto>(cycle);
    }

    public async Task<Result> UpdateCycleStatus(Guid id, UpdatePerformanceCycleStatusRequest request)
    {
        var cycle = await context.PerformanceCycles.FirstOrDefaultAsync(c => c.Id == id);
        if (cycle is null)
        {
            return Error.NotFound("PerformanceCycle.NotFound", "Performance cycle not found");
        }

        cycle.Status = request.Status;
        context.PerformanceCycles.Update(cycle);
        await context.SaveChangesAsync();

        return Result.Success();
    }
}
